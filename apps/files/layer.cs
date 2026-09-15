#!/usr/bin/env dotnet

Layer([
    TextBlock("输入你的文字").Font(120, SKColors.Black).Align(0,0).Id("Text"),
    Rect().Bind("Text")
    .OnFrame(e=>e.Sender.Shaders(Shaders.AlphaLinearGradient(e.p)))
])
.Align(0, 0)
.AsClip(out var clip1, frames: 40, name: "layer");

run(stage(1920, 1080, bg: SKColors.White), [clip1]);