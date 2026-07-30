using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// 富文本中的一个样式区间。
/// </summary>
public class NBTextRun : NBVisual, INBTextRun
{
    public string Text { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 40;
    public SKFontStyleWeight FontWeight { get; set; } = SKFontStyleWeight.Normal;
    public SKFontStyleWidth FontWidth { get; set; } = SKFontStyleWidth.Normal;
    public SKFontStyleSlant FontSlant { get; set; } = SKFontStyleSlant.Upright;

    public SKColor Foreground { get; set; } = SKColors.Black;

    public bool StrokesFirst { get; set; } = true;

    public NBStrokeCollection Strokes { get; set; } = new NBStrokeCollection();

    /// <summary>
    /// 行高；如果为 NaN，则自动按字体度量计算。
    /// </summary>
    public float LineHeight { get; set; } = float.NaN;

    /// <summary>
    /// 字间距。
    /// </summary>
    public float LetterSpacing { get; set; } = 0f;

    // 创建对应的 SKTypeface（可缓存优化，此处每次创建）
    public SKTypeface CreateTypeface()
    {
        var style = new SKFontStyle((int)FontWeight, (int)FontWidth, FontSlant);
        var tf = SKTypeface.FromFamilyName(FontFamily, style);
        return tf ?? SKTypeface.Default;
    }

    public SKFont CreateFont(SKTypeface typeface)
    {
        return new SKFont(typeface, FontSize);
    }

    public SKPaint CreateFillPaint()
    {
        return new SKPaint
        {
            IsAntialias = true,
            Color = Foreground,
            IsDither = true
        };
    }

    /// <summary>
    /// 获取本 Run 的有效行高（未加全局字间距与描边前的基本值）。
    /// </summary>
    public float GetBaseLineHeight(SKFont font)
    {
        var m = font.Metrics;
        return (float)Math.Ceiling(m.Descent - m.Ascent + m.Leading);
    }

    /// <summary>
    /// 获取本 Run 描边带来的额外边距（取最大描边宽度）。
    /// </summary>
    public float GetStrokeMargin()
    {
        return Strokes?.GetMaxStrokeWidth() ?? 0;
    }
}