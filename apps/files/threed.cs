#!/usr/bin/env dotnet

font("阿里巴巴普惠体 2.0");

float len = 10;
float thickness = 5;

HStack([
    VStack([
        TextBlock("3D动画 (Camera 固定)").Font(40, SKColors.Black),
        Canvas3D(800,800,SKColors.Black)
            .Camera(PerspectiveCamera(Vec3(10, 12, -25)))
            .Nodes([
                GroundGrid(10,1,SKColors.White,1),
                Line3D(Vec3(),Vec3(0,0,len),SKColors.Red,thickness)
                    .OnFrameT(e=>e.Sender.End = Vec3(0,0,len*e.pf)),
                Line3D(Vec3(),Vec3(0,len,0),SKColors.Green,thickness)
                    .OnFrameT(e=>e.Sender.End = Vec3(0,len*e.pf,0)),
                Line3D(Vec3(),Vec3(len,0,0),SKColors.Blue,thickness)
                    .OnFrameT(e=>e.Sender.End = Vec3(len*e.pf,0,0)),
            ]).Align(0,-1)
    ]),
    VStack([
        TextBlock("3D动画 (Camera 旋转)").Font(40, SKColors.Black),
        Canvas3D(800,800,SKColors.White).Ref(out var canvas)
            .OnFrame(e=>{ canvas.RotateCamera(2f,0); canvas.ZoomCamera(-0.1f); })
            .Camera(PerspectiveCamera(Vec3(-10, -12, 25)))
            .Nodes([
                GroundGrid(10,1,SKColors.Black,1),
                Line3D(Vec3(),Vec3(0,0,len),SKColors.Red,thickness),
                Line3D(Vec3(),Vec3(0,len,0),SKColors.Green,thickness),
                Line3D(Vec3(),Vec3(len,0,0),SKColors.Blue,thickness),
            ]).Align(0,-1)
    ])
])
.Align(0, 0)
.AsClip(out var clip1, frames: 40, name: "3d");

// VStack([
//     TypstFile("./typst/code2.typ").MaxHeight(900).Align(0,0),
//     TextBlock("生成视频的全部代码").Font(40, SKColors.Black).Align(0,-1),
// ]).Align(0,0).AsClip(out var clip2, frames: 120, name: "code");

run(stage(1920, 1080, bg: SKColors.Orange), [clip1]);

// run(stage(1920, 1080, bg: SKColors.Orange), [clip1,clip2]);

