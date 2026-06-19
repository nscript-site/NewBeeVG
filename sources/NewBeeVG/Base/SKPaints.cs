using SkiaSharp;

namespace NewBeeVG;

public class SKPaints
{
    public static SKPaint From(SKColor color)
    {
        return new SKPaint
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
    }
}

public static class SKPaint_Extentions
{
    public static SKPaint ToPaint(this SKColor color)
    {
        return SKPaints.From(color);
    }
}
