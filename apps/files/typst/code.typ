#import "@preview/zebraw:0.6.3": *
#show: zebraw
#set page(fill: none)
#set page(  width: 600pt, height: auto)
#set page(
    margin: (
    top: 0pt,
    bottom: 0pt,
    left: 0pt,
    right: 0pt,
    )
)

#zebraw(
lang: true,
```
#!/usr/bin/env dotnet

VGrid($"*", [
    TypstFile("./typst/page1.typ").Ref(out var typ)
        .Align(0,0)
        .OnFrame(e=> typ.UpdateInputs("frames", e.frame))
    ]).Background(SKColors.DeepSkyBlue)
    .AsClip(out var clip, 30, name: "typst");

run(stage(bg: SKColors.Orange), [clip]);
```
)
