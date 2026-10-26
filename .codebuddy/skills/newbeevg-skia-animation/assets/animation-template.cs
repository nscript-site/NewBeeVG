#!/usr/bin/env dotnet

#:package NewBeeVG@1.0.1.13

// NewBeeVG SkiaSharp 逐帧动画模板（file-based app）
// 用法：复制到 apps/files/ 下，修改尺寸、帧数、帧率、ContentSetting / ContentTheme 与 DrawFrame 后运行
// 注意：顶层语句在前，类型声明必须放在文件末尾

using NewBeeVG;
using SkiaSharp;

const int Width = 1080;
const int Height = 1920;
const int Frames = 90;
const int Fps = 30;

// 内容参数集中放在子类的内部类 ContentSetting 中
var content = new MyAnimation.ContentSetting
{
    Title = "NewBee VG",
    TitleSize = 96f,
    Radius = 120f
};

// 内置主题颜色不够时，用 ContentTheme 补充专用颜色
// 也可用 MyAnimation.ContentTheme.From(NBVideoThemes.Ocean) 基于内置主题扩展
var contentTheme = new MyAnimation.ContentTheme
{
    Name = "深海",
    Primary = new SKColor(0x2E, 0x6B, 0xFF),
    Background = new SKColor(0x0A, 0x12, 0x1E),
    TextPrimary = new SKColor(0xF2, 0xF6, 0xFF),
    TextSecondary = new SKColor(0x8B, 0x9A, 0xAE),
    Divider = new SKColor(0x24, 0x31, 0x42),
    Positive = new SKColor(0x3D, 0xD5, 0x8C),
    Negative = new SKColor(0xFF, 0x6B, 0x6B),

    // ContentTheme 补充的颜色
    Grid = new SKColor(0x1C, 0x2A, 0x3A),
    Highlight = new SKColor(0x66, 0xE0, 0xFF)
};

// 第 5 个参数可选：不传则使用 ContentTheme.CreateDefault()（复制自然主题）
var anim = new MyAnimation(Width, Height, Frames, content, contentTheme);

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
    private readonly ContentSetting _content;
    private readonly ContentTheme _theme;

    // 推荐签名：尺寸 + 帧数 + 内容配置 + 可选扩展主题
    public MyAnimation(
        int width,
        int height,
        int frames,
        ContentSetting content,
        ContentTheme? contentTheme = null)
        : base(width, height, frames, contentTheme ?? ContentTheme.CreateDefault())
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));

        // 基类 Theme 一定是 ContentTheme（传入实例或 CreateDefault 实例）
        _theme = (ContentTheme)Theme;
    }

    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        // 每帧清空背景；需要透明背景时改用 canvas.Clear(SKColors.Transparent)
        canvas.Clear(_theme.Background);

        // 帧号 -> 0..1 进度
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

        float cx = size.Width / 2f;
        float cy = size.Height / 2f;

        // 基础主题色
        using (var paint = new SKPaint { IsAntialias = true, Color = _theme.Primary })
        {
            canvas.DrawCircle(cx, cy, _content.Radius * (1f + 0.3f * MathF.Sin(t * MathF.PI * 2f)), paint);
        }

        // ContentTheme 补充的专用颜色
        using (var grid = new SKPaint
        {
            IsAntialias = true,
            Color = _theme.Grid,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f
        })
        {
            canvas.DrawLine(0, cy, size.Width, cy, grid);
        }

        using (var highlight = new SKPaint { IsAntialias = true, Color = _theme.Highlight })
        {
            canvas.DrawCircle(cx, cy, _content.Radius * 0.25f, highlight);
        }

        using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei") ?? SKTypeface.Default;
        using var font = new SKFont(typeface, _content.TitleSize);
        using var titlePaint = new SKPaint { IsAntialias = true, Color = _theme.TextPrimary };
        canvas.DrawText(_content.Title, cx, size.Height - 140f, SKTextAlign.Center, font, titlePaint);
    }

    /// <summary>
    /// 动画内容配置：把标题、字号、半径等可变输入集中在一个内部类中。
    /// </summary>
    public class ContentSetting
    {
        /// <summary>标题文字。</summary>
        public string Title { get; set; } = "";

        /// <summary>标题字号（像素）。</summary>
        public float TitleSize { get; set; } = 96f;

        /// <summary>圆形半径（像素）。</summary>
        public float Radius { get; set; } = 120f;
    }

    /// <summary>
    /// 扩展主题：NBVideoTheme 的 7 个基础颜色不够用时，
    /// 继承它并补充网格线、第二强调色等专用颜色。
    /// </summary>
    public class ContentTheme : NBVideoTheme
    {
        /// <summary>网格线 / 辅助线颜色。</summary>
        public SKColor Grid { get; set; } = new SKColor(0x2A, 0x33, 0x40);

        /// <summary>第二种强调色（在 Primary 之外）。</summary>
        public SKColor Highlight { get; set; } = new SKColor(0xFF, 0xC8, 0x57);

        /// <summary>未传入主题时使用的默认配色（复制自自然主题）。</summary>
        public static ContentTheme CreateDefault() => From(NBVideoThemes.Nature);

        /// <summary>基于任一内置主题复制出一套可扩展的 ContentTheme。</summary>
        public static ContentTheme From(NBVideoTheme theme)
        {
            ArgumentNullException.ThrowIfNull(theme);

            return new ContentTheme
            {
                Name = theme.Name,
                Primary = theme.Primary,
                Background = theme.Background,
                TextPrimary = theme.TextPrimary,
                TextSecondary = theme.TextSecondary,
                Divider = theme.Divider,
                Positive = theme.Positive,
                Negative = theme.Negative
            };
        }
    }
}
