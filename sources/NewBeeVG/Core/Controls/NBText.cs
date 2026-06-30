using SkiaSharp;

namespace NewBeeVG;

public class NBText : NBLayoutable, IPaddingable
{
    /// <summary>
    /// 文本内容的内边距。
    /// </summary>
    public Thickness Padding { get; set; }

    /// <summary>
    /// 文本内容。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 字体族名称。
    /// </summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>
    /// 字号。
    /// </summary>
    public float FontSize { get; set; } = 40;

    /// <summary>
    /// 字体粗细。
    /// </summary>
    public SKFontStyleWeight FontWeight { get; set; } = SKFontStyleWeight.Normal;

    /// <summary>
    /// 字体宽度。
    /// </summary>
    public SKFontStyleWidth FontWidth { get; set; } = SKFontStyleWidth.Normal;

    /// <summary>
    /// 字体倾斜样式。
    /// </summary>
    public SKFontStyleSlant FontSlant { get; set; } = SKFontStyleSlant.Upright;

    /// <summary>
    /// 是否自动换行。
    /// </summary>
    public bool IsWrapText { get; set; } = false;

    /// <summary>
    /// 超出宽度时是否截断。
    /// </summary>
    public bool IsTrimming { get; set; } = false;

    /// <summary>
    /// 文字颜色。
    /// </summary>
    public SKColor Foreground { get; set; } = SKColors.Black;

    /// <summary>
    /// 行高；如果为 NaN，则自动按字体度量计算。
    /// </summary>
    public float LineHeight { get; set; } = float.NaN;

    /// <summary>
    /// 字间距。
    /// </summary>
    public float LetterSpacing { get; set; } = 0f;

    /// <summary>
    /// 最大行数；如果为 null 表示不限行。
    /// </summary>
    public int? MaxLines { get; set; }

    /// <summary>
    /// 文本水平对齐方式。
    /// </summary>
    public SKTextAlign TextAlign { get; set; } = SKTextAlign.Left;

