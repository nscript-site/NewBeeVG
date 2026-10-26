using SkiaSharp;

namespace NewBeeVG;

public class NBTrack : IPlayable
{
    public string Name { get; set; } = "track";
    public List<NBClip> Clips { get; set; } = new List<NBClip>();
    public bool IsVisible { get; set; } = true;

    public int DurationFrames { get; protected set; }

    public string FullName => $"[Track] {Name}";

    public bool ContainsVisualClip
    {
        get; private set;
    } = true;
    
    private int _frames = -1;

    public void Reset()
    {
        foreach (var item in Clips)
            item.Reset();
    }

    public int Measure()
    {
        if (_frames >= 0) return _frames;

        int max = 0;

        for(int i = 0; i < Clips.Count; i++)
        {
            var clip = Clips[i];
            if (clip.StartFrame == null && clip.ReferenceClip == null)
            {
                clip.StartFrame = max;
                max = Math.Max(max, clip.StartFrame.Value + clip.DurationFrames);
            }
        }

        for(int i = 0; i < Clips.Count; i++)
        {
            int count = 0;
            foreach (var clip in Clips)
            {
                if (clip.StartFrame == null && clip.ReferenceClip != null)
                {
                    count++;
                    var r = clip.ReferenceClip;
                    if(r.StartFrame != null)
                    {
                        clip.StartFrame = r.StartFrame.Value + clip.ReferenceClipStartFrame;
                        max = Math.Max(max, clip.StartFrame.Value + clip.DurationFrames);
                    }
                }
            }
            if (count == 0) break;
        }
        
        _frames = max;
        return max;
    }

    public virtual void Prepare(NBStage stage) { }

    public void Render(SKCanvas canvas, NBStage stage, int frame, bool includeStageBackground, bool preview)
    {
        if (ContainsVisualClip == false) return;
        {
            if (includeStageBackground == true)
            {
                stage.DrawBackground(canvas, preview);
            }

            foreach (var clip in Clips)
            {
                if (clip.IsVisible == false || clip.NeedRenderInTrack(frame) == false) continue;
                clip.Render(canvas, stage, frame - clip.StartFrame ?? 0, false, false);
            }
        }
    }

    internal StereoPcmDataSet ExportStereoPcm(double frameRate, int sampleRate)
    {
        StereoPcmDataSet ds = new StereoPcmDataSet() { SampleRate = sampleRate, FrameRate = frameRate };
        foreach(var clip in Clips)
        {
            if(clip.StartFrame != null)
            {
                var pcm = clip.ExportStereoPcm(sampleRate);
                if (pcm != null)
                {
                    var item = new StereoPcmData() { Frame = clip.StartFrame.Value, Pcm = pcm };
                    ds.Data.Add(item);
                }
            }
        }
        return ds;
    }
}
