namespace NewBeeVG.Demo.Samples;

internal class LayerSample
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        var clip1 = clip(
             name: "animate",
             frames: 40,
             builder: (ctx, clip) => {
                 float v = (float)ctx.progress;
                 var shader = (SKRect rect) =>
                 {
                     return SKShader.CreateAlphaLinearGradient(rect.LeftMiddle, rect.RightMiddle,
                         [0 - 0.4f, (v - 0.4f) / 0.6f, 0.1f + (v - 0.4f) / 0.6f, 1 + 0.2f],
                         [0, 0, 1, 1]);
                 };
                 return
                   Panel([
                       Layer()
                            .Source(TextBlock("输入你的文字").Font(120, SKColors.Black).Align(0,0).Id("Text"))
                            .Mask(Rect().Bind("Text").Shader(shader))])
                   .Align(0, 0);
             }
         );

        run(stage(1920, 1080, bg: SKColors.White), [clip1]);
    }
}
