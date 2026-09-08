using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.ThreeD;

public class NBNode3DUpdateEvent
{
    public NBNode3D Sender { get; set; } = default!;

    public NBDrawContext Ctx { get; set; } = default!;

    public NBNode3DUpdateEvent(NBDrawContext Ctx)
    {
        this.Ctx = Ctx;
    }

    public T? SenderAs<T>() where T : NBNode3D
    {
        return Sender as T;
    }

    public double progress => Ctx.progress;

    public double p => Ctx.progress;

    public int frame => Ctx.frame;

    public float pf => (float)Ctx.progress;
}

public class NBTypedNode3DUpdateEvent<T> where T: NBNode3D
{
    public T Sender { get; set; } = default!;

    public NBDrawContext Ctx { get; set; } = default!;

    public NBTypedNode3DUpdateEvent(NBDrawContext Ctx)
    {
        this.Ctx = Ctx;
    }

    public T? SenderAs<T>() where T : NBNode3D
    {
        return Sender as T;
    }

    public double progress => Ctx.progress;

    public double p => Ctx.progress;

    public int frame => Ctx.frame;

    public float pf => (float)Ctx.progress;
}
