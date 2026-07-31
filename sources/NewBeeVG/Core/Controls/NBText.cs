using SkiaSharp;
using static NewBeeVG.NBTextUtils;

namespace NewBeeVG;

public class NBText : NBLayoutable, IPaddingable, IOrientation, INBTextRun, IRightToLeft,ILineSpacing
{
    /// <summary>
    /// 文本方向：横向或纵向。
    /// </summary>
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>
    /// 是否右向左阅读。目前仅 Orientation = Orientation.Vertical 时生效。
    /// </summary>
    public bool RightToLeft { get; set; } = false;

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

    public bool StrokesFirst { get; set; } = true;

    public NBStrokeCollection Strokes { get; private set; } = new NBStrokeCollection();

    /// <summary>
    /// 行高；如果为 NaN，则自动按字体度量计算。
    /// </summary>
    public double LineHeight { get; set; } = float.NaN;

    /// <summary>
    /// 字间距。
    /// </summary>
    public double LetterSpacing { get; set; }

    public double LineSpacing { get; set; }

    /// <summary>
    /// 最大行数；如果为 null 表示不限行。
    /// </summary>
    public int? MaxLines { get; set; }

    /// <summary>
    /// 文本水平对齐方式。
    /// </summary>
    public NBTextAlign TextAlign { get; set; } = NBTextAlign.LeftOrTop;

    private float GetLetterSpacingWithStroke()
    {
        return (float)LetterSpacing + GetStrokeMargin();
    }

    /// <summary>
    /// 测量文本控件在给定可用空间下所需的尺寸。
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;

