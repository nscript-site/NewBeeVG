#!/usr/bin/env dotnet

 VStack([
     TextBlock("Clip1").Font(80, SKColors.Orange).Align(0,0)
 ]).Align(0, 0).AsClip(frames: 40, name: "clip1");

 VStack([
     TextBlock("Clip2").Font(80, SKColors.Orange).Align(0,0)
 ]).Align(0, 0).AsClip(frames: 40, name: "clip2");

 // 音频链接到 name 为 clip1 的 第0帧（初始帧）
 AudioFile("./assets/audio01.mp3")
     .AsClip("audio").LinkTo("clip1");

 run(stage(1920, 1080, bg: SKColors.White), clips(),args:args);