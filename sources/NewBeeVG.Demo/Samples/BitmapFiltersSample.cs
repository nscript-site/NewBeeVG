using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class BitmapFiltersSample
{
    public static void Run()
    {
        VStack([
            TextBlock("Code").Font(80, SKColors.Orange).Align(0,0),
            TypstFile("./Assets/code1.typ")
            .MaxHeight(800).Align(0,0)
        ]).BitmapFilters(new DemoBitmapFilter()).Align(0, 0).AsClip(out var clip1, frames: 40, name: "code");

        run(stage(1920, 1080, bg: SKColors.White), [clip1]);
    }
}

internal class DemoBitmapFilter : NBBitmapFilter
{
    public override (SKBitmap?, SKPoint) Filter(NBDrawContext ctx, SKRect rect, SKBitmap? bitmap)
    {
        if(bitmap == null) return (null, new SKPoint(0, 0));
        var c = new SKCanvas(bitmap);
        c.DrawText(ctx.frame.ToString(), 50, 50, new SKFont(SKTypeface.Default, 40), new SKPaint() { Color = SKColors.Red });
        return (bitmap, new SKPoint(0, 0));
    }
}
