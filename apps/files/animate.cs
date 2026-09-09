#!/usr/bin/env dotnet

font("阿里巴巴普惠体 2.0");

VStack([
    TextBlock("输入你的文字").Font(120, SKColors.Black).Align(0,0).Id("Text"),
    TextBlock("输入你的文字").Font(120, SKColors.Black)
        .OnFrame(e=> { e.Sender.Opacity(e.p);  e.SenderLayoutable?.Margin(0,e.p * 200,0,0); })
])
.Align(0, 0)
.AsClip(out var clip1, frames: 40, name: "animate");

VStack([
    TextBlock("Code").Font(80, SKColors.Orange).Align(0,0),
    TypstFile("./typst/code1.typ").MaxHeight(800).Align(0,0),
    TextBlock("生成视频的全部代码").Font(40, SKColors.Black).Align(0,-1),
]).Align(0,0).AsClip(out var clip2, frames: 120, name: "code");

TextBlock("NewBee VG").FontSize(40).Margin(20).Align(1, -1)
.AsClip(out var logo, frames: -1, start: 0, name: "logo");

run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, logo]);
