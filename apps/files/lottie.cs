#!/usr/bin/env dotnet

HStack([
    LottieFile("./assets/fire.json").Size(100,100).Align(0, 1),
    LottieFile("./assets/fire.json").Size(200,200).Speed(2).Align(0, 1),
])
.Align(0, 0)
.AsClip(out var clip1, frames: 100, name: "animate");

run(stage(1920, 1080, bg: SKColors.Orange), [clip1]);

