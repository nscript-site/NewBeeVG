#!/usr/bin/env dotnet

HGrid("*,200,0.5*,*,2*,Auto",
[
    null,
    Rect(0,100,SKColors.Green,20).Align(null,0),
    Rect(0,100,SKColors.Red,20).Align(null,-1),
    Rect(0,100,SKColors.Blue,20).Align(null,0),
    Rect(0,100,SKColors.Green,20).Align(null,1),
    Rect(100,100,SKColors.Blue,20).Align(0,0),
]).AsClip(out var clip, 30, name: "clip");

run(stage(1920,1080,bg: SKColors.Orange), [clip]);