namespace NewBeeVG.Demo.Samples;

internal class LayerSample
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        VStack([
            Layer([
                TextBlock("输入你的文字").Font(120, SKColors.Black).Align(0,0).Id("Text"),
                Rect().Bind("Text").Shader(AlphaLinearGradientShader())
            ]),
            TextBlock("输入你的文字").Font(120, SKColors.Black)
                .OnFrame(
                    e=>
                    {
                        e.Sender.Opacity(e.p); 

                        e.SenderLayoutable?.Margin(0,e.p * 200,0,0);
                    }
                )
        ])
        .Align(0, 0)
        .AsClip(out var clip1, frames: 40, name: "animate");

        VStack([
            TextBlock("Code").Font(80, SKColors.Orange).Align(0,0),
            TypstFile("./Assets/code1.typ")
            .MaxHeight(800).Align(0,0)
        ]).Align(0,0).AsClip(out var clip2, frames: 40, name: "code");

        run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2]);
    }
}
