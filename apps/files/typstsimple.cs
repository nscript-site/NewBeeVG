#!/usr/bin/env dotnet

VStack([
    TypstFile("./typst/math.typ").Ref(out var typ)
        .Align(0,0)
]).Background(SKColors.DeepSkyBlue)
.AsClip(out var clip, 30, name: "typst");

run(stage(bg: SKColors.Orange), [clip]);