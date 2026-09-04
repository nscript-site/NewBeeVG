using SkiaSharp;

namespace NewBeeVG;

public class NBBorder
{
    public float Thickness = 0;
    public SKColor Color { get; init; }

    public NBBorder(float thickness, SKColor color)
    {
        Thickness = Math.Max(0,thickness);
        Color = color;
    }
}
