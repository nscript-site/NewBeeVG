#!/usr/bin/env dotnet
#:package NewBeeVG@1.0.1.15
// 包含 MyAnimation 所在的源文件路径
#:include MyAnimation.cs

// 在仓库 apps/files 内运行时，Directory.Build.props 已提供 NewBeeVG 项目引用；
// 若出现重复引用或版本冲突，删除上面的 #:package 指令即可。

using NewBeeVG;
using SkiaSharp;
using static NewBeeVG.Methods;

// debug = true  ：只跑 AI 测试语句（PNG / MP4 / GIF 导出）
// debug = false ：跳过测试，执行结尾 run(...)，用下面 SkiaAnim 注册的真实片段出片
// AI 编写/测试期间必须保持 true，禁止改为 false；false 仅由使用者交付后手动切换
bool debug = true;

// ============================================================
//  测试入口模板（保存为 MyAnimation_Test.cs）
//  - 必须与 MyAnimation.cs 同目录（或修正 #:include 的相对路径）
//  - 动画类文件里不能有顶层语句，入口只在本文件
//  - 每个场景用 { } 隔离变量，末尾用 SkiaAnim 写真实场景用法（不加 debug 条件）
// ============================================================

const int Width = 1920;
const int Height = 1080;
const int Frames = 90;

// ------------------------------------------------------------
// 场景 1：默认主题，导出首尾两帧
// ------------------------------------------------------------
{
    var content = new MyAnimation.ContentSetting
    {
        From = 0,
        To = 12580,
        Format = "N0",
        FontSize = 160f
    };

    var anim = new MyAnimation(Width, Height, Frames, content);

    if (debug == true) anim.SavePNG(0, "output/myanimation/frame_000.png");
    if (debug == true) anim.SavePNG(Frames - 1, "output/myanimation/frame_last.png");

    // 实际场景用法：只参与编译，不执行渲染
    SkiaAnim(new MyAnimation(Width, Height, Frames, content))
        .AsClip(Frames * 5, name: "默认主题");
}

// ------------------------------------------------------------
// 场景 2：扩展主题（基础 7 色 + 专用色）
// ------------------------------------------------------------
{
    var content = new MyAnimation.ContentSetting
    {
        From = 0,
        To = 12580,
        Format = "N0",
        FontSize = 160f
    };

    var theme = new MyAnimation.ContentTheme
    {
        Name = "夜墨铜金",
        Primary = new SKColor(0xC2, 0x91, 0x4E),
        Background = new SKColor(0x0A, 0x14, 0x20),
        TextPrimary = new SKColor(0xEF, 0xE6, 0xD2),
        TextSecondary = new SKColor(0x4E, 0x8F, 0xA0),
        Divider = new SKColor(0x4E, 0x47, 0x3E),
        Positive = new SKColor(0xCE, 0x4A, 0x32),
        Negative = new SKColor(0x3F, 0x7D, 0x5C),
        Glow = new SKColor(0xFF, 0xF0, 0xD6)
    };

    var anim = new MyAnimation(Width, Height, Frames, content, theme);
    if (debug == true) anim.SavePNG(Frames - 1, "output/myanimation/frame_last_themed.png");

    SkiaAnim(new MyAnimation(Width, Height, Frames, content, theme))
        .AsClip(Frames * 5, name: "掠金");
}

// ------------------------------------------------------------
// 场景 3：PNG 序列 + MP4 / GIF 导出
// ------------------------------------------------------------
{
    var content = new MyAnimation.ContentSetting
    {
        From = 0,
        To = 98500,
        Format = "N0"
    };

    var anim = new MyAnimation(Width, Height, Frames, content);

    if (debug == true)
    {
        for (int i = 0; i < anim.Frames; i++)
        {
            if (!anim.SavePNG(i, $"output/myanimation/frames/{i:0000}.png"))
            {
                Console.WriteLine($"第 {i} 帧渲染失败");
            }
        }

        // SaveMp4 / SaveGif 不会自动创建目录
        Directory.CreateDirectory("output");
        anim.SaveMp4(30, "output/myanimation.mp4");
        anim.SaveGif(25, "output/myanimation.gif");
    }

    SkiaAnim(anim)
        .AsClip(anim.Frames, name: "完整动画");
}

// debug = false 时正式出片；debug = true 时只跑上面的测试
if (debug == false)
    run(stage(Width, Height), clips());
