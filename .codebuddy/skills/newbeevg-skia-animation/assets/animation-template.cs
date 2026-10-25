#!/usr/bin/env dotnet

#:package NewBeeVG@1.0.1.11
using NewBeeVG;
using SkiaSharp;

// NewBeeVG SkiaSharp 逐帧动画模板（file-based app）
// 用法：复制到 apps/files/ 下，修改尺寸、帧数、帧率与 DrawFrame 内的绘制逻辑后运行
// 注意：顶层语句在前，类型声明必须放在文件末尾

const int Width = 1080;
const int Height = 1920;
const int Frames = 90;
const int Fps = 30;

var anim = new MyAnimation(Width, Height, Frames);

// 1) 先验证单帧（SavePNG 会自动创建目录）
anim.SavePNG(0, "output/preview.png");

// 2) 导出 PNG 序列
for (int i = 0; i < anim.Frames; i++)
{
    if (!anim.SavePNG(i, $"output/frames/{i:0000}.png"))
        Console.WriteLine($"第 {i} 帧渲染失败");
}

// 3) 直接导出 MP4 / GIF（不会自动创建目录，先确保 output 存在）
// Directory.CreateDirectory("output");
// anim.SaveMp4(Fps, "output/animation.mp4");
// anim.SaveGif(25, "output/animation.gif");

// 4) 或手动合成视频（在项目根目录执行）：
// ffmpeg -framerate 30 -i output/frames/%04d.png -c:v libx264 -pix_fmt yuv420p -crf 18 output/animation.mp4
Console.WriteLine($"已导出 {anim.Frames} 帧，建议帧率 {Fps}fps");

public class MyAnimation : SKCustomAnimation
{
    public MyAnimation(int width, int height, int frames)
        : base(width, height, frames)
    {
    }

    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        // 每帧清空背景；位图带 Alpha，可改用 SKColors.Transparent 导出透明背景
        canvas.Clear(new SKColor(0xFF101820));

        // 帧号 -> 0..1 进度
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

        float cx = size.Width / 2f;
        float cy = size.Height / 2f;

        // 在这里替换为实际绘制逻辑
        using (var paint = new SKPaint { IsAntialias = true, Color = new SKColor(0xFFF0B429) })
        {
            canvas.DrawCircle(cx, cy, 120f + 40f * MathF.Sin(t * MathF.PI * 2f), paint);
        }
    }
}
