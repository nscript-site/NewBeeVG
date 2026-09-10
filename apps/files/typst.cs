#!/usr/bin/env dotnet

VStack([
    TypstFile("./typst/page1.typ").Ref(out var typ)
        .Align(0,0)
        .OnFrame(e=> typ.UpdateInputs("frames", e.frame))
]).Background(SKColors.DeepSkyBlue)
.AsClip(out var clip, 30, name: "typst");

run(stage(bg: SKColors.Orange), [clip]);