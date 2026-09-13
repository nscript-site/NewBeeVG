#!/usr/bin/env dotnet

// 通过函数创建补间动画
var clip1 = clip(
    name: "clip1",
    frames: 30,
    builder: (ctx, clip) =>
    {
        var easing = Easing.SineInOut;
        float v = (float)easing(ctx.progress);
        return
        VecPath(RectPath(0,0,200,200),CirclePath(100,100,100), v, SKColors.Green).Align(0,0);
    }
);

// 通过 MarkUp 创建补间动画
VecPath(SKColors.Green).Ref(out var v2)
.OnFrame(e=>v2.Path(RectPath(0,0,200,200),CirclePath(100,100,100),e.pf))
.Align(0,0)
.AsClip(out var clip2, 30, name: "clip2");

run(stage(bg: SKColors.Orange), [clip1, clip2]);