using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace NewBeeVG.Internal;

public class SKBitmapDrawOperation : ICustomDrawOperation
{
    private readonly SKBitmap? _frame;
    private readonly Rect _bounds;
    private readonly IBrush? _backgroundBrush;

    public SKBitmapDrawOperation(Rect bounds, SKBitmap? frame, IBrush? backgroundBrush)
    {
        _frame = frame;
        _bounds = bounds;
        _backgroundBrush = backgroundBrush;
    }

    public void Dispose()
    {
    }

    public Rect Bounds => _bounds;

    public bool HitTest(Point p) => _bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => false;

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null)
        {
            return;
        }
        using var lease = leaseFeature.Lease();
        var canvas = lease?.SkCanvas;
        if (canvas is { } && _frame is { } && _frame.IsEmpty == false)
        {
            if (_backgroundBrush != null)
            {
                context.FillRectangle(_backgroundBrush.ToImmutable(),
                    _bounds);
            }

            canvas.DrawBitmap(_frame, 0, 0);
        }
    }
}