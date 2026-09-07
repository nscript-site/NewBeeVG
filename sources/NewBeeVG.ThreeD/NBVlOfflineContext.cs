using HelixToolkit.Nex;
using HelixToolkit.Nex.Engine;
using HelixToolkit.Nex.Engine.CameraControllers;
using HelixToolkit.Nex.Engine.Cameras;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Graphics.Vulkan;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Rendering;
using HelixToolkit.Nex.Scene;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace NewBeeVG.ThreeD;

/// <summary>
/// Comprehensive point cloud rendering demo with:
/// - Multiple point cloud entities (sphere, helix, random cluster, animated wave)
/// - GPU picking (click a point to select its entity)
/// - ImGui controls for point size, colour, min screen size, add/remove clouds
/// - Orbit camera with keyboard (WASD) fallback
/// </summary>
public class NBVlOfflineContext : IDisposable
{
    private static readonly ILogger _logger = LogManager.Create<NBVlOfflineContext>();
    private const string ViewportTextureName = "ViewportTexture";

    private readonly IContext _context;
    private Engine? _engine;
    private RenderContext? _renderContext;
    private WorldDataProvider? _worldDataProvider;
    private Node? _root;

    // Camera
    private Camera _camera = new PerspectiveCamera();
    private OrbitCameraController? _orbitController;

    private TextureResource? _renderTexture;
    private Size ViewportSize;

    public Action<Node> OnBuildScene { get; set; }

    public Node? Root => _root;
    public Camera? Camera => _camera;
    public OrbitCameraController? OrbitController => _orbitController;

    public NBVlOfflineContext()
    {
        var vulkanConfig = new VulkanContextConfig
        {
            TerminateOnValidationError = true, // 调试打开，生产关闭
        };
        _context = VulkanBuilder.CreateHeadless(vulkanConfig);
        _engine = NB3DEngineBuilder.Create(_context).WithDefaultNodes(false)
            .WithSMAA()
            .WithBloom()
            .RenderToCustomTarget(Format.RGBA_UN8)
            .Build();
    }

    // ------------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------------

    public void Initialize(int width, int height)
    {
        _camera = new PerspectiveCamera
        {
            Position = new Vector3(0, 12, -25),
            Target = Vector3.Zero,
            FarPlane = 500,
        };
        _orbitController = new OrbitCameraController(_camera);

        ViewportSize = new Size(width, height);

        // Build the engine with the point rendering node
        _engine = NB3DEngineBuilder
            .Create(_context)
            .WithDefaultNodes()
            .RenderToCustomTarget(GraphicsSettings.IntermediateTargetFormat)
            .Build();
        _renderContext = _engine.CreateRenderContext();
        _renderContext.Initialize();
        _renderContext.WindowSize = ViewportSize;
        _renderContext.ResourceSet.AddTexture(
            ViewportTextureName,
            res =>
            {
                _renderTexture = _context.CreateRenderTarget2D(
                    Format.BGRA_UN8,
                    (uint)_renderContext.WindowSize.Width,
                    (uint)_renderContext.WindowSize.Height,
                    debugName: ViewportTextureName
                );
                return _renderTexture;
            },
            dependsOnScreenSize: false
        );

        _worldDataProvider = _engine.CreateWorldDataProvider();
        _worldDataProvider.Initialize();

        var world = _worldDataProvider!.World;
        _root = new Node(world) { Name = "Root" };

        OnBuildScene?.Invoke(_root);
    }

    public unsafe void DownloadRenderedImage()
    {
        if (_renderTexture == null)
            return;
        var desc = new TextureRangeDesc() { Dimensions = new Dimensions((uint)ViewportSize.Width, (uint)ViewportSize.Height) };
        var buff = new byte[ViewportSize.Width * ViewportSize.Height * 4];
        TextureHandle h = _renderTexture.Handle;
        fixed (byte* p = buff)
        {
            _context.Download(h, desc, (nint)p, (uint)buff.Length);
        }
    }

    public void Render()
    {
        if (_engine is null || _renderContext is null || _worldDataProvider is null)
            return;

        _renderContext.Update(_camera);

        // 3D render (offscreen)
        _engine.BeginFrame();

        var cmdBuf = _engine.RenderOffscreen(
            _renderContext,
            _worldDataProvider,
            ViewportTextureName
        );

        _engine.Submit(cmdBuf, TextureHandle.Null);
        _engine.WaitForIdle();
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _worldDataProvider?.Dispose();
            _renderContext?.Teardown();
            _engine?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}