using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples
{
    internal class TypstAnimSample
    {
        public static void Run()
        {
            Panel([
                 TypstAnim("./Assets/anim_plot.typ")
                    .Size(400*3,240*3)
                    .Align(0,0),
            ]).Background(SKColors.Red)
            .Align(0, 0)
            .AsClip(frames: 50, name: "animate");

            run(stage(1920, 1080, bg: SKColors.Orange), clips());
        }
    }
}
