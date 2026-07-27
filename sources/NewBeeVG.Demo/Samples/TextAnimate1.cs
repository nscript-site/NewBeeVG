namespace NewBeeVG.Demo.Samples;

internal class TextAnimate1
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        var style = (NBVisual v) => { 
            v.As<NBText>()?
                .AddStroke(SKColors.Orange, 55).AddStroke(SKColors.White, 50).AddStroke(SKColors.Red, 40)
                .FontSize(120).Foreground(SKColors.Black).Align(0, 0).LetterSpacing(10)
                .OnFrame(e => { e.SenderAs<NBText>()?.LetterSpacing(-80 + 100 * e.pf).InvalidateMeasure(); })
                ;
        };

        var content = () => 
            VStack([
                TextBlock("求关注").Styles(style).Id("Text").FrameMask(FrameMasks.Scanlines()),
            ]).Spacing(0)
            .Align(0, 0);

        content().AsClip(out var clip1, 30, name: "qiuguanzhu 1");


        var style2 = (NBVisual v) => {
            v.As<NBText>()?
                .AddStroke(SKColors.Orange, 55).AddStroke(SKColors.White, 50).AddStroke(SKColors.Red, 40)
                .FontSize(120).Foreground(SKColors.Black).Align(0, 0)
                .OnFrame(e =>
                {
                    var txt = e.SenderAs<NBText>()?.Text ?? "";
                    var idx = "求关注".IndexOf(txt);
                    if (idx < 0) idx = 0;
                    var frame = e.frame % 10;
                    var animateIdx = e.frame % 30 / 10;
                    if (animateIdx == idx)
                    {
                        float p = frame / 10.0f;
                        p = (float)Easing.BackIn(p);
                        e.Sender.RenderTransform(SKMatrix.CreateScale(1 + 0.2f * p, 1 + 0.2f * p));
                    }
                    else
                    {
                        e.Sender.RenderTransform();
                    }
                });
        };

        var content2 =  () => 
            VStack([
                HStack([
                        TextBlock("求").Styles(style2),
                        TextBlock("关").Styles(style2),
                        TextBlock("注").Styles(style2),
                    ]).Spacing(20).FrameMask(FrameMasks.Scanlines()),
            ]).Spacing(0)
            .Align(0, 0);

        content2().AsClip(out var clip2, 30, name: "qiuguanzhu 2");

        VStack([
            Layer([
                TextBlock("求关注").Styles(style).ClearOnFrames().Id("Text"),
                Rect().Bind("Text")
                    .OnFrame(e=>e.Sender.Shaders(Shaders.AlphaLinearGradient(e.p)))
            ]).Size(800,double.NaN),
        ]).Spacing(10).Align(0, 0)
        .AsClip(out var clip3, 30, name: "qiuguanzhu 3");

        Panel([
            content(),
            Ellipse(2400,2400).Align(0,0)
                .OnFrame(e=>
                {
                    float v = 1 - (float)Easing.CubicOut(e.p);
                    var shader = Shaders.RadialGradientOnRect([SKColors.Transparent, SKColors.Transparent, SKColors.Black, SKColors.Black],[0, v - 0.04f, v, 1]);
                    e.Sender.Shaders(shader);
                })
            ]).AsClip(out var clip4, 30, name: "qiuguanzhu 4");

        TextBlock("Demo").Foreground(SKColors.Orange).Align(1, -1).Margin(20)
            .AsClip(out var logo, -1, 0, "logo");

        run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3, clip4, logo]);
    }
}
