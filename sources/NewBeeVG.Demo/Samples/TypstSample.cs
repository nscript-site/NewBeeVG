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

        var content = """
            #t1[2025年，中国对外直接投资流量2135.8亿美元，比上年增长11.1%，对外投资存量3.4万亿美元，连续9年保持全球前三，占全球投资的比重增加到7.4%。对外投资行业覆盖国民经济的18个行业门类，主要集中在租赁和商务服务、批发零售、制造和金融四个领域，近年来逐步向绿色低碳、数字经济和绿色矿产等领域稳步拓展。]

            #t2[截至2025年末，我国在境外设立企业5.8万家，遍布189个国家和地区，境外企业从业员工总数476.1万人，其中雇用外方员工296.5万人。]

            #t3[2025年，中国企业对共建“一带一路”国家直接投资460.5亿美元，占当年对外投资流量的21.6%，在共建“一带一路”国家设立境外企业数量2.2万家，投资存量4072.5亿美元。]
            """;

        VStack([
            TextBlock("TypstContent 直接嵌入内容").Margin(10),
                    TypstContent(content).Width(800).PageMargin(0).PageSize(300,null)
                        .Seg("t1",SKColors.Red).Seg("t2",SKColors.Green).Seg("t3",SKColors.Blue)
                        .Align(0,-1).Margin(100)
        ]).Margin(100)
        .AsClip(out var clip3, 30, name: "typstcontent");

        var math = """
                   $ #t1[A] #t2[=] #t3[pi r^2] $
                   $ "area" = pi dot "radius"^2 $
                   $ cal(A) :=
                       { x in RR | x "is natural" } $
                   #let x = 5
                   $ #x < 17 $
                   """;
        VStack([
            TextBlock("TypstMath 直接嵌入数学公式").Margin(10),
            TypstMath(math).PageMargin(0).Ref(out var tm)
                .OnFrame(e=>{
                    SKColor color = SKColors.Red;
                    byte alpha1 = 255;
                    byte alpha2 = (byte)(e.frame < 10 ? 0 : 255);
                    byte alpha3 = (byte)(e.frame < 20 ? 0 : 255);
                    tm.Seg("t1",color,alpha1);
                    tm.Seg("t2",color,alpha2);
                    tm.Seg("t3",color,alpha3);
                })
                .Align(0,-1).Margin(100)
        ]).Margin(100)
        .AsClip(out var clip4, 30, name: "typstmath");

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
            TypstCode(code,600, "cs", boxBg: SKColors.LightGray).PageMargin(0)
                .Align(0,-1).Margin(0)
        ]).Margin(10)
        .AsClip(out var clip5, 30, name: "typstcode");

        run(stage(bg: SKColors.Orange), [clip1, clip2, clip3, clip4, clip5]);
    }
}
