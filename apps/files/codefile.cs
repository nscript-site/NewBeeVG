#!/usr/bin/env dotnet

VStack([
    TextBlock("自举显示代码").Margin(10),
    TypstCodeFile().Align(0,-1).Margin(0)
]).Margin(10)
.AsClip(out var clip1, 30, name: "self");

VStack([
    TextBlock("显示其它代码").Margin(10),
    TypstCodeFile("./lottie.cs").Margin(0)
]).Margin(10)
.AsClip(out var clip2, 30, name: "lottie.cs");

run(stage(1920,1080, bg: SKColors.White), [clip1, clip2]);