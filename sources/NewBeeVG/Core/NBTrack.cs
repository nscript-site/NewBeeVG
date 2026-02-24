using Avalonia.Media.Imaging;

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

    public bool Render(RenderTargetBitmap bitmap, int frame)
    {
        foreach (var clip in Clips)
        {
            if (clip.IsVisible == false || clip.NeedRenderInTrack(frame) == false) continue;
            clip.Render(bitmap, frame - clip.StartFrame??0);
        }
        return true;
    }

}
