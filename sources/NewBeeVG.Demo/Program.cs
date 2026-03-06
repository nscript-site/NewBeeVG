var clip1 = clip(
    name: "clip1",
    frames: 30,
    builder: (ctx, clip) =>
    {
        var easing = Easing.SineInOut;
        double v = easing(ctx.progress) * 2 * Math.PI;
        var r = 900;
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

var clip3 = skclip(
    name: "skclip",
    frames:10,
    builder: (ctx, clip, canvas) =>
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Red.WithAlpha((byte)(ctx.progress * 255)),
                IsAntialias = true,
            };
            var radius = 100 + 200 * ctx.progress;
            canvas.DrawCircle(ctx.width / 2, ctx.height / 2, (float)radius, paint);
        }
    );

var logo = clip(
    name: "logo",
    start: 0,
    frames: -1,
    builder: (ctx, clip) =>
    {
        return
            TextBlock("Demo").Align(1, -1).FontSize(40).Margin(20);
    }
);

var logo2 = clip(
    name: "logo2",
    start: 0,
    frames: -1,
    builder: (ctx, clip) =>
    {
        return
            TextBlock("NewBee VG").Align(-1, -1).FontSize(40).Margin(20);
    }
);

run(stage(bg: Brushes.White), [clip1, clip2, clip3, logo, logo2]);

