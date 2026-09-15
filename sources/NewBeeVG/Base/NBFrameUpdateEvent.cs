namespace NewBeeVG;

public class NBFrameUpdateEvent
{
    public NBVisual Sender { get; set; } = default!;

    public NBDrawContext Ctx { get; set; } = default!;

    public NBFrameUpdateEvent(NBDrawContext Ctx)
    {
        this.Ctx = Ctx;
    }

    public NBLayoutable? SenderLayoutable => Sender as NBLayoutable;

    public T? SenderAs<T>() where T : NBVisual
    {
        return Sender as T;
    }

    /// <summary>
    /// 在 clip 中的进度，double 值
    /// </summary>
    public double progress => Ctx.progress;

    /// <summary>
    /// 在 clip 中的进度，double 值
    /// </summary>
    public double p => Ctx.progress;

    public byte pb => (byte)(Ctx.progress * 255);

    /// <summary>
    /// 在 clip 中的帧编号
    /// </summary>
    public int frame => Ctx.frame;

    public int durationFrames => Ctx.durationFrames;

    /// <summary>
    /// 在 clip 中的进度，float 值
    /// </summary>
    public float pf => (float)Ctx.progress;

    public int w => Ctx.width;
    public int h => Ctx.height;

    public int width => Ctx.width;
    public int height => Ctx.height;
}
