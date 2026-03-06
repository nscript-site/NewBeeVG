#!/usr/bin/env dotnet

// 所有缓动函数的例子

#:property PublishAot=false
#:project ../../sources/NewBeeVG/NewBeeVG.csproj

using Avalonia.Media;
using NewBeeVG;
using static NewBeeVG.Methods;
using SkiaSharp;

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
        }
    );

    clips.Add(newClip);
}

run(stage(width: 1920, height: 1080, bg: Brushes.Black), clips);
