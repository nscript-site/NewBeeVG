using Avalonia;
using HelixToolkit.Nex.Engine.CameraControllers;
using HelixToolkit.Nex.Engine.Cameras;
using SkiaSharp;
using System.Numerics;

namespace NewBeeVG.ThreeD;

public class NBCanvas3D : NBVisual
{
    public int? HAlign { get; set; } = -1;
    public int? VAlign { get; set; } = -1;

    public int Width { get; set { field = Math.Max(0, value); } }
    public int Height { get; set { field = Math.Max(0, value); } }

    public SKColor? Background { get; set; }

    public NBScene3D Scene { get; private set; } = new NBScene3D();

    public Camera? Camera { get; internal set; }

    public ICameraController? CameraController { get; internal set; }

    protected override void TryMeasure(Size availableSize)
    {
        if (VAlign == null) Height = (int)availableSize.Height;
        if (HAlign == null) Width = (int)availableSize.Width;

        this.DesiredSize = new Size(Width, Height);
    }

    protected override void TryArrange(Rect rect)
    {
        SKPoint origin = new SKPoint((float)rect.Left, (float)rect.Top);
        if (HAlign != null)
        {
            if (HAlign < 0) origin.X = (float)rect.Left;
            else if (HAlign == 0) origin.X = (float)(rect.Left + rect.Width / 2 - Width / 2);
            else if (HAlign > 0) origin.X = (float)(rect.Right - Width);
        }
        if (VAlign != null)
        {
            if (VAlign < 0) origin.Y = (float)rect.Top;
            else if (VAlign == 0) origin.Y = (float)(rect.Top + rect.Height / 2 - Height / 2);
            else if (VAlign > 0) origin.Y = (float)(rect.Bottom - Height);
        }

        this.Bounds = new SKRect(origin.X, origin.Y, origin.X + (float)Width, origin.Y + (float)Height);
    }

    NBEngine? _engine;
    private bool _3dContextCreated = false;
    
    private void Create3DContext()
    {
        if (Camera == null)
        {
            Camera = new PerspectiveCamera
            {
                Position = new Vector3(0, 12, -25),
                Target = Vector3.Zero,
                FarPlane = 500,
            };
            CameraController = new OrbitCameraController(Camera);
        }
        Scene.AddMaterials();
        Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
        _engine = NB3D.CreateEngine(this.Width, this.Height, (Background ?? SKColors.Black).ToHelixColor());
        this.OnFrameUpdated += Scene.OnFrameUpdated;
        Scene.Build(_engine);
    }

    protected override void RenderContent(SKCanvas context)
    {
        if(_3dContextCreated == false)
        {
            lock(this)
            {
                _3dContextCreated = true;
                Create3DContext();
            }
        }

        using var snap = Render3D();
        if (snap != null)
        {
            using var paint = new SKPaint();
            context.DrawBitmap(snap, this.Bounds, null);
        }
    }

    protected SKBitmap? Render3D()
    {
        if (_engine == null || Camera == null) return null;

        _engine.Update(Camera);
        _engine.Render();

        var snap = _engine.SnapshotSKBitmap();
        return snap;
    }
}

public static partial class NBExtentions_Three3D
{
    public static T Nodes<T>(this T self, IList<NBNode3D> nodes) where T : NBCanvas3D
    {
        self.Scene.Nodes.AddRange(nodes);
        return self;
    }

    public static T Align<T>(this T self, int? hAlign = null, int? vAlign = null) where T : NBCanvas3D
    {
        self.HAlign = hAlign;
        self.VAlign = vAlign;
        return self;
    }

    public static T Size<T>(this T self, int width, int height) where T : NBCanvas3D
    {
        self.Width = width;
        self.Height = height;
        return self;
    }

    public static T Height<T>(this T self, int height) where T : NBCanvas3D
    {
        self.Height = height;
        return self;
    }

    public static T Width<T>(this T self, int width) where T : NBCanvas3D
    {
        self.Width = width;
        return self;
    }

    public static T Camera<T>(this T self, Camera camera, ICameraController? cameraController = null) where T : NBCanvas3D
    {
        self.Camera = camera;
        if (cameraController != null) self.CameraController = cameraController;
        return self;
    }
}