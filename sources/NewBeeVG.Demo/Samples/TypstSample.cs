using NewBeeVG.Core.Controls;

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
                VStack([
                    TextBlock("通过函数来创建 typst 动画").Margin(10),
                    TypstFile("./Assets/page1.typ")
                        .Align(0,0).Margin(100)
                        .TypstInputs("frames",ctx.frame)
                    ]).Margin(100).Background(SKColors.DeepSkyBlue);
            }
        );

        VStack([
            TextBlock("通过 Markup 来创建 typst 动画").Margin(10),
            TypstFile("./Assets/page1.typ").Ref(out var typ2)
                .Align(0,0).Margin(100)
                .OnFrame(e => typ2.UpdateInputs("frames", e.frame))
        ]).Margin(100).Background(SKColors.DeepSkyBlue)
        .AsClip(out var clip2, 30, name: "typst2");

        var math = """
                   $ A = pi r^2 $
                   $ "area" = pi dot "radius"^2 $
                   $ cal(A) :=
                       { x in RR | x "is natural" } $
                   #let x = 5
                   $ #x < 17 $
                   """;
        VStack([
            TextBlock("TypstMath 直接嵌入数学公式").Margin(10),
            TypstMath(math).PageMargin(0)
                .Align(0,-1).Margin(100)
        ]).Margin(100)
        .AsClip(out var clip3, 30, name: "typstmath");

        var code = """
                   #!/usr/bin/env dotnet

                   VGrid($"*", [
                       TypstFile("./typst/page1.typ").Ref(out var typ)
                           .Align(0,0)
                           .OnFrame(e=> typ.UpdateInputs("frames", e.frame))
                       ]).Background(SKColors.DeepSkyBlue)
                       .AsClip(out var clip, 30, name: "typst");

                   run(stage(bg: SKColors.Orange), [clip]);
                   """;
        VStack([
            TextBlock("TypstCode 直接嵌入代码").Margin(10),
            TypstCode(code,600, "csharp").PageMargin(0)
                .Align(0,-1).Margin(0)
        ]).Margin(10)
        .AsClip(out var clip4, 30, name: "typstcode");

        run(stage(bg: SKColors.Orange), [clip1, clip2, clip3, clip4]);
    }
}