        if (Orientation == Orientation.Horizontal)
        {
            return MeasureHorizontal(availableSize, padding);
        }
        else
        {
            return MeasureVertical(availableSize, padding);
        }
    }

    private Size MeasureHorizontal(Size availableSize, Thickness padding)
    {
        var innerAvailableLength = GetInnerAvailableWidth(availableSize.Width, padding);

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface);

        float eps = 0.001f;
        var lineHeight = GetLineHeight(font);
        var lines = BuildLines(Text, innerAvailableLength + eps, font);

        float contentWidth = 0;
        foreach (var line in lines)
        {
            contentWidth = (float)Math.Max(contentWidth, MeasureLineLength(line, font));
        }

        double contentHeight = lines.Count * lineHeight;

        return new Size(
            contentWidth + padding.Left + padding.Right,
            contentHeight + padding.Top + padding.Bottom);
    }

    private Size MeasureVertical(Size availableSize, Thickness padding)
    {
        var innerAvailableLength = GetInnerAvailableHeight(availableSize.Height, padding);

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface);

        float eps = 0.001f;
        var lines = BuildLines(Text, innerAvailableLength + eps, font);

        float contentHeight = 0;
        float contentWidth = 0;
        foreach (var line in lines)
        {
            contentHeight = (float)Math.Max(contentHeight, MeasureLineLength(line, font));
            contentWidth += (float)GetLineWidth(font, line);
        }

        return new Size(
            contentWidth + padding.Left + padding.Right,
            contentHeight + padding.Top + padding.Bottom);
    }

    /// <summary>
    /// 计算文本中单个字符的最大宽度，用于纵向排版时确定列宽。
    /// </summary>
    /// <param name="font"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    private float GetMaxTextWidth(SKFont font, string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        float maxWidth = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            float w = font.MeasureText(rune.ToString());
            if (w > maxWidth)
                maxWidth = w;
        }
        return maxWidth;
    }

    protected override void RenderContent(SKCanvas context)
    {
        if (Orientation == Orientation.Horizontal)
        {
            RenderHorizontalText(context);
        }
        else
        {
            RenderVerticalText(context);
        }
    }

    /// <summary>
    /// 在当前 Bounds 内绘制文本。
    /// </summary>
    private void RenderHorizontalText(SKCanvas context)
    {
        if (context == null) return;
        if (string.IsNullOrEmpty(Text)) return;

        var bounds = Bounds;
        var padding = Padding;

        float eps = 0.001f;

        var innerLeft = bounds.Left + (float)padding.Left;
        var innerTop = bounds.Top + (float)padding.Top;
        var innerWidth = Math.Max(0, eps + bounds.Width - (float)padding.Left - (float)padding.Right);
        var innerHeight = Math.Max(0, eps + bounds.Height - (float)padding.Top - (float)padding.Bottom);

        if (innerWidth <= 0 || innerHeight <= 0)
            return;

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface);
        using var paint = CreateFillTextPaint();

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
                lines[^1] = TrimToLength(lines[^1], innerWidth, font);
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
            var lineWidth = MeasureLineLength(line, font);

            var x = GetLineX(innerLeft, innerWidth, lineWidth) + GetStrokeMargin() * 0.5f;
            var y = lineTop - metrics.Ascent;

            if (Strokes.IsEmpty() == false)
            {
                if (StrokesFirst == true)
                {
                    DrawStrokes(context, font, line, x, y, (float)lineHeight);
                    DrawLine(context, font, paint, line, x, y, false, (float)lineHeight);
                }
                else
                {
                    DrawLine(context, font, paint, line, x, y, false, (float)lineHeight);
                    DrawStrokes(context, font, line, x, y, (float)lineHeight);
                }
            }
            else
            {
                DrawLine(context, font, paint, line, x, y, false, (float)lineHeight);
            }
        }

        context.Restore();
    }

    private void DrawStrokes(SKCanvas context, SKFont font, string line, float x, float y, float? maxLineWidth = null)
    {
        Strokes.ForEachStroke(s =>
        {
            using var strokePaint = s.CreatePaint();
            DrawLine(context, font, strokePaint, line, x, y, true, maxLineWidth);
        });
    }

    private void RenderVerticalText(SKCanvas context)
    {
        if (context == null) return;
        if (string.IsNullOrEmpty(Text)) return;

        var bounds = Bounds;
        var padding = Padding;

        float eps = 0.001f;
        var innerLeft = bounds.Left + (float)padding.Left;
        var innerTop = bounds.Top + (float)padding.Top;
        var innerWidth = Math.Max(0, eps + bounds.Width - (float)padding.Left - (float)padding.Right);
        var innerHeight = Math.Max(0, eps + bounds.Height - (float)padding.Top - (float)padding.Bottom);

        if (innerWidth <= 0 || innerHeight <= 0)
            return;

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface);
        using var paint = CreateFillTextPaint();

        var metrics = font.Metrics;
        var maxLength = innerHeight;

        // 先按当前高度排版出最终可绘制的行。
        var lines = BuildLines(Text, maxLength, font);

        // 限制最大行数。
        if (MaxLines.HasValue && MaxLines.Value > 0 && lines.Count > MaxLines.Value)
        {
            lines = lines.GetRange(0, MaxLines.Value);

            if (IsTrimming && maxLength > 0)
            {
                lines[^1] = TrimToLength(lines[^1], maxLength, font);
            }
        }

        context.Save();
        context.ClipRect(bounds);

        bool rightToLeft = RightToLeft;
        var xStart = rightToLeft ? innerLeft + innerWidth : innerLeft;
        int direction = rightToLeft ? -1 : 1;
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            var maxLineWidth = (float)GetLineWidth(font, line);
            var lineHeight = MeasureLineLength(line, font);

            var lineTop = GetLineY(innerTop, innerHeight, lineHeight);

            var x = (rightToLeft ? xStart - maxLineWidth : xStart);
            var y = lineTop - metrics.Ascent + GetStrokeMargin() * 0.5f;

            if (Strokes.IsEmpty() == false)
            {
                if (StrokesFirst == true)
                {
                    DrawStrokes(context, font, line, x, y, maxLineWidth);
                    DrawLine(context, font, paint, line, x, y, false, maxLineWidth);
                }
                else
                {
                    DrawLine(context, font, paint, line, x, y, false, maxLineWidth);
                    DrawStrokes(context, font, line, x, y, maxLineWidth);
                }
            }
            else
            {
                DrawLine(context, font, paint, line, x, y, false, maxLineWidth);
            }

            if (rightToLeft)
                xStart -= (maxLineWidth);
            else
                xStart += (maxLineWidth);
        }

        context.Restore();
    }

    /// <summary>
    /// 创建字体族对应的 Typeface。
    /// </summary>
    private SKTypeface CreateTypeface()
    {
        var fontStyle = new SKFontStyle((int)FontWeight, (int)FontWidth, FontSlant);
        var font = SKTypeface.FromFamilyName(FontFamily, fontStyle);
        return font ?? SKTypeface.Default;
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
    private SKPaint CreateFillTextPaint()
    {
        return new SKPaint
        {
            IsAntialias = true,
            Color = Foreground,
            IsDither = true,
        };
    }

    private float GetStrokeMargin()
    {
        return Strokes.GetMaxStrokeWidth();
    }

    /// <summary>
    /// 获取文本内容的有效行高。
    /// </summary>
    private double GetLineHeight(SKFont font)
    {
        if (double.IsNaN(LineHeight) == false) return LineHeight;

        var metrics = font.Metrics;
        var lineHeight = Math.Ceiling(metrics.Descent - metrics.Ascent + metrics.Leading) + LineSpacing;
        return lineHeight + GetStrokeMargin();
    }

    private double GetLineWidth(SKFont font, string txt)
    {
        if (double.IsNaN(LineHeight) == false) return LineHeight;

        var metrics = font.Metrics;
        var lineWidth = GetMaxTextWidth(font, txt) + LineSpacing;
        return lineWidth + GetStrokeMargin();
    }

    /// <summary>
    /// 计算文本在可用宽度下生成的最终行列表。
    /// </summary>
    private List<string> BuildLines(string text, double innerAvailableLength, SKFont font)
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
            if (!IsWrapText || double.IsPositiveInfinity(innerAvailableLength))
            {
                lines.Add(paragraph);
                continue;
            }

            if (innerAvailableLength <= 0)
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
                int count = FitPrefix(rest, (float)innerAvailableLength, font);
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
            if (IsTrimming && !double.IsPositiveInfinity(innerAvailableLength) && innerAvailableLength > 0)
            {
                lines[^1] = TrimToLength(lines[^1], (float)innerAvailableLength, font);
            }
        }

        return lines;
    }

    /// <summary>
    /// 计算单行文本的长度。
    /// </summary>
    private double MeasureLineLength(string value, SKFont font)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (Orientation == Orientation.Horizontal)
        {
            // 使用新的 SkiaSharp API 进行测量，避免使用已废弃的 SKPaint.MeasureText。
            double len = font.MeasureText(value);

            // 字间距需要手动累加。
            int runeCount = CountRunes(value);

            var letterSpacing = GetLetterSpacingWithStroke();

            if (letterSpacing != 0 && runeCount > 1)
                len += letterSpacing * (runeCount - 1);

            return len + GetStrokeMargin();
        }
        else
        {
            var metrics = font.Metrics;
            float lineHeight = Math.Abs(metrics.Ascent - metrics.Descent + metrics.Leading);
            int runeCount = CountRunes(value);
            double len = runeCount * lineHeight + (runeCount - 1) * GetLetterSpacingWithStroke();
            return len + GetStrokeMargin();
        }
    }

    /// <summary>
    /// 计算在指定尺寸内最多可容纳多少个“字符单元”（按 Rune 切分，避免拆坏代理项）。
    /// </summary>
    private int FitPrefix(string value, float maxLength, SKFont font)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (MeasureLineLength(value, font) <= maxLength)
            return CountRunes(value);

        int lo = 1;
        int hi = CountRunes(value);
        int best = 1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            var prefix = SubstringByRunes(value, mid);
            float width = (float)MeasureLineLength(prefix, font);

            if (width <= maxLength)
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
    private string TrimToLength(string value, float max, SKFont font)
    {
        const string ellipsis = "…";

        if (MeasureLineLength(ellipsis, font) > max)
            return string.Empty;

        if (MeasureLineLength(value, font) <= max)
            return value;

        float ellipsisWidth = (float)MeasureLineLength(ellipsis, font);
        float prefixWidth = max - ellipsisWidth;

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
            NBTextAlign.Center => left + (float)((width - lineWidth) / 2.0),
            NBTextAlign.RightOrBottom => left + (float)(width - lineWidth),
            _ => left
        };
    }

    /// <summary>
    /// 获取文本行的起始 Y 坐标，支持上对齐 / 居中 / 下对齐。
    /// </summary>
    /// <param name="top"></param>
    /// <param name="height"></param>
    /// <param name="lineHeight"></param>
    /// <returns></returns>
    private float GetLineY(float top, float height, double lineHeight)
    {
        return TextAlign switch
        {
            NBTextAlign.Center => top + (float)((height - lineHeight) / 2.0),
            NBTextAlign.RightOrBottom => top + (float)(height - lineHeight),
            _ => top
        };
    }

    /// <summary>
    /// 绘制单行文本；如果设置了字间距，则按 Rune 逐个绘制。
    /// </summary>
    private void DrawLine(SKCanvas context, SKFont font, SKPaint paint, string line, float x, float y, bool isStroke = false, float? maxLineWidth = null)
    {
        if (Orientation == Orientation.Horizontal)
            DrawHorizontalLine(context, font, paint, line, x, y, isStroke, GetLetterSpacingWithStroke(), maxLineWidth);
        else
            DrawVerticalLine(context, font, paint, line, x, y, isStroke, GetLetterSpacingWithStroke(), maxLineWidth);
    }
}

