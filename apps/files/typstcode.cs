#!/usr/bin/env dotnet

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
VStack([
    TextBlock("TypstCode 直接嵌入代码").Margin(10),
    TypstCode(code,600, "cs").PageMargin(0)
        .Align(0,-1).Margin(0)
]).Margin(10)
.AsClip(out var clip, 30, name: "typstcode");

run(stage(bg: SKColors.Orange), [clip]);