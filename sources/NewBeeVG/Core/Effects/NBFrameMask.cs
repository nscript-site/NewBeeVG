using SkiaSharp;

namespace NewBeeVG;

public abstract class NBFrameMask
{
    public abstract SKBitmap? BuildMaskBitmap(NBDrawContext ctx, SKRect rect);
}

public class NBBitmapFrameMask : NBFrameMask
{
    public Func<NBDrawContext, SKRect, SKBitmap> BitmapFunc { get; init; }
    public NBBitmapFrameMask(Func<NBDrawContext, SKRect, SKBitmap> bitmapFunc)
    {
        BitmapFunc = bitmapFunc;
    }
    public override SKBitmap? BuildMaskBitmap(NBDrawContext ctx, SKRect rect)
    {
        return BitmapFunc(ctx, rect);
    }
}