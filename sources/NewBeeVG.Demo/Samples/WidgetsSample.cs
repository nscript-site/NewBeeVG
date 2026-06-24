using System.Runtime.CompilerServices;

namespace NewBeeVG.Demo.Samples;

internal class WidgetsSample
{
    public static void Run([CallerFilePath] string filePath = "")
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
                            null,
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

        var svg = $"""
                    <svg xmlns="http://www.w3.org/2000/svg" width="300" height="200" viewBox="0 0 300 200">            
                        <rect width="100" height="100" fill="#f5f5f5"/>
                        <text x="140" y="108" font-size="24" fill="#222">Hello SVG</text>
                    </svg>
                    """;

        var clip4 = clip(
            name: "svg",
            frames: 30,
            builder: (ctx, clip) =>
            {
                return
                VGrid($"*", [
                        SVG(svg).Align(0,0)
                    ]).Background(SKColors.DeepSkyBlue);
            }
        );

        var clip5 = clip(
            name: "typst",
            frames: 30,
            builder: (ctx, clip) =>
            {
                return
                VGrid($"*", [
                        TypstFile("./Assets/page1.typ")
                            .Align(0,0)
                            .TypstInputs(x=>{x["frames"] = $"{ctx.frame}"; })
                        ]).Background(SKColors.DeepSkyBlue);
            }
        );

        Utils.EmbedPython(filePath);
        dynamic m = py_module("./Assets/plot.py");

        var clip6 = clip(
            name: "pyclip",
            frames: 30,
            builder: (ctx, clip) =>
            {
                SKBitmap bmp;
                using (py_gil())
                {
                    var img = m.plot_3d_data(ctx.progress * 2 * Math.PI);
                    bmp = py_imdecode(img);
                }

                return
                VGrid($"*", [
                        Image(bmp)
                            .Align(0,0)
                        ]).Background(SKColors.DeepSkyBlue);
            }
        );

        run(stage(bg: SKColors.Orange), [clip1, clip2, clip3, clip4, clip5, clip6]);
    }
}
