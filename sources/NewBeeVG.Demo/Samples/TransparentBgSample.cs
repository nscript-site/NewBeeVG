using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class TransparentBgSample
{
    public static void Run()
    {
        VStack([
            Panel().Size(200,200).Background(SKColors.Gray).Align(0,0)
        ])
        .Align(0, 0)
        .AsClip(out var clip1, frames: 30, name: "TransparentBg");

        run(stage(1920, 1080), [clip1]);
    }
}
