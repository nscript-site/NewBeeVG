using Avalonia.Media.Imaging;
using Avalonia.Threading;

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

    public RenderTargetBitmap CreateBitmap()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(Stage.Width, Stage.Height), new Vector(Stage.Dpi, Stage.Dpi));
        return bitmap;
    }

    public Control? Build(NBStage stage, int frame, bool includeStageBackground)
    {
        var panel = new Panel();
        panel.Width = stage.Width;
        panel.Height = stage.Height;

        if (includeStageBackground == true && stage.Background != null)
        {
            panel.Background = stage.Background;
        }

        foreach (var track in Tracks)
        {
            if (track.IsVisible == false) continue;
            var ctrl = track.Build(stage, frame, false);
            if (ctrl != null) panel.Children.Add(ctrl);
        }

        DrawingHelper.Layout(panel, stage.Width, stage.Height);

        return panel;
    }
}
