namespace NewBeeVG.Demo.Samples
{
    internal class RichTextSamples
    {
        public static void Run()
        {
            font("阿里巴巴普惠体 2.0");

            var style1 = (NBVisual v) => {
                v.As<NBTextRun>()?
                    .FontSize(30).Foreground(SKColors.Black)
                    //.FontSlant(SKFontStyleSlant.Italic)
                    .LetterSpacing(2);
            };

            var style2 = (NBVisual v) => {
                v.As<NBTextRun>()?
                    //.AddStroke(SKColors.Red, 10)
                    .FontSize(50)
                    .Foreground(SKColors.Blue).LetterSpacing(30);
            };

            var vertical = (NBVisual v) =>
            {
                v.As<IOrientation>()?.Vertical();
            };

            var style = (NBVisual v) => {
                v.As<NBText>()?
                    .FontSize(30).Foreground(SKColors.Black).Align(0, 0).LetterSpacing(2)
                    .LineSpacing(10)
                    //.LineHeight(100)
                    .Padding(10);
            };


            var s0 = "ABCDEDFHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890.求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注";
            var htext = () =>
                VStack([
                    TextBlock(s0).Styles(style),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, -1);

            var vtext = (bool rtl) =>
                HStack([
                    TextBlock(s0).Styles(style, vertical).RightToLeft(rtl),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, -1);

            htext().AsClip(out var clip01, 30, name: "htext");
            vtext(false).AsClip(out var clip02, 30, name: "vtext");
            vtext(true).AsClip(out var clip03, 30, name: "vtext(rtl)");

            var s1 = "abcdefgdffss求关注求关注求关注求关注求关注";
            var s2 = "求关注求关注";

            var hcontent = () =>
                VStack([
                    RichText([
                        TextRun(s1).Styles(style1),
                        TextRun(s1).Styles(style2),
                        TextRun(s1).Styles(style1),
                        ]).CrossAxisAlign(0).Padding(100).LineSpacing(10),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, -1);

            var vcontent = (bool rtl) =>
                HStack([
                    RichText([
                        TextRun(s1).Styles(style1),
                        TextRun(s1).Styles(style2),
                        TextRun(s1).Styles(style1),
                        ]).Styles(vertical).CrossAxisAlign(0).RightToLeft(rtl).Padding(100).LineSpacing(10),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, -1);

            hcontent().AsClip(out var clip1, 30, name: "hrichtext");
            vcontent(false).AsClip(out var clip2, 30, name: "vrichtext");
            vcontent(true).AsClip(out var clip3, 30, name: "vrichtext(rtl)");

            run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3, clip01, clip02, clip03]);
        }
    }
}
