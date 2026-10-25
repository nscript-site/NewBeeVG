#!/usr/bin/env dotnet

#:include modules/TextIntroAnimation.cs

SkiaAnim(new TextIntroAnimation(1920, 1080, 120, "HELLO WORLD", "> SYSTEM ONLINE", true))
    .AsClip(out var clip1, 120, name: "clip1");
SkiaAnim(new TextIntroAnimation(1920, 1080, 120, "YOUR TEXT", "> YOUT SUBTITLE", true))
    .AsClip(out var clip2, 120, name: "clip2");
Text("By NewBeeVG").Align(1, -1).Margin(100).Foreground(SKColors.White)
    .AsClip(out var clip3, -1, start: -1, name: "clip3");
run(stage(1920, 1080, bg: SKColors.DarkBlue), [clip1, clip2, clip3]);