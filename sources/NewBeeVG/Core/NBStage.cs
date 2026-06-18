using SkiaSharp;

namespace NewBeeVG;

public class NBStage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; } = 25;
    public SKColor? Background { get; set; }
}
