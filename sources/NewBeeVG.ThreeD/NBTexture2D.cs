using HelixToolkit.Nex;
using HelixToolkit.Nex.Graphics;
using SkiaSharp;

namespace NewBeeVG.ThreeD;

public class NBTexture2D : IDisposable
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Format Format { get; private set; }
    
    private TextureResource _renderTexture;
    public TextureResource Texture => _renderTexture;
    public Handle<Texture> Handle => Texture.Handle;

    private IContext _ctx;
    private bool _owned;

    public float Aspect => Width / (float)Height;

    public NBTexture2D(IContext ctx, int width, int height, bool owned = true)
    {
        _owned = owned;
        _ctx = ctx;
        Width = width;
        Height = height;
        Format = Format.BGRA_UN8;
        _renderTexture = ctx.CreateRenderTarget2D(
            Format.BGRA_UN8,
            (uint)Width,
            (uint)Height
        );
    }

    public void Save(string bmpFilePath)
    {
        _renderTexture.SaveSKBitmap(_ctx, Width, Height, bmpFilePath);
    }

    public SKBitmap? SnapshotSKBitmap()
    {
        return _renderTexture.SnapshotSKBitmap(_ctx, Width, Height);
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        // 告诉GC：不要再执行终结器，资源已经手动释放过
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return; // 防重复释放

        if (_owned == true)
        {
            if (Handle != Handle<Texture>.Null)
                _renderTexture.Dispose();
        }

        _disposed = true;
    }

    ~NBTexture2D()
    {
        Dispose(false);
    }
}
