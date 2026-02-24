
var clip1 = clip(
    name: "clip1",
    frames: 30,
    builder: (ctx, clip) =>
    {
        var easing = Easing.SineInOut;
        double v = easing(ctx.progress) * 2 * Math.PI;
        var r = 200;
        var x = r * Math.Sin(v);
        var y = r * Math.Cos(v);
        return
        HGrid("*", [
            TextBlock("Clip1")
                .Align(0,0)
                .Margin(x, y, 0,0).FontSize(200)
            ]);
    }
);

var clip2 = clip(
    name: "clip2",
    frames: 30,
    builder: (ctx, clip) =>
    {
        var easing = Easing.SineInOut;        
        double v = easing(ctx.progress);

        return
        HGrid("*", [
            TextBlock("Clip2")
                .Align(0,-1)
                .Margin(0, 100 + (ctx.height - 500) * v, 0,0).FontSize(200)
            ]);
    }
);

run(stage(), [clip1, clip2]);

