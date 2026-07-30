namespace NewBeeVG.Demo.Samples;

internal class TextSamples
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        var style = (NBVisual v) => {
            v.As<NBText>()?
                .AddStroke(SKColors.Orange, 15)
                .AddStroke(SKColors.White, 13)
                .AddStroke(SKColors.Red, 10).TextAlign(0)
                .FontSize(30).Foreground(SKColors.Black).Align(0, 0).LetterSpacing(2).Padding(10);
        };

        var vertical = (NBVisual v) =>
        {
            v.As<NBText>()?.Vertical();
        };

        var s = "ABCDEDFHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890.求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注";
        //var s = "ABCDEDFHIJKLMNOPQRSTUVWXYZ";

        var hcontent = () =>
            VStack([
                TextBlock(s).Styles(style).Id("Text"),
            ]).Spacing(0).Background(SKColors.Yellow)
            .Align(0, -1);

        var vcontent = (bool rtl) =>
            HStack([
                TextBlock(s).Styles(style, vertical).RightToLeft(rtl).Id("Text"),
            ]).Spacing(0).Background(SKColors.Yellow)
            .Align(0, -1);

        hcontent().AsClip(out var clip1, 30, name: "htext");
        vcontent(false).AsClip(out var clip2, 30, name: "vtext");
        vcontent(true).AsClip(out var clip3, 30, name: "vtext(rtl)");

        run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3]);
    }
}
