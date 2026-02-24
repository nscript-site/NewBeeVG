namespace NewBeeVG;

public class NBStage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Dpi { get; set; } = 96;
    public double FrameRate { get; set; } = 25;
    public IBrush? Background { get; set; }
}
