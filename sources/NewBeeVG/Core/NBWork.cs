using Avalonia.Media.Imaging;
using SkiaSharp;

namespace NewBeeVG;

public class NBWork : IPlayable
{
    public string Name { get; set; } = "work";
    public NBStage Stage { get; set; } = new NBStage();
    public List<NBTrack> Tracks { get; set; } = new List<NBTrack>();

    public string FullName => $"[Work] {Name}";

    private int _frames = -1;

    public int Measure()
    {
        if(_frames >= 0) return _frames;
        int max = 0;
        foreach (var track in Tracks)
            max = Math.Max(max, track.Measure());
        _frames = max;
        return max;
    }

    public virtual void Prepare() { }

    public SKBitmap CreateBitmap()
    {
        var bitmap = new SKBitmap(Stage.Width, Stage.Height);
        return bitmap;
    }

    public virtual SKBitmap? Render(NBStage stage, int frame, bool includeStageBackground)
    {
        var bitmap = new SKBitmap(stage.Width, stage.Height);
        using var canvas = new SKCanvas(bitmap);
        {
            if (includeStageBackground == true && stage.Background != null)
            {
                var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = stage.Background.Value,
                    IsAntialias = true
                };
                canvas.DrawRect(0, 0, stage.Width, stage.Height, paint);
            }
            foreach (var track in Tracks)
            {
                if (track.IsVisible == false) continue;
                var bmp = track.Render(stage, frame, false);
                if (bmp != null)
                    canvas.DrawBitmap(bmp, 0, 0);
            }
        }
        return bitmap;
    }
}
