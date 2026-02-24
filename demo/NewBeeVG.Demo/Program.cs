
var clip1 = clip(
    name: "clip1",
    frames: 5,
    builder: (ctx, clip) =>
    {
        return
        HGrid("*", [
            TextBlock("Clip1")
                .Margin(2 * ctx.frame, 2 * ctx.frame, 0,0).FontSize(40)
            ]);
    }
);

var clip2 = clip(
    name: "clip2",
    frames: 10,
    builder: (ctx, clip) =>
    {
        return
        HGrid("*", [
            TextBlock("Clip2")
                .Margin(2 * ctx.frame, 2 * ctx.frame, 0,0).FontSize(40)
            ]);
    }
);

run(stage(), [clip1, clip2]);

