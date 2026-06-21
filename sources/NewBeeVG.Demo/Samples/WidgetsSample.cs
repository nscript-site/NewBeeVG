using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class WidgetsSample
{
    public static void Run()
    {
        var clip1 = clip(
           name: "clip1",
           frames: 30,
           builder: (ctx, clip) =>
           {
               return
               Panel([
                        WrapPanel([
                            TextBlock("BBBB").Margin(100),
                            TextBlock("BBBB").Margin(100),
                            TextBlock("BBBB").Margin(100),
                            TextBlock("BBBB").Margin(100),
                            TextBlock("BBBB").Margin(100),
                            TextBlock("BBBB").Margin(100),
                            ])
                        .Background(SKColors.Red)
                        .Align(0,1)
                        .Margin(100)
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
                double h = 100 + (ctx.height - 100) * v;
                return
                VGrid($"*,{h}",[
                        null,
                        HGrid("200, *", [
                            TextBlock("AAAA").Align(0,0),
                            TextBlock("BBBB").Align(0,-1).Margin(0,h-100,0,0)
                            ])
                        .Background(SKColors.Red)
                    ]).Background(SKColors.DeepSkyBlue);
            }
        );

        var clip3 = clip(
            name: "clip_image",
            frames: 30,
            builder: (ctx, clip) =>
            {
                var easing = Easing.SineInOut;
                double v = easing(ctx.progress);
                double h = 100 + (ctx.height - 100) * v;
                return
                VGrid($"*,{h}", [
                        null,
                        Image("./Assets/snows.jpg").Align(0,-1).Size(300,200).Stretch(Stretch.Fill)
                    ]).Background(SKColors.DeepSkyBlue);
            }
        );

        run(stage(bg: SKColors.Orange), [clip1, clip2, clip3]);
    }
}
