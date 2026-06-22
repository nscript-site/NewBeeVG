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
            var dstWidth = _bounds.Width;
            var dstHeight = _bounds.Height;
            var srcWidth = _frame.Width;
            var srcHeight = _frame.Height;

            if (dstWidth <= 0 || dstHeight <= 0 || srcWidth <= 0 || srcHeight <= 0)
                return;

            var scaleX = dstWidth / srcWidth;
            var scaleY = dstHeight / srcHeight;
            var scale = Math.Min(scaleX, scaleY);

            var drawWidth = srcWidth * scale;
            var drawHeight = srcHeight * scale;

            var left = _bounds.X + (dstWidth - drawWidth) / 2f;
            var top = _bounds.Y + (dstHeight - drawHeight) / 2f;

            var dstRect = new SKRect((float)left, (float)top, (float)(left + drawWidth), (float)(top + drawHeight));

            if (_backgroundBrush != null)
            {
                context.FillRectangle(_backgroundBrush.ToImmutable(),
                    new Rect(dstRect.Left, dstRect.Top, dstRect.Width, dstRect.Height));
            }

            canvas.DrawBitmap(_frame, dstRect);
            //TODO: 提高绘制质量
            //using var paint = new SKPaint
            //{
            //    FilterQuality = SKFilterQuality.High,
            //    IsAntialias = true
            //};

            //canvas.DrawBitmap(_frame, dstRect, paint);
        }
    }
}