using Avalonia.Media.Imaging;
using SkiaSharp;

namespace NewBeeVG;

public class NBTrack : IPlayable
{
    public string Name { get; set; } = "track";
    public List<NBClip> Clips { get; set; } = new List<NBClip>();
    public bool IsVisible { get; set; } = true;

    public string FullName => $"[Track] {Name}";

    public int Measure()
    {
        int max = 0;

        for(int i = 0; i < Clips.Count; i++)
        {
            var clip = Clips[i];
            if (clip.StartFrame == null)
                clip.StartFrame = max;

            max = Math.Max(max, clip.StartFrame.Value + clip.DurationFrames);
        }

        return max;
    }

    public Control? Build(NBStage stage, int frame, bool includeStageBackground)
    {
        var panel = new Panel();
        panel.Width = stage.Width;
        panel.Height = stage.Height;
        
        if(includeStageBackground == true && stage.Background != null)
        {
            panel.Background = stage.Background;
        }

        foreach (var clip in Clips)
        {
            if (clip.IsVisible == false || clip.NeedRenderInTrack(frame) == false) continue;
            var ctrl = clip.Build(stage, frame - clip.StartFrame ?? 0, false);
            if(ctrl != null) panel.Children.Add(ctrl);
        }

        DrawingHelper.Layout(panel, stage.Width, stage.Height);

        return panel;
    }

}
