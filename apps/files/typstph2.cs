#!/usr/bin/env dotnet

var str = "2025年，中国对外直接投资流量2135.8亿美元，比上年增长11.1%";
VStack([
    TextBlock("PlaceHolder 动画").Margin(10),
    TypstFile("typst/placeholder.typ").Ref(out var typ)
        .OnFrame(e=>{
            typ.Ph("str", str.Substring(0, e.frame + 1));
         })
        .Align(0,-1).Margin(20)
 ]).Margin(100)
.AsClip(out var clip, 60, name: "typst ph2");

run(stage(1920, 1080, bg: SKColors.Orange), [clip]);