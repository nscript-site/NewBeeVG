using HelixToolkit.Nex.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.ThreeD;

/// <summary>
/// Abstracts the per-viewport shared engine output resource and its synchronization, hiding the
/// platform difference between the Windows path (D3D11 shared NT-handle texture + keyed mutex) and
/// the Linux path (exportable Vulkan external memory + semaphore). The control selects one concrete
/// bridge based on the host operating system and drives it each frame: the engine renders into
/// <see cref="EngineTarget"/>, the presenter imports <see cref="CreateImportDescription"/> through
/// <c>ICompositionGpuInterop</c>, and <see cref="CreateSurfaceSync"/> serializes the compositor read.
/// </summary>
public interface IEngineOutputBridge : IDisposable
{
    /// <summary>
    /// The engine render target the offscreen frame is rendered into. Backed by the shared texture
    /// (Windows) or the exportable Vulkan image (Linux).
    /// </summary>
    TextureHandle EngineTarget { get; }

    /// <summary>
    /// Recreates the shared output resources at the new size, releasing the previous ones. Called
    /// when the control size changes to nonzero dimensions.
    /// </summary>
    /// <param name="width">The new width in pixels.</param>
    /// <param name="height">The new height in pixels.</param>
    void Resize(uint width, uint height);

    /// <summary>
    /// Advances to the next output buffer after a frame has been rendered and handed to the presenter.
    /// A multi-buffered bridge rotates its write target so the engine can render the next frame into a
    /// free buffer while the compositor still reads the previously presented one, decoupling the
    /// engine from the compositor read and removing the single-texture keyed-mutex/semaphore stall.
    /// Single-buffered implementations treat this as a no-op.
    /// </summary>
    void AdvanceFrame();
}

/// <summary>
/// An <see cref="IEngineOutputBridge"/> that rotates over several independent single-buffer bridges to
/// decouple the engine render from the compositor read. Each wrapped bridge is a full shared output
/// (its own shared texture / external-memory image and its own keyed-mutex/semaphore synchronization).
/// </summary>
/// <remarks>
/// <para>
/// With a single shared texture the engine's next write blocks on the compositor's read of the same
/// texture (they contend on one keyed mutex / semaphore), so engine and compositor run in series and
/// the frame rate collapses to roughly half the display refresh rate. Rotating the write target across
/// N buffers lets the engine render frame N+1 into a free buffer while the compositor still reads the
/// buffer presented for frame N, so the two pipeline and the frame rate can reach the refresh rate.
/// </para>
/// <para>
/// The render loop drives this by rendering into <see cref="EngineTarget"/>, capturing the current
/// buffer's import description / surface sync, then calling <see cref="AdvanceFrame"/> to rotate the
/// write target for the next tick. Two buffers are sufficient when at most one present is in flight at
/// a time (the loop awaits the previous present before issuing the next), which is the model used here.
/// </para>
/// </remarks>
internal sealed class BufferedEngineOutputBridge : IEngineOutputBridge
{
    private readonly IEngineOutputBridge[] _buffers;
    private int _writeIndex;
    private bool _disposed;

    /// <summary>Creates a buffered bridge over the supplied per-buffer bridges (at least one).</summary>
    /// <param name="buffers">The independent single-buffer bridges to rotate over.</param>
    public BufferedEngineOutputBridge(IEngineOutputBridge[] buffers)
    {
        ArgumentNullException.ThrowIfNull(buffers);
        if (buffers.Length == 0)
        {
            throw new ArgumentException("At least one buffer is required.", nameof(buffers));
        }
        _buffers = buffers;
    }

    /// <summary>The number of rotating output buffers.</summary>
    public int BufferCount => _buffers.Length;

    /// <inheritdoc />
    public TextureHandle EngineTarget => _buffers[_writeIndex].EngineTarget;

    /// <inheritdoc />
    public void AdvanceFrame() => _writeIndex = (_writeIndex + 1) % _buffers.Length;

    /// <inheritdoc />
    public void Resize(uint width, uint height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (IEngineOutputBridge buffer in _buffers)
        {
            buffer.Resize(width, height);
        }
        // Restart the rotation so the first post-resize frame writes buffer 0.
        _writeIndex = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        foreach (IEngineOutputBridge buffer in _buffers)
        {
            buffer.Dispose();
        }
    }
}
