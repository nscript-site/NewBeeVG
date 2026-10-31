namespace NewBeeVG.Demo.Samples;

internal class LayoutSample
{
    public static void Run()
    {
        VStack([
            TextBlock("Code").Font(80, SKColors.Orange).Align(0,0),
            TypstFile("./Assets/code1.typ")
            .MaxHeight(800).Align(0,0)
        ]).Align(0, 0).AsClip(frames: 40, name: "code");
        run(stage(1920, 1080, bg: SKColors.White));
    }
}
