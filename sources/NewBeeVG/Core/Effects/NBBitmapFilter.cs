using SkiaSharp;

namespace NewBeeVG;

public abstract class NBBitmapFilter
{
    public abstract (SKBitmap?, SKMatrix?) Filter(SKBitmap? bitmap);
}
