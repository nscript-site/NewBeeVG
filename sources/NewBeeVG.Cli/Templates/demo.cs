#!/usr/bin/env dotnet

// nuget 包引用，如果没在 Directory.Build.props 统一设置则需要取消注释
#:package NewBeeVG@1.0.1.30

// 引用其他文件
// #:include other.cs

// 命名空间，如果没在 Directory.Build.props 统一设置则需要取消注释
using NewBeeVG;
using SkiaSharp;
using static NewBeeVG.Methods;

VStack([
    TextBlock("Hello").Font(80, SKColors.Orange).Ref(out var tb1)
    .Margin(100,0,0,100).Align(-1,0)
    .OnFrame(e=>tb1.Margin(100 + e.frame * 20,0,0,100))
]).Align(-1, 0).AsClip(frames: 40, name: "clip1");

VStack([
    TextBlock("World").Font(80, SKColors.Orange).Ref(out var tb2)
    .Align(0,0)
    .OnFrame(e=>tb2.Opacity(1-e.pf))
]).Align(0, 0).AsClip(frames: 40, name: "clip2");

// 音频链接到 name 为 clip1 的 第0帧（初始帧）
// AudioFile("./assets/audio01.mp3")
//     .AsClip("audio").LinkTo("clip1",0);

/**************************************************************************************************
 dotnet <file.cs>						// 预览视频
 dotnet <file.cs> -- "@dump"			// 将视频的元数据及clip信息输出在控制台
 dotnet <file.cs> -- demo.mp4			// 视频保存为 mp4
 dotnet <file.cs> -- 3 3.png			// 把视频中第 3 帧保存为 png
 dotnet <file.cs> -- 3 @dump			// 把视频中第 3 帧转码为 png，以base64输出控制台
 dotnet <file.cs> -- clip1				// 预览 name=clip1 的片段
 dotnet <file.cs> -- clip1 clip1.mp4	// 把 name=clip1 的片段保存为 mp4
 dotnet <file.cs> -- clip1 3 "@dump"	// 把 name=clip1 的片段的第3帧转码为 png，以base64输出控制台
**************************************************************************************************/
run(stage(1920, 1080, bg: SKColors.White), args: args);
