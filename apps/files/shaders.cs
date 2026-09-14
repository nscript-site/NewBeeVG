#!/usr/bin/env dotnet

Rect(400,200)
.OnFrame(e=>e.Sender.Shaders(Shaders.LinearGradientOnRect([ SKColors.Red, SKColors.Green, SKColors.Blue],[0, e.pf, 1])))
.Align(0, 0)
.AsClip(out var clip1, frames: 30, name: "animate");

run(stage(1920, 1080, bg: SKColors.White), [clip1]);