#!/usr/bin/env dotnet

VStack([
    TextBlock("Seg 动画").Margin(10),
    TypstFile("typst/seg.typ").Ref(out var typ)
        .OnFrame(e=>{
            typ.Seg("t1", SKColors.Red, 30 + e.frame * 4);
            typ.Seg("t2", SKColors.Green, 30 + e.frame * 4);
            typ.Seg("t3", SKColors.Blue, 30 + e.frame * 4);
            })
        .Align(0,-1).Margin(20)
]).Margin(100)
.AsClip(out var clip, 30, name: "typst seg");

run(stage(1920, 1080, bg: SKColors.Orange), [clip]);