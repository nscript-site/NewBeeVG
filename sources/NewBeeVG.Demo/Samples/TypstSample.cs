using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class TypstSample
{
    public static void Run()
    {
        // 通过函数来创建 typst 动画
        var clip1 = clip(
            name: "typst",
            frames: 30,
            builder: (ctx, clip) =>
            {
                return
                VGrid($"*", [
                    TextBlock("通过函数来创建 typst 动画").Margin(10),
                    TypstFile("./Assets/page1.typ")
                        .Align(0,0).Margin(100)
                        .TypstInputs("frames",ctx.frame)
                    ]).Margin(100).Background(SKColors.DeepSkyBlue);
            }
        );

        VGrid($"*", [
            TextBlock("通过 Markup 来创建 typst 动画").Margin(10),
            TypstFile("./Assets/page1.typ").Ref(out var typ2)
                .Align(0,0).Margin(100)
                .OnFrame(e => typ2.UpdateInputs("frames", e.frame))
        ]).Margin(100).Background(SKColors.DeepSkyBlue)
        .AsClip(out var clip2, 30, name: "typst2");

        run(stage(bg: SKColors.Orange), [clip1, clip2]);
    }
}
