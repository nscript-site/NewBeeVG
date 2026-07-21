namespace NewBeeVG.Demo.Samples;

internal class TextAnimate1
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        var content = () =>
            {
                return
                    VStack([
                        TextBlock("输入你的文字").FontSize(120).Foreground(SKColors.Black).Align(0,0).Id("Text"),
                             TextBlock("……").FontSize(120).Foreground(SKColors.Black).Align(-1,-1).Id("Text2")
                    ]).Spacing(0)
                    .Align(0, 0);
            };

        var clip1 = clip(
             name: "animate",
             frames: 40, blend: SKBlendMode.SrcOut,
             builder: (ctx, clip) => {
                 float v = (float)ctx.progress;
                 var shader = (SKRect rect) =>
                 {
                     return SKShader.CreateAlphaLinearGradient(rect.LeftMiddle, rect.RightMiddle,
                         [0 - 0.4f, (v - 0.4f) / 0.6f, 0.1f + (v - 0.4f) / 0.6f, 1 + 0.2f],
                         [0, 0, 1, 1]);
                 };
                 return
                   VStack([
                       Layer([
                           TextBlock("输入你的文字").FontSize(120).Foreground(SKColors.Black).Align(0,0).Id("Text"),
                           Rect().Bind("Text").Shader(shader)
                       ]).Size(800,200),
                       Layer([TextBlock("……").FontSize(120).Foreground(SKColors.Black).Align(-1,-1).Id("Text2"),
                            Rect().Bind("Text2").Shader(shader)
                       ]).Size(800,200),
                   ]).Spacing(10)
                   .Align(0, 0);
             }
         );

        var clip2 = clip(
             name: "fixed",
             frames: 30,
             builder: (ctx, clip) => content()
         );

        var clip3 = clip(
             name: "animate2",
             frames: 30,
             builder: (ctx, clip) =>
             {
                 float v = 1 - (float)Easing.CubicOut(ctx.progress);

                 var shader = (SKRect rect) => 
                    SKShader.CreateRadialGradient(rect, SKColors.Transparent, SKColors.Black, v - 0.04f, v);

                 return
                 Panel([
                     content(),
                     Ellipse(2400,2400).Align(0,0).Shader(shader),
                 ]);
             }
         );

        var logo = clip(
            name: "logo",
            start: 0,
            frames: -1,
            builder: (ctx, clip) =>
            {
                return
                    TextBlock("Demo").Foreground(SKColors.Orange).Align(1, -1).Margin(20);
            }
        );

        run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3, logo]);
    }
}
