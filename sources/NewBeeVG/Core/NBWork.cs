using SkiaSharp;

namespace NewBeeVG;

public class NBWork : IPlayable
{
    public string Name { get; set; } = "work";
    public NBStage Stage { get; set; } = new NBStage();
    public List<NBTrack> Tracks { get; set; } = new List<NBTrack>();

    public string FullName => $"[Work] {Name}";

    private int _frames = -1;

    public double Fps => Stage.FrameRate;

    public void Reset()
    {
        foreach (var item in Tracks)
            item.Reset();
    }

    public int Measure()
    {
        if(_frames >= 0) return _frames;
        int max = 0;
        foreach (var track in Tracks)
            max = Math.Max(max, track.Measure());
        _frames = max;
        return max;
    }

    public StereoPcmData? MixedAudioData { get; protected set; }

    public virtual void Prepare(NBStage stage)
    {
        Measure();
        StereoPcmDataSet ds = new StereoPcmDataSet() { SampleRate = stage.AudioFrameRate, FrameRate = stage.FrameRate };
        foreach (var track in Tracks)
        {
            var mixed = track.MixedAudioData;
            if (mixed != null)
            {
                ds.Data.Add(mixed);
            }
        }
        MixedAudioData = ds.Mixing();
    }

    public SKBitmap CreateBitmap()
    {
        var bitmap = new SKBitmap(Stage.Width, Stage.Height);
        return bitmap;
    }

    public virtual void Render(SKCanvas canvas, NBStage stage, int frame, bool includeStageBackground, bool preview)
    {
        if (includeStageBackground == true)
        {
            stage.DrawBackground(canvas, preview);
        }

        foreach (var track in Tracks)
        {
            if (track.IsVisible == false) continue;
            track.Render(canvas, stage, frame, false, false);
        }
    }
}
