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
             builder: (ctx, clip) => content(),
             mask: (ctx, clip) =>
             {
                 float v = (float)ctx.progress;
                 var shader = (SKRect rect) =>
                 {
                     return SKShader.CreateAlphaLinearGradient(rect.LeftMiddle, rect.RightMiddle, 
                         [0-0.4f, (v-0.4f)/0.6f, 0.1f + (v - 0.4f) / 0.6f, 1 + 0.2f], 
                         [0, 0, 1, 1]);
                 };

                 return
                 Panel([
                    Rect().Bind("Text").Shader(shader),
                    Rect().Bind("Text2").Shader(shader),
                 ]);
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
                 float v = (float)Easing.CubicOut(ctx.progress);
                 // 渐变色标：圆心透明 → 紫色 → 外圈黑色
                 SKColor[] colors = new[]
                 {
                       new SKColor(0, 0, 0, 0),        // 圆心：完全透明
                       new SKColor(0, 0, 0, 0),        // 圆心：完全透明
                       new SKColor(0, 0, 0, 255),       // 外圈边缘：纯黑
                       new SKColor(0, 0, 0, 255)       // 外圈边缘：纯黑
                 };

                 v = 1 - v;

                 // 对应每个颜色的径向位置 0~1
                 float[] colorPositions = new[] { 0f, v - 0.04f, v, 1f };
                 float gap = 0.02f; // 调整这个值来控制颜色过渡的范围

                 var shader = (SKRect rect) =>
                 {
                     return SKShader.CreateRadialGradient(rect.Center, rect.MaxRadius, colors, colorPositions, SKShaderTileMode.Clamp);
                 };

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
                    TextBlock("Demo").Align(1, -1).Margin(20);
            }
        );

        run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3, logo]);
    }
}
