using Avalonia.Media.Imaging;

namespace NewBeeVG;

public class NBWork : IPlayable
{
    public string Name { get; set; } = "work";
    public NBStage Stage { get; set; } = new NBStage();
    public List<NBTrack> Tracks { get; set; } = new List<NBTrack>();

    public string FullName => $"[Work] {Name}";

    public int Measure()
    {
        int max = 0;
        foreach (var track in Tracks)
            max = Math.Max(max, track.Measure());
        return max;
    }

    public bool Render(RenderTargetBitmap bitmap, int frame)
    {
        foreach (var track in Tracks)
        {             
            if (track.IsVisible == false) continue;
            track.Render(bitmap, frame);
        }
        return true;
    }

    public RenderTargetBitmap CreateBitmap()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(Stage.Width, Stage.Height), new Vector(Stage.Dpi, Stage.Dpi));
        return bitmap;
    }
}
