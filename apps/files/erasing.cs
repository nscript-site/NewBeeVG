#!/usr/bin/env dotnet

// #:project ../../sources/NewBeeVG/NewBeeVG.csproj

// 所有缓动函数的例子
var erasings = Easing.All();
var clips = new List<NBClip>();
foreach (var e in erasings)
{
    var newClip = skclip(
        name: $"{e.Item1}",
        frames: 15,
        builder: (ctx, clip, canvas) =>
        {
            var erasing = e.Item2;
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White,
                IsAntialias = true
            };
            var radius = 60;
            var x = ctx.width / 2 - 500 + 1000 * erasing(ctx.progress);
            canvas.DrawCircle((float)x, ctx.height / 2, (float)radius, paint);
            
            using var textPaint = new SKPaint
            {
                Color = SKColors.White,          // 文本颜色
                TextSize = 48,                   // 字体大小（像素）
                IsAntialias = true,              // 抗锯齿（必开，否则文字有锯齿）
                TextAlign = SKTextAlign.Left     // 文本对齐方式（左对齐）
            };
            canvas.DrawText(e.Item1, new SKPoint(100,100),textPaint);
        }
    );

    clips.Add(newClip);
}

run(stage(width: 1920, height: 1080, bg: Brushes.Black), clips);
