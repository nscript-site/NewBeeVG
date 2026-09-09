#!/usr/bin/env dotnet

VGrid($"*", [
    TypstFile("./typst/page1.typ")
        .Align(0,0)
        .OnFrame(e=> e.SenderAs<NBTypst>()?.TypstInputs(x=>x["frames"] = $"{e.frame}"))
    ]).Background(SKColors.DeepSkyBlue)
    .AsClip(out var clip, 30, name: "typst");

run(stage(bg: SKColors.Orange), [clip]);