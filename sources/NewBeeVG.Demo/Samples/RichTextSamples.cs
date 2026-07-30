namespace NewBeeVG.Demo.Samples
{
    internal class RichTextSamples
    {
        public static void Run()
        {
            font("阿里巴巴普惠体 2.0");

            var style1 = (NBVisual v) => {
                v.As<NBTextRun>()?
                    .AddStroke(SKColors.Orange, 15)
                    .AddStroke(SKColors.White, 13)
                    .AddStroke(SKColors.Red, 10)
                    .FontSize(30).Foreground(SKColors.Black);
            };

            var style2 = (NBVisual v) => {
                v.As<NBTextRun>()?
                    .AddStroke(SKColors.Orange, 15)
                    .AddStroke(SKColors.White, 13)
                    .AddStroke(SKColors.Red, 10)
                    .FontSize(30).Foreground(SKColors.Black);
            };

            var vertical = (NBVisual v) =>
            {
                v.As<IOrientation>()?.Vertical();
            };

            var s1 = "求关注";
            var s2 = "ABCDEDFHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890.求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注求关注";
            //var s = "ABCDEDFHIJKLMNOPQRSTUVWXYZ";
            
            var hcontent = () =>
                VStack([
                    RichText([
                        TextRun(s1).Styles(style1),
                        TextRun(s2).Styles(style2),
                        ]),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, -1);

            var vcontent = (bool rtl) =>
                HStack([
                    RichText([
                        TextRun(s1).Styles(style1),
                        TextRun(s2).Styles(style2),
                        ]).Styles(vertical),
                ]).Spacing(0).Background(SKColors.Yellow)
                .Align(0, -1);

            hcontent().AsClip(out var clip1, 30, name: "htext");
            vcontent(false).AsClip(out var clip2, 30, name: "vtext");
            vcontent(true).AsClip(out var clip3, 30, name: "vtext(rtl)");

            run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, clip3]);
        }
    }
}
