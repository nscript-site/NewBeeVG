using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class AudioSample
{
    public static void Run()
    {
        VStack([
            TextBlock("Clip1").Font(80, SKColors.Orange).Align(0,0)
        ]).Align(0, 0).AsClip(frames: 40, name: "clip1");

        VStack([
            TextBlock("Clip2").Font(80, SKColors.Orange).Align(0,0)
        ]).Align(0, 0).AsClip(frames: 40, name: "clip2");

        // 音频链接到 name 为 clip1 的 第0帧（初始帧）
        AudioFile("./Assets/audio01.mp3")
            .AsClip("audio").LinkTo("clip1");

        // 音频链接到 clip1 的 第10帧（初始帧为 0）
        //AudioFile("./Assets/audio01.mp3")
        //    .AsClip("audio").LinkTo("clip1", 10);

        run(stage(1920, 1080, bg: SKColors.White), clips(), args: ["clip1"]);
    }
}
