using HelixToolkit.Nex;
using HelixToolkit.Nex.ECS;
using HelixToolkit.Nex.Engine;
using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Rendering;
using HelixToolkit.Nex.Rendering.Components;
using HelixToolkit.Nex.Rendering.SDF;
using HelixToolkit.Nex.Scene;
using SkiaSharp;

namespace NewBeeVG.ThreeD;

public class NBEngine : IDisposable
{
    private RenderContext _renderContext;
    private NBTexture2D? _renderTexture;

    public string TextureName { get; private set; } = "RenderContext2D_Texture" + Guid.NewGuid().ToString();

    public Size Size => _renderContext.WindowSize;
    public int Width => _renderContext.WindowSize.Width;
    public int Height => _renderContext.WindowSize.Height;

    public TextureResource? Texture => _renderTexture?.Texture;
    public Handle<Texture>? TextureHandle => _renderTexture?.Handle;

    public IContext Context => _context;
    public RenderContext RenderContext => _renderContext;
    public Engine _engine;
    private WorldDataProvider _worldDataProvider;
    private Node _root;
    private IContext _context;
    public Node Root => _root;
    public World World => _worldDataProvider.World;

    public WorldDataProvider WorldDataProvider => _worldDataProvider;

    public IResourceManager ResourceManager => _engine.ResourceManager;

    public NBEngine(int width, int height, Color? backgroundColor = null)
    {
        _context = NB3D.CreateHeadlessContext();
        _engine = NB3D.CreateEngine(_context);

        _renderContext = _engine.CreateRenderContext();
        _renderContext.RenderParams.BackgroundColor = backgroundColor ?? Color.Transparent;
        _renderContext.EnvironmentMap.Enabled = false;
        _renderContext.WindowSize = new HelixToolkit.Nex.Maths.Size(width,height);
        _renderContext.Initialize();
        _renderContext.ResourceSet.AddTexture(
            TextureName,
            res =>
            {
                _renderTexture = NB3D.CreateTexture2D(_context, width, height, false);
                return _renderTexture.Texture;
            },
            dependsOnScreenSize: false
        );

        _worldDataProvider = _engine.CreateWorldDataProvider();
        _worldDataProvider.Initialize();

        var world = _worldDataProvider.World;
        _root = new Node(world) { Name = "Root" };
    }

    public Handle<GeometryResourceType> Add(Geometry geometry)
    {
        return _engine.ResourceManager.Geometries.Add(geometry);
    }

    public void Update(ICameraParamsProvider camrea)
    {
        _renderContext.Update(camrea);
    }

    public void Render()
    {   
        // 3D render (offscreen)
        _engine.BeginFrame();

        var cmdBuf = _engine.RenderOffscreen(
            _renderContext,
            _worldDataProvider,
            TextureName
        );

        _engine.Submit(cmdBuf, HelixToolkit.Nex.Handle<HelixToolkit.Nex.Graphics.Texture>.Null);
        _engine.WaitForIdle();
    }

    public T? GetRenderNode<T>()
        where T : RenderNode
    {
        return _engine.GetRenderNode<T>();
    }

    public BillboardDrawInfo CreateBillboard(
        BuildinFontAtlas fontType,
        string text,
        float fontSize,
        Color4 color,
        Color4? background = null,
        BillboardAnchor anchor = BillboardAnchor.Center,
        string materialName = "SDFFont",
        bool fixedSize = true,
        float cullDistance = 0
    )
    {
        return BillboardExtensions.CreateBillboard(_engine, fontType, text, fontSize, color, background, anchor, materialName, fixedSize, cullDistance);
    }

    public void Save(string bmpFilePath)
    {
        Texture?.SaveSKBitmap(_context, Width, Height, bmpFilePath);
    }

    public SKBitmap? SnapshotSKBitmap()
    {
        return _renderTexture?.SnapshotSKBitmap();
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

        _worldDataProvider.Dispose();
        _renderContext.Teardown();
        _engine.Dispose();
        _context.Dispose();

        _disposed = true;
    }

    ~NBEngine()
    {
        Dispose(false);
    }
}
