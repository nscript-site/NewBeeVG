namespace NewBeeVG.Demo.Samples;

internal class TextAnimate1
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        var style = (NBVisual v) => { 
            v.As<NBText>()?
                .AddStroke(SKColors.Orange, 55).AddStroke(SKColors.White, 50).AddStroke(SKColors.Red, 40)
                .FontSize(120).Foreground(SKColors.Black).Align(0, 0)
                .OnFrame(e => { e.SenderAs<NBText>()?.LetterSpacing(10 + 100 * e.pf).InvalidateMeasure(); });
        };

        var content = () => {
            return
                VStack([
                    TextBlock("输入你的文字").Styles(style).Id("Text"),
                    TextBlock("……").FontSize(120).Foreground(SKColors.Black).Align(0,-1).Id("Text2")
                ]).Spacing(0)
                .Align(0, 0);
        };

        content().AsClip(out var clip1, 30, name: "fix");

        VStack([
            Layer([
                TextBlock("输入你的文字").Styles(style).ClearOnFrames().Id("Text"),
                Rect().Bind("Text")
                    .OnFrame(e=>e.Sender.Shaders(Shaders.AlphaLinearGradient(e.p)))
            ]).Size(800,double.NaN),
            Layer([
                TextBlock("……").FontSize(120).Foreground(SKColors.Black).Align(0,-1).Id("Text2"),
                Rect().Bind("Text2")
                    .OnFrame(e=>e.Sender.Shaders(Shaders.AlphaLinearGradient(e.p)))
            ]).Size(800,200),
        ]).Spacing(10).Align(0, 0)
        .AsClip(out var clip2, 30, name: "animate1");

        Panel([
            content(),
            Ellipse(2400,2400).Align(0,0)
                .OnFrame(e=>
                {
                    float v = 1 - (float)Easing.CubicOut(e.p);
                    var shader = Shaders.RadialGradientOnRect([SKColors.Transparent, SKColors.Transparent, SKColors.Black, SKColors.Black],[0, v - 0.04f, v, 1]);
                    e.Sender.Shaders(shader);
                })
            ]).AsClip(out var clip3, 30, name: "animate2");

        TextBlock("Demo").Foreground(SKColors.Orange).Align(1, -1).Margin(20)
            .AsClip(out var logo, -1, 0, "logo");

        run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3, logo]);
    }
}
