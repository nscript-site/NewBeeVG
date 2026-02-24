using Avalonia.Media.Imaging;

namespace NewBeeVG;

public struct NBBuildContext
{
    public int frame;
    public int width;
    public int height;
    public double progress;
    public Vector dpi;
}

/// <summary>
/// 视频片段
/// </summary>
public class NBClip : IPlayable
{
    public int? StartFrame { get; internal set; }

    public int DurationFrames { get; set { field = Math.Max(1, value); } } = 1;

    public string Name { get; init; }

    public bool IsVisible { get; set; } = true;

    public Func<NBBuildContext, NBClip, Control?>? Builder { get; internal set; }

    public NBClip(string name = "clip", Func<NBBuildContext, NBClip, Control?>? builder = null, int duration = 1, int? start = null)
    {
        Name = name;
        Builder = builder;
        DurationFrames = Math.Max(1,duration);
        StartFrame = start;
    }

    public string FullName => $"[Clip] {Name}";

    public int Measure()
    {
        return DurationFrames; 
    }

    private static double CalculateProgress(int frame, int count)
    {
        // 校验输入合法性（避免负数/0帧等异常情况）
        if (count <= 1 || frame < 0) return 0;
        else if(frame >= count) return 1;

        return frame /(count - 1.0);
    }

    public bool Render(RenderTargetBitmap bitmap, int frame)
    {
        if (Builder == null) return false;

        var context = new NBBuildContext
        {
            frame = frame,
            width = bitmap.PixelSize.Width,
            height = bitmap.PixelSize.Height,
            dpi = bitmap.Dpi,
            progress = CalculateProgress(frame, DurationFrames)
        };

        var content = Builder(context, this);
        if(content == null) return false;

        content.Measure(new Size(context.width, context.height));
        content.Arrange(new Rect(0, 0, context.width, context.height));
        content.UpdateLayout();
        
        bitmap.Render(content);

        return true;
    }

    internal bool NeedRenderInTrack(int frame)
    {
        if (Builder == null) return false;
        
        if (StartFrame == null) return true;
        else return frame >= StartFrame && frame < StartFrame + DurationFrames;
    }
}
