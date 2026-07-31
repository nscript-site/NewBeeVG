namespace NewBeeVG.Demo.Samples;

internal class TextSamples
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        var style1 = (NBVisual v) => {
            v.As<NBText>()?
                .AddStroke(SKColors.Orange, 55)
                .AddStroke(SKColors.White, 50)
                .AddStroke(SKColors.Red, 40)
                .TextAlign(0)
                .LineSpacing(10)
                .FontSize(50)
                .Foreground(SKColors.Black)
                .Align(0, 0);
        };

        var style2 = (NBVisual v) => {
            v.As<NBText>()?
                .TextAlign(0)
                .FontSize(80).Foreground(SKColors.Black).Align(0, 0).LetterSpacing(2).Padding(10);
        };

        var vertical = (NBVisual v) =>
        {
            v.As<NBText>()?.Vertical();
        };

        var s = "ABCDEDFHIvwxyz1234567890.求关注求关注求关注求关注求关注求关注";

        var htxt = (int idx) =>
            VStack([
                TextBlock(s).Styles( idx == 0 ? style1 : style2).LineSpacing(20),
            ]).Spacing(0).Background(SKColors.Yellow)
            .Align(null, 0);

        var vtxt = (int idx, bool rtl) =>
            HStack([
                TextBlock(s).Styles(idx == 0 ? style1 : style2, vertical).RightToLeft(rtl),
            ]).Spacing(0).Background(SKColors.Yellow)
            .Align(0, null);

        htxt(0).AsClip(out var clip1, 30, name: "htext1");
        htxt(1).AsClip(out var clip2, 30, name: "htext2");
        vtxt(0, false).AsClip(out var clip3, 30, name: "vtext1");
        vtxt(0, true).AsClip(out var clip4, 30, name: "vtext1(rtl)");
        vtxt(1, false).AsClip(out var clip5, 30, name: "vtext2");
        vtxt(1, true).AsClip(out var clip6, 30, name: "vtext2(rtl)");

        run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3, clip4, clip5, clip6]);
    }
}
