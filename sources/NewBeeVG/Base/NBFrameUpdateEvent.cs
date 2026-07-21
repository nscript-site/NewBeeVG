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

    public double progress => Ctx.progress;

    public double p => Ctx.progress;

    public int frame => Ctx.frame;

    public float pf => (float)Ctx.progress;
}
