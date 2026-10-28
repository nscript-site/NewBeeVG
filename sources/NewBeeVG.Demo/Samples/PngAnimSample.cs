using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class PngAnimSample
{
    public static void Run()
    {
        VStack([
             PngAnim("./Assets/dollar_rain.png").Size(400,400).Align(0,0),
        ])
        .Align(0, 0)
        .AsClip(frames: 400, name: "animate");

        run(stage(1920, 1080, bg: SKColors.White), clips());
    }
}
