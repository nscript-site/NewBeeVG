using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class WidgetsSample
{
    public static void Run()
    {
        //var clip1 = clip(
        //name: "clip1",
        //frames: 30,
        //builder: (ctx, clip) =>
        //{
        //    var easing = Easing.SineInOut;
        //    double v = easing(ctx.progress) * 2 * Math.PI;
        //    var r = 900;
        //    var x = r * Math.Sin(v);
        //    var y = r * Math.Cos(v);
        //    return
        //    HGrid("*", [
        //        TextBlock("Clip1")
        //            .Align(0,0)
        //            .Margin(x, y, 0,0).FontSize(200)
        //        ]);
        //}
        //);

        //var clip2 = clip(
        //    name: "clip2",
        //    frames: 30,
        //    builder: (ctx, clip) =>
        //    {
        //        var easing = Easing.SineInOut;
        //        double v = easing(ctx.progress);

        //        return
        //        HGrid("*", [
        //            TextBlock("Clip2")
        //        .Align(0,-1)
        //        .Margin(0, 100 + (ctx.height - 500) * v, 0,0).FontSize(200)
        //            ]);
        //    }
        //);

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

        //var clip2 = clip(
        //    name: "clip2",
        //    frames: 30,
        //    builder: (ctx, clip) =>
        //    {
        //        var easing = Easing.SineInOut;
        //        double v = easing(ctx.progress);

        //        return
        //        Panel([
        //                TextBlock("Clip2").FontSize(200)
        //                .Align(0,-1)
        //                .Margin(0, 100 + (ctx.height - 500) * v, 0,0),

        //                HStack([
        //                    TextBlock("AAAA"),
        //                    TextBlock("BBBB")
        //                    ])
        //                .Background(SKColors.Red)
        //                .Align(0,1)
        //                .Margin(100)
        //            ]);
        //    }
        //);

        run(stage(bg: SKColors.Orange), [clip1]);
    }
}
