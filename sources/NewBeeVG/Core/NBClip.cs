namespace NewBeeVG;

/// <summary>
/// 视频片段
/// </summary>
public class NBClip : IPlayable
{
    /// <summary>
    /// 如果为 null, 则表示自动衔接在前一个 clip 之后
    /// </summary>
    public int? StartFrame { get; internal set; }

    /// <summary>
    /// duration frames 如果 <= 0, 则表示持续到最后一帧
    /// </summary>
    public int DurationFrames { get; set; }

    public Func<NBDrawContext, NBClip, Control?>? Builder { get; internal set; }

    public string Name { get; init; }

    public bool IsVisible { get; set; } = true;

    public NBClip(string name = "clip", Func<NBDrawContext, NBClip, Control?>? builder = null, int duration = 1, int? start = null)
    {
        Name = name;
        Builder = builder;
        DurationFrames = duration;
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

    public Control? Build(NBStage stage, int frame, bool includeStageBackground)
    {
        if (Builder == null) return null;

        var content = BuildCore(stage, frame);
        if (content == null) return null;

        if (includeStageBackground == false) return content;

        var panel = new Panel();
        panel.Width = stage.Width;
        panel.Height = stage.Height;

        if (includeStageBackground == true && stage.Background != null)
        {
            panel.Background = stage.Background;
        }

        panel.Children.Add(content);

        DrawingHelper.Layout(panel, stage.Width, stage.Height);

        return panel;
    }

    private Control? BuildCore(NBStage stage, int frame)
    {
        if (Builder == null) return null;

        var context = new NBDrawContext
        {
            frame = frame,
            width = stage.Width,
            height = stage.Height,
            dpi = new Vector(stage.Dpi, stage.Dpi),
            progress = CalculateProgress(frame, DurationFrames)
        };

        var content = Builder(context, this);
        if (content == null) return null;

        content.Measure(new Size(context.width, context.height));
        content.Arrange(new Rect(0, 0, context.width, context.height));
        content.UpdateLayout();

        return content;
    }

    internal bool NeedRenderInTrack(int frame)
    {
        if (Builder == null) return false;
        
        if (StartFrame == null || DurationFrames <= 0) return true;
        else return frame >= StartFrame && frame < StartFrame + DurationFrames;
    }
}
