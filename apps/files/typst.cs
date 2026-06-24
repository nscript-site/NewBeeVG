var clip1 = clip(
    name: "typst",
    frames: 30,
    builder: (ctx, clip) =>
    {
        return
        VGrid($"*", [
                TypstFile("./typst/page1.typ")
                    .Align(0,0)
                    .TypstInputs(x=>{x["frames"] = $"{ctx.frame}"; })
                ]).Background(SKColors.DeepSkyBlue);
    }
);

run(stage(bg: SKColors.Orange), [clip1]);