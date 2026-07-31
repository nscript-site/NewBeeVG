namespace NewBeeVG.Demo.Samples
{
    internal class RichTextSamples
    {
        public static void Run()
        {
            font("阿里巴巴普惠体 2.0");

            var style1 = (NBVisual v) => {
                v.As<NBTextRun>()?
                    .AddStroke(SKColors.Orange, 55)
                    .AddStroke(SKColors.White, 50)
                    .AddStroke(SKColors.Red, 40)
                    .FontSize(30).Foreground(SKColors.Black)
                    .LetterSpacing(2);
            };

            var style2 = (NBVisual v) => {
                v.As<NBTextRun>()?
                    .FontSize(50)
                    .Foreground(SKColors.Blue).LetterSpacing(30);
            };

            var vertical = (NBVisual v) =>
            {
                v.As<IOrientation>()?.Vertical();
            };

            var style3 = (NBVisual v) => {
                v.As<NBText>()?
                    .FontSize(30).Foreground(SKColors.Black).Align(0, 0).LetterSpacing(2)
                    .LineSpacing(10)
                    .Padding(10);
            };


            var s1 = "abcdefgdffss求关注求关注求关注求关注求关注";
            var s2 = "求关注求关注";

            // rich text clips
            var hrich = () =>
                VStack([
                    RichText([
                        TextRun(s1).Styles(style1),
                        TextRun(s1).Styles(style2),
                        TextRun(s1).Styles(style1),
                        ]).CrossAxisAlign(0).Padding(100).LineSpacing(10),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(null, 0);

            var vrich = (bool rtl) =>
                HStack([
                    RichText([
                        TextRun(s1).Styles(style1),
                        TextRun(s1).Styles(style2),
                        TextRun(s1).Styles(style1),
                        ]).Styles(vertical).CrossAxisAlign(0).RightToLeft(rtl).Padding(100).LineSpacing(10),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, null);

            hrich().AsClip(out var clip01, 30, name: "hrichtext");
            vrich(false).AsClip(out var clip02, 30, name: "vrichtext");
            vrich(true).AsClip(out var clip03, 30, name: "vrichtext(rtl)");

            // normal text clips
            var s3 = "ABCDEDFHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890.求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注";
            var htext = () =>
                VStack([
                    TextBlock(s3).Styles(style3),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(null, 0);

            var vtext = (bool rtl) =>
                HStack([
                    TextBlock(s3).Styles(style3, vertical).RightToLeft(rtl),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, null);

            htext().AsClip(out var clip11, 30, name: "htext");
            vtext(false).AsClip(out var clip12, 30, name: "vtext");
            vtext(true).AsClip(out var clip13, 30, name: "vtext(rtl)");

            run(stage(1920, 1080, bg: SKColors.White), [clip01, clip02, clip03, clip11, clip12, clip13]);
        }
    }
}