public static partial class NBExtentions
{
    //public static TWidget FontSize<TWidget>(this TWidget widget, float fontSize) where TWidget : NBText
    //{
    //    widget.FontSize = fontSize;
    //    return widget;
    //}

    //public static TWidget Font<TWidget>(this TWidget widget, float size, SKColor? color = null) where TWidget : NBText
    //{
    //    widget.FontSize = size;
    //    if(color.HasValue)
    //    {
    //        widget.Foreground = color.Value;
    //    }
    //    return widget;
    //}

    //public static TWidget Text<TWidget>(this TWidget widget, string text) where TWidget : NBText
    //{
    //    widget.Text = text;
    //    return widget;
    //}

    //public static TWidget FontFamily<TWidget>(this TWidget widget, string fontFamily) where TWidget : NBText
    //{
    //    widget.FontFamily = fontFamily;
    //    return widget;
    //}

    //public static TWidget FontWeight<TWidget>(this TWidget widget, SKFontStyleWeight fontWeight) where TWidget : NBText
    //{
    //    widget.FontWeight = fontWeight;
    //    return widget;
    //}

    //public static TWidget FontWidth<TWidget>(this TWidget widget, SKFontStyleWidth fontWidth) where TWidget : NBText
    //{
    //    widget.FontWidth = fontWidth;
    //    return widget;
    //}

