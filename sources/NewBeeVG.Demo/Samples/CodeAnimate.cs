namespace NewBeeVG.Demo.Samples;

internal class CodeAnimate
{
    public static void Run()
    {
        void Update(NBTypstCode tc, NBFrameUpdateEvent e)
        {
            var lines = 10;
            var step = e.durationFrames / lines;
            var bounds = tc.Bounds;
            var lineHeight = bounds.Height / lines;
            int lineIdx = e.frame / step;
            var rect = new SKRect(lineHeight*1.5f, lineIdx * lineHeight, bounds.Width, (lineIdx + 1) * lineHeight);
            byte rectAlpha = 0;
            tc.FrameMaskByAlphaBitmap(e.w, e.h, 150, locals: [(rect, rectAlpha)]);
        }

        var code = """
            #!/usr/bin/env dotnet

            VGrid($"*", [
                TypstFile("./typst/page1.typ").Ref(out var typ)
                    .Align(0,0)
                    .OnFrame(e=> typ.UpdateInputs("frames", e.frame))
                ]).Background(SKColors.DeepSkyBlue)
                .AsClip(out var clip, 30, name: "typst");

            run(stage(bg: SKColors.Orange), [clip]);
            """;
        Panel([
            TypstCode(code,600, "cs").Ref(out var tc)
                .PageMargin(0)
                .OnFrame(e => { Update(tc, e); })
                .Align(0,0)
        ]).Background(SKColors.White).Align(null,0)
        .Margin(10)
        .AsClip(out var clip, 300, name: "typstcode");

        run(stage(1920, 1080, bg: SKColors.Orange), [clip]);
    }
}
