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
        float thickness = 5;

        HStack([
            VStack([
                TextBlock("3D演示1").Font(40, SKColors.Black),
                Canvas3D(800,800,SKColors.Black)
                    .Camera(PerspectiveCamera(Vec3(10, 12, -25)))
                    .Nodes([
                        Line3D(Vec3(0,0,0),Vec3(0,0,len),SKColors.Red,thickness)
                            .OnFrameT(e=>e.Sender.End = Vec3(0,0,len*e.pf)),
                        Line3D(Vec3(0,0,0),Vec3(0,len,0),SKColors.Green,thickness)
                            .OnFrameT(e=>e.Sender.End = Vec3(0,len*e.pf,0)),
                        Line3D(Vec3(0,0,0),Vec3(len,0,0),SKColors.Blue,thickness)
                            .OnFrameT(e=>e.Sender.End = Vec3(len*e.pf,0,0)),
                    ]).Align(0,-1)
            ]),
            VStack([
                TextBlock("3D演示2").Font(40, SKColors.Black),
                Canvas3D(800,800,SKColors.White)
                    .Camera(PerspectiveCamera(Vec3(-10, -12, 25)))
                    .Nodes([
                        Line3D(Vec3(0,0,0),Vec3(0,0,len),SKColors.Red,thickness)
                            .OnFrameT(e=>e.Sender.End = Vec3(0,0,len*e.pf)),
                        Line3D(Vec3(0,0,0),Vec3(0,len,0),SKColors.Green,thickness)
                            .OnFrameT(e=>e.Sender.End = Vec3(0,len*e.pf,0)),
                        Line3D(Vec3(0,0,0),Vec3(len,0,0),SKColors.Blue,thickness)
                            .OnFrameT(e=>e.Sender.End = Vec3(len*e.pf,0,0)),
                    ]).Align(0,-1)
            ])
        ])
        .Align(0, 0)
        .AsClip(out var clip1, frames: 40, name: "3d");

        run(stage(1920, 1080, bg: SKColors.Orange), [clip1]);
    }
}

