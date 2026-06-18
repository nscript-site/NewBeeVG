using SkiaSharp;

namespace NewBeeVG;

public class NBTrack : IPlayable
{
    public string Name { get; set; } = "track";
    public List<NBClip> Clips { get; set; } = new List<NBClip>();
    public bool IsVisible { get; set; } = true;

    public string FullName => $"[Track] {Name}";

    public bool ContainsVisualClip
    {
        get; private set;
    } = true;
    
    private int _frames = -1;

    public int Measure()
    {
        if (_frames >= 0) return _frames;

        int max = 0;

        for(int i = 0; i < Clips.Count; i++)
        {
            var clip = Clips[i];
            if (clip.StartFrame == null)
                clip.StartFrame = max;

            max = Math.Max(max, clip.StartFrame.Value + clip.DurationFrames);
        }
        
        _frames = max;
        return max;
    }

    public virtual void Prepare() { }

    public virtual SKBitmap? Render(NBStage stage, int frame, bool includeStageBackground)
    {
        if (ContainsVisualClip == false) return null;

        var bitmap = new SKBitmap(stage.Width, stage.Height);
        using var canvas = new SKCanvas(bitmap);
        {
            if (includeStageBackground == true && stage.Background != null)
            {
                var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = stage.Background.Value
                };
                canvas.DrawRect(0, 0, stage.Width, stage.Height, paint);
            }
            foreach (var clip in Clips)
            {
                if (clip.IsVisible == false || clip.NeedRenderInTrack(frame) == false) continue;
                var bmp = clip.Render(stage, frame - clip.StartFrame ?? 0, false);
                if (bmp != null)
                    canvas.DrawBitmap(bmp, 0, 0);
            }
        }
        return bitmap;
    }
}
