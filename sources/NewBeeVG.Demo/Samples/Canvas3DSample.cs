using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class Canvas3DSample
{
    public static void Run()
    {
        font("阿里巴巴普惠体 2.0");

        float len = 10;

        VStack([
            TextBlock("3D演示").Font(120, SKColors.Black),
            Canvas3D(800,800,SKColors.Black).Nodes([
                //Line3D(Vec3(0,0,0),Vec3(0,0,len),SKColors.Red,2),
                //Line3D(Vec3(0,0,0),Vec3(0,len,0),SKColors.Green,2),
                Line3D(Vec3(0,0,0),Vec3(len,0,0),SKColors.Blue,2)
                    .OnFrameT(e=>e.Sender.End = Vec3(len*e.pf,0,0)),
                ]).Align(0,-1)
        ])
        .Align(0, 0)
        .AsClip(out var clip1, frames: 40, name: "3d");

        run(stage(1920, 1080, bg: SKColors.Orange), [clip1]);
    }
}

