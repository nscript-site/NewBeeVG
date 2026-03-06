using Avalonia.Interactivity;
using NewBeeVG.Internal;
using SkiaSharp;

namespace NewBeeVG;

public class NBSkiaBitmap : Control
{
    public SKBitmap? Bitmap { get; set; } = default;

    public bool Pinned { get; set; }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bitmap == null) return;
        context.Custom(new SKBitmapDrawOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), Bitmap, null));
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (Bitmap != null && Pinned == false)
        {
            lock(this)
            {
                Bitmap.Dispose();
                Bitmap = null;
            }
        }
    }
}