    //public static TWidget FontSlant<TWidget>(this TWidget widget, SKFontStyleSlant fontSlant) where TWidget : NBText
    //{
    //    widget.FontSlant = fontSlant;
    //    return widget;
    //}

    //public static TWidget Foreground<TWidget>(this TWidget widget, SKColor color) where TWidget : NBText
    //{
    //    widget.Foreground = color;
    //    return widget;
    //}

    //public static TWidget StrokeFirst<TWidget>(this TWidget widget, bool strokesFirst) where TWidget : NBText
    //{
    //    widget.StrokesFirst = strokesFirst;
    //    return widget;
    //}

    //public static TWidget Strokes<TWidget>(this TWidget widget, SKColor color, float width, SKStrokeCap cap = SKStrokeCap.Round, SKStrokeJoin join = SKStrokeJoin.Bevel) where TWidget : NBText
    //{
    //    var stroke = new NBStroke { StrokeCap = cap, StrokeColor = color, StrokeJoin = join, StrokeWidth = width * 2 };
    //    widget.Strokes.ClearStrokes();
    //    widget.Strokes.AddStroke(stroke);
    //    return widget;
    //}

    //public static TWidget AddStroke<TWidget>(this TWidget widget, SKColor color, float width, SKStrokeCap cap = SKStrokeCap.Round, SKStrokeJoin join = SKStrokeJoin.Bevel) where TWidget : NBText
    //{
    //    var stroke = new NBStroke { StrokeCap = cap, StrokeColor = color, StrokeJoin = join, StrokeWidth = width * 2 };
    //    widget.Strokes.AddStroke(stroke);
    //    return widget;
    //}

    //public static TWidget ClearStrokes<TWidget>(this TWidget widget) where TWidget : NBText
    //{
    //    widget.Strokes.ClearStrokes();
    //    return widget;
    //}

    //public static TWidget Fg<TWidget>(this TWidget widget, SKColor color) where TWidget : NBText
    //{
    //    widget.Foreground = color;
    //    return widget;
    //}

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

    public static TWidget MaxLines<TWidget>(this TWidget widget, int? maxLines) where TWidget : NBText
    {
        widget.MaxLines = maxLines;
        return widget;
    }

    public static TWidget TextAlign<TWidget>(this TWidget widget, int textAlign) where TWidget : NBText
    {
        widget.TextAlign =textAlign.ToNBTextAlign();
        return widget;
    }
}