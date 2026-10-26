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
        ]).Align(0, 0).AsClip(out var clip1, frames: 40, name: "clip1");

        VStack([
            TextBlock("Clip2").Font(80, SKColors.Orange).Align(0,0)
        ]).Align(0, 0).AsClip(out var clip2, frames: 40, name: "clip2");

        AudioFile("./Assets/audio01.mp3").AsClip("audio").LinkTo(clip2);

        run(stage(1920, 1080, bg: SKColors.White), clips());
    }
}