    /// <summary>
    /// 测量文本控件在给定可用空间下所需的尺寸。
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var innerAvailableWidth = GetInnerAvailableWidth(availableSize.Width, padding);

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface);

        var lineHeight = GetLineHeight(font);
        var lines = BuildLines(Text, innerAvailableWidth, font);

        double contentWidth = 0;
        foreach (var line in lines)
        {
            contentWidth = Math.Max(contentWidth, MeasureLineWidth(line, font));
        }

        double contentHeight = lines.Count * lineHeight;

        // 将内容尺寸加上内边距后返回。
        return new Size(
            contentWidth + padding.Left + padding.Right,
            contentHeight + padding.Top + padding.Bottom);
    }

    /// <summary>
    /// 在子元素绘制完成后绘制文本本身。
    /// </summary>
    protected override void RenderDecorations(SKCanvas context)
    {
        RenderText(context);
    }

    /// <summary>
    /// 在当前 Bounds 内绘制文本。
    /// </summary>
    private void RenderText(SKCanvas context)
    {
        if (context == null) return;
        if (string.IsNullOrEmpty(Text)) return;

        var bounds = Bounds;
        var padding = Padding;

        var innerLeft = bounds.Left + (float)padding.Left;
        var innerTop = bounds.Top + (float)padding.Top;
        var innerWidth = Math.Max(0, bounds.Width - (float)padding.Left - (float)padding.Right);
        var innerHeight = Math.Max(0, bounds.Height - (float)padding.Top - (float)padding.Bottom);

        if (innerWidth <= 0 || innerHeight <= 0)
            return;

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface);
        using var paint = CreateTextPaint();

        var metrics = font.Metrics;
        var lineHeight = GetLineHeight(font);

        // 先按当前宽度排版出最终可绘制的行。
        var lines = BuildLines(Text, innerWidth, font);

        // 限制最大行数。
        if (MaxLines.HasValue && MaxLines.Value > 0 && lines.Count > MaxLines.Value)
        {
            lines = lines.GetRange(0, MaxLines.Value);

            if (IsTrimming && innerWidth > 0)
            {
                lines[^1] = TrimToWidth(lines[^1], innerWidth, font);
            }
        }

        context.Save();
        context.ClipRect(bounds);

        for (int i = 0; i < lines.Count; i++)
        {
            var lineTop = innerTop + (float)(i * lineHeight);
            if (lineTop > bounds.Bottom)
                break;

            var line = lines[i];
            var lineWidth = MeasureLineWidth(line, font);

            // 根据 TextAlign 计算当前行的起始 X 坐标。
            var x = GetLineX(innerLeft, innerWidth, lineWidth);
            var y = lineTop - metrics.Ascent;

            DrawLine(context, font, paint, line, x, y);
        }

        context.Restore();
    }

    /// <summary>
    /// 创建字体族对应的 Typeface。
    /// </summary>
    private SKTypeface CreateTypeface()
    {
        var fontStyle = new SKFontStyle((int)FontWeight, (int)FontWidth, FontSlant);
        return SKTypeface.FromFamilyName(FontFamily, fontStyle) ?? SKTypeface.Default;
    }

    /// <summary>
    /// 创建用于测量的字体对象。
    /// </summary>
    private SKFont CreateFont(SKTypeface typeface)
    {
        return new SKFont(typeface, FontSize);
    }

    /// <summary>
    /// 创建用于绘制的画笔。
    /// </summary>
    private SKPaint CreateTextPaint()
    {
        return new SKPaint
        {
            IsAntialias = true,
            Color = Foreground, 
            IsDither = true,
        };
    }

    /// <summary>
    /// 获取文本内容的有效行高。
    /// </summary>
    private double GetLineHeight(SKFont font)
    {
        var metrics = font.Metrics;
        var lineHeight = double.IsNaN(LineHeight)
            ? Math.Ceiling(metrics.Descent - metrics.Ascent + metrics.Leading)
            : LineHeight;

        return lineHeight < 0 ? 0 : lineHeight;
    }

    /// <summary>
    /// 计算文本在可用宽度下生成的最终行列表。
    /// </summary>
    private List<string> BuildLines(string text, double innerAvailableWidth, SKFont font)
    {
        var lines = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            lines.Add(string.Empty);
            return lines;
        }

        // 统一换行符，避免 \r\n / \r / \n 混用造成判断复杂。
        var paragraphs = NormalizeText(text).Split('\n');

        foreach (var paragraph in paragraphs)
        {
            // 不换行，或者宽度无限大时，直接作为一行。
            if (!IsWrapText || double.IsPositiveInfinity(innerAvailableWidth))
            {
                lines.Add(paragraph);
                continue;
            }

            if (innerAvailableWidth <= 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            // 按宽度不断切分，生成多行。
            var rest = paragraph;
            while (rest.Length > 0)
            {
                int count = FitPrefix(rest, (float)innerAvailableWidth, font);
                if (count <= 0)
                    count = 1;

                lines.Add(SubstringByRunes(rest, count));
                rest = RemovePrefixByRunes(rest, count);
            }
        }

        // 如果设置了最大行数，则在这里统一裁剪。
        if (MaxLines.HasValue && MaxLines.Value > 0 && lines.Count > MaxLines.Value)
        {
            lines = lines.GetRange(0, MaxLines.Value);

            // 如果需要截断，最后一行要补省略号。
            if (IsTrimming && !double.IsPositiveInfinity(innerAvailableWidth) && innerAvailableWidth > 0)
            {
                lines[^1] = TrimToWidth(lines[^1], (float)innerAvailableWidth, font);
            }
        }

        return lines;
    }

    /// <summary>
    /// 计算单行文本的宽度。
    /// </summary>
    private double MeasureLineWidth(string value, SKFont font)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        // 使用新的 SkiaSharp API 进行测量，避免使用已废弃的 SKPaint.MeasureText。
        double width = font.MeasureText(value);

        // 字间距需要手动累加。
        int runeCount = CountRunes(value);
        if (LetterSpacing != 0 && runeCount > 1)
            width += LetterSpacing * (runeCount - 1);

        return width;
    }

    /// <summary>
    /// 计算在指定宽度内最多可容纳多少个“字符单元”（按 Rune 切分，避免拆坏代理项）。
    /// </summary>
    private int FitPrefix(string value, float maxWidth, SKFont font)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (MeasureLineWidth(value, font) <= maxWidth)
            return CountRunes(value);

        int lo = 1;
        int hi = CountRunes(value);
        int best = 1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            var prefix = SubstringByRunes(value, mid);
            float width = (float)MeasureLineWidth(prefix, font);

            if (width <= maxWidth)
            {
                best = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return best;
    }

    /// <summary>
    /// 将文本裁剪到指定宽度，并在末尾附加省略号。
    /// </summary>
    private string TrimToWidth(string value, float maxWidth, SKFont font)
    {
        const string ellipsis = "…";

        if (MeasureLineWidth(ellipsis, font) > maxWidth)
            return string.Empty;

        if (MeasureLineWidth(value, font) <= maxWidth)
            return value;

        float ellipsisWidth = (float)MeasureLineWidth(ellipsis, font);
        float prefixWidth = maxWidth - ellipsisWidth;

        if (prefixWidth <= 0)
            return ellipsis;

        int count = FitPrefix(value, prefixWidth, font);
        return SubstringByRunes(value, count) + ellipsis;
    }

    /// <summary>
    /// 获取文本行的起始 X 坐标，支持左对齐 / 居中 / 右对齐。
    /// </summary>
    private float GetLineX(float left, float width, double lineWidth)
    {
        return TextAlign switch
        {
            SKTextAlign.Center => left + (float)((width - lineWidth) / 2.0),
            SKTextAlign.Right => left + (float)(width - lineWidth),
            _ => left
        };
    }

    /// <summary>
    /// 绘制单行文本；如果设置了字间距，则按 Rune 逐个绘制。
    /// </summary>
    private void DrawLine(SKCanvas context, SKFont font, SKPaint paint, string line, float x, float y)
    {
        if (string.IsNullOrEmpty(line))
            return;

        // 没有字间距时，直接整体绘制即可。
        if (LetterSpacing == 0 || CountRunes(line) <= 1)
        {
            context.DrawText(line, x, y, font, paint);
            return;
        }

        // 有字间距时，逐个 Rune 绘制。
        float currentX = x;

        foreach (var rune in line.EnumerateRunes())
        {
            var runeText = rune.ToString();
            context.DrawText(runeText, currentX, y, font, paint);
            currentX += (float)font.MeasureText(runeText) + LetterSpacing;
        }
    }

    /// <summary>
    /// 统一文本换行符，便于后续处理。
    /// </summary>
    private static string NormalizeText(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    /// <summary>
    /// 计算字符串中 Rune 的数量。
    /// </summary>
    private static int CountRunes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        int count = 0;
        foreach (var _ in value.EnumerateRunes())
            count++;

        return count;
    }

    /// <summary>
    /// 按 Rune 数量截取前缀，不会截断代理项。
    /// </summary>
    private static string SubstringByRunes(string value, int runeCount)
    {
        if (string.IsNullOrEmpty(value) || runeCount <= 0)
            return string.Empty;

        int utf16Length = 0;
        int taken = 0;

        foreach (var rune in value.EnumerateRunes())
        {
            if (taken >= runeCount)
                break;

            utf16Length += rune.Utf16SequenceLength;
            taken++;
        }

        return value.Substring(0, utf16Length);
    }

    /// <summary>
    /// 按 Rune 数量删除前缀，返回剩余文本。
    /// </summary>
    private static string RemovePrefixByRunes(string value, int runeCount)
    {
        if (string.IsNullOrEmpty(value) || runeCount <= 0)
            return value;

        int utf16Length = 0;
        int taken = 0;

        foreach (var rune in value.EnumerateRunes())
        {
            if (taken >= runeCount)
                break;

            utf16Length += rune.Utf16SequenceLength;
            taken++;
        }

        if (utf16Length >= value.Length)
            return string.Empty;

        return value.Substring(utf16Length);
    }

    private static double GetInnerAvailableWidth(double availableWidth, Thickness padding)
    {
        if (double.IsPositiveInfinity(availableWidth))
            return double.PositiveInfinity;

        return Math.Max(0, availableWidth - padding.Left - padding.Right);
    }
}

