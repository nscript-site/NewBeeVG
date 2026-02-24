namespace NewBeeVG;

public struct NBBuildContext
{
    public int frame;
    public int width;
    public int height;
    public int dpi;
}

/// <summary>
/// 视频片段
/// </summary>
public class NBClip
{
    public int? StartFrame { get; internal set; }

    public int DurationFrames { get; set; } = 1;

    public string Name { get; init; }

    public bool IsVisible { get; set; } = true;

    public Func<NBBuildContext, NBClip, Control?>? Builder { get; internal set; }

    public NBClip(string name = "clip", Func<NBBuildContext, NBClip, Control?>? builder = null, int duration = 1, int? start = null)
    {
        Builder = builder;
        DurationFrames = Math.Max(1,duration);
        StartFrame = start;
    }

    public bool NeedBuild(int frameIndex)
    {
        if (Builder == null) return false;
        return true;
    }

    public Control? Build(int frame, NBStage stage)
    {
        if (Builder == null) return null;
        var context = new NBBuildContext
        {
            frame = frame,
            width = stage.Width,
            height = stage.Height,
            dpi = stage.Dpi
        };
        return Builder(context, this);
    }
}
