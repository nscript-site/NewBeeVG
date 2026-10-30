using NewBeeVG;
using SkiaSharp;

// ============================================================
//  动画类文件模板（保存为 MyAnimation.cs）
//  - 只放类型声明：不要写 #!、不要写顶层语句
//  - 类名与文件名一致：MyAnimation.cs / class MyAnimation
//  - 内容参数放 ContentSetting，扩展颜色放 ContentTheme
//  - 测试入口见 MyAnimation_Test.cs
// ============================================================

/// <summary>
/// 示例动画：在整段动画内把数字从 From 增长到 To。
/// </summary>
public class MyAnimation : SKCustomAnimation
{
    private readonly ContentSetting _content;
    private readonly ContentTheme _theme;

    /// <summary>
    /// 创建动画实例。
    /// </summary>
    /// <param name="width">单帧宽度（像素）。</param>
    /// <param name="height">单帧高度（像素）。</param>
    /// <param name="frames">总帧数，有效帧号为 0 ~ frames-1。</param>
    /// <param name="content">内容配置，不能为 null。</param>
    /// <param name="contentTheme">扩展主题；为 null 时使用自然主题的副本。</param>
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

    /// <summary>逐帧绘制逻辑。</summary>
    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        // 透明背景不要清空画布：每帧位图初始就是透明的
        // 只有非透明背景才铺底色
        if (_theme.Background.Alpha > 0)
            canvas.Clear(_theme.Background);

        // Margin 决定内容区域；特效默认填满该矩形
        var m = _content.GetMargin();
        float left = m[0], top = m[1], right = m[2], bottom = m[3];
        float w = size.Width - left - right;
        float h = size.Height - top - bottom;
        if (w <= 0 || h <= 0) return;

        canvas.Save();
        canvas.ClipRect(new SKRect(left, top, left + w, top + h));
        canvas.Translate(left, top);

        // 原始进度 → 按 AnimRatio 压缩后的动画进度
        float raw = Frames <= 1 ? 1f : frame / (float)(Frames - 1);
        float ratio = Math.Clamp(_content.AnimRatio, 0f, 1f);
        float t = ratio <= 0f ? 1f : Math.Clamp(raw / ratio, 0f, 1f);

        double value = _content.From + (_content.To - _content.From) * t;
        string text = value.ToString(_content.Format);

        // 未指定字体时使用官方默认字体，避免跨平台硬编码字体名
        var fontName = string.IsNullOrWhiteSpace(_content.FontFamily)
            ? GetDefaultFontName()
            : _content.FontFamily;

        using var typeface = SKTypeface.FromFamilyName(fontName) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, _content.FontSize);

        // 基础主题色
        using var paint = new SKPaint { IsAntialias = true, Color = _theme.Primary };
        canvas.DrawText(text, w / 2f, h / 2f, SKTextAlign.Center, font, paint);

        // ContentTheme 补充的专用颜色
        using var glow = new SKPaint
        {
            IsAntialias = true,
            Color = _theme.Glow,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f
        };
        canvas.DrawLine(w * 0.2f, h * 0.8f, w * 0.8f, h * 0.8f, glow);

        canvas.Restore();
    }

    /// <summary>
    /// 动画内容配置：把可变输入集中在一个内部类中，避免构造函数参数膨胀。
    /// 必须继承 SKAnimSetting，以支持 Margin 留白。
    /// </summary>
    public class ContentSetting : SKAnimSetting
    {
        /// <summary>起始值。</summary>
        public double From { get; set; } = 0;

        /// <summary>结束值。</summary>
        public double To { get; set; } = 10000;

        /// <summary>数字格式，例如 "N0"（千分位）、"F2"（两位小数）。</summary>
        public string Format { get; set; } = "N0";

        /// <summary>字体名；为空时使用 SKCustomAnimation.GetDefaultFontName()。</summary>
        public string FontFamily { get; set; } = "";

        /// <summary>字号（像素）。</summary>
        public float FontSize { get; set; } = 160f;
    }

    /// <summary>
    /// 扩展主题：NBVideoTheme 的 7 个基础颜色不够用时，
    /// 继承它并补充专用颜色。
    /// </summary>
    public class ContentTheme : NBVideoTheme
    {
        /// <summary>辉光 / 装饰线颜色。</summary>
        public SKColor Glow { get; set; } = new SKColor(0x66, 0xE0, 0xFF);

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