public static partial class NBExtentions
{
    public static TWidget FontSize<TWidget>(this TWidget widget, float fontSize) where TWidget : NBText
    {
        widget.FontSize = fontSize;
        return widget;
    }

    public static TWidget Text<TWidget>(this TWidget widget, string text) where TWidget : NBText
    {
        widget.Text = text;
        return widget;
    }

    public static TWidget FontFamily<TWidget>(this TWidget widget, string fontFamily) where TWidget : NBText
    {
        widget.FontFamily = fontFamily;
        return widget;
    }

    public static TWidget FontWeight<TWidget>(this TWidget widget, SKFontStyleWeight fontWeight) where TWidget : NBText
    {
        widget.FontWeight = fontWeight;
        return widget;
    }

    public static TWidget FontWidth<TWidget>(this TWidget widget, SKFontStyleWidth fontWidth) where TWidget : NBText
    {
        widget.FontWidth = fontWidth;
        return widget;
    }

    public static TWidget FontSlant<TWidget>(this TWidget widget, SKFontStyleSlant fontSlant) where TWidget : NBText
    {
        widget.FontSlant = fontSlant;
        return widget;
    }

    public static TWidget WrapText<TWidget>(this TWidget widget, bool wrap = true) where TWidget : NBText
    {
        widget.IsWrapText = wrap;
        return widget;
    }

    public static TWidget Trimming<TWidget>(this TWidget widget, bool trimming = true) where TWidget : NBText
    {
        widget.IsTrimming = trimming;
        return widget;
    }

    public static TWidget Foreground<TWidget>(this TWidget widget, SKColor color) where TWidget : NBText
    {
        widget.Foreground = color;
        return widget;
    }

    public static TWidget LineHeight<TWidget>(this TWidget widget, float lineHeight) where TWidget : NBText
    {
        widget.LineHeight = lineHeight;
        return widget;
    }

    public static TWidget LetterSpacing<TWidget>(this TWidget widget, float letterSpacing) where TWidget : NBText
    {
        widget.LetterSpacing = letterSpacing;
        return widget;
    }

    public static TWidget MaxLines<TWidget>(this TWidget widget, int? maxLines) where TWidget : NBText
    {
        widget.MaxLines = maxLines;
        return widget;
    }

    public static TWidget TextAlign<TWidget>(this TWidget widget, SKTextAlign textAlign) where TWidget : NBText
    {
        widget.TextAlign = textAlign;
        return widget;
    }
}