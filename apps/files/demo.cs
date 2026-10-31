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

/**************************************************************************************************
 dotnet .\demo.cs						// 预览视频
 dotnet .\demo.cs -- @dump				// 将视频的元数据及clip信息输出在控制台
 dotnet .\demo.cs -- demo.mp4			// 视频保存为 mp4
 dotnet .\demo.cs -- 3 3.png			// 把视频中第 3 帧保存为 png
 dotnet .\demo.cs -- 3 @dump			// 把视频中第 3 帧转码为 png，以base64输出控制台
 dotnet .\demo.cs -- clip1				// 预览 name=clip1 的片段
 dotnet .\demo.cs -- clip1 clip1.mp4	// 把 name=clip1 的片段保存为 mp4
 dotnet .\demo.cs -- clip1 3 @dump		// 把 name=clip1 的片段的第3帧转码为 png，以base64输出控制台
**************************************************************************************************/
run(stage(1920, 1080, bg: SKColors.White), args:args);

