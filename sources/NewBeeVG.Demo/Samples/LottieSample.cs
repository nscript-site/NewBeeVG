using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class LottieSample
{
    public static void Run()
    {
        VStack([
            Panel([
                LottieFile("./Assets/fire.json").Size(100,400).Align(0,0)
            ])
        ])
        .Align(0, 0)
        .AsClip(out var clip1, frames: 400, name: "animate");

        run(stage(1920, 1080, bg: SKColors.White), [clip1]);
    }
}
