using SkiaSharp;
using static NewBeeVG.NBTextUtils;

namespace NewBeeVG;

/// <summary>
/// 富文本中的一个样式区间。
/// </summary>
public class NBTextRun : NBVisual, INBTextRun
{
    public string Text { get; set {
            field = NormalizeText(value);
        } } = string.Empty;

    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 40;
    public SKFontStyleWeight FontWeight { get; set; } = SKFontStyleWeight.Normal;
    public SKFontStyleWidth FontWidth { get; set; } = SKFontStyleWidth.Normal;
    public SKFontStyleSlant FontSlant { get; set; } = SKFontStyleSlant.Upright;

    public SKColor Foreground { get; set; } = SKColors.Black;

    public bool StrokesFirst { get; set; } = true;

    public NBStrokeCollection Strokes { get; set; } = new NBStrokeCollection();
    
    internal Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>
    /// 行高；如果为 NaN，则自动按字体度量计算。
    /// </summary>
    public double LineHeight { get; set; } = double.NaN;

    /// <summary>
    /// 字间距。
    /// </summary>
    public double LetterSpacing { get; set; } = 0f;

    internal List<NBTextRunClipInfo> Clips { get; set; } = new List<NBTextRunClipInfo>();

    internal SKPoint Origin { get; set; } = new SKPoint();

    internal void UpdateLayout(SKPoint origin)
    {
        SKRect? maxRect = null;
        foreach(var clip in Clips)
        {
            var b = clip.GetBound();
            var x = b.Left + origin.X;
            var y = b.Top + origin.Y;
            b = new SKRect(x, y, x + b.Width, y + b.Height);
            if (maxRect == null) maxRect = b;
            else maxRect = SKRect.Union(maxRect.Value, b);
        }
        this.Bounds = maxRect ?? new SKRect(origin.X, origin.Y, origin.X, origin.Y);
        Origin = origin;
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

    protected override void RenderContent(SKCanvas context)
    {
        context.Save();

        //var pbg = new NBColorBrush(SKColors.Green).GetPaint();
        //context.DrawRect(Bounds, pbg);

        SKPoint origin = Origin;
        foreach (var clip in Clips)
        {
            using var paint = CreateFillTextPaint();
            using var typeface = CreateTypeface();
            using var font = CreateFont(typeface);
            var line = clip.Text;
            float x = clip.X + origin.X + clip.DeltaX;
            float y = clip.Y + origin.Y + clip.DeltaY;
            float maxLineWidth = clip.Height;
            if (Strokes.IsEmpty() == false)
            {
                if (StrokesFirst == true)
                {
                    DrawStrokes(context, clip.Orientation, font, line, x, y, maxLineWidth);
                    DrawLine(context, clip.Orientation, font, paint, line, x, y, false, maxLineWidth);
                }
                else
                {
                    DrawLine(context, clip.Orientation, font, paint, line, x, y, false, maxLineWidth);
                    DrawStrokes(context, clip.Orientation, font, line, x, y, maxLineWidth);
                }
            }
            else
            {
                DrawLine(context, clip.Orientation, font, paint, line, x, y, false, maxLineWidth);
            }

        }
        context.Restore();
    }
    private void DrawStrokes(SKCanvas context, Orientation orientation, SKFont font, string line, float x, float y, float? maxLineWidth = null)
    {
        Strokes.ForEachStroke(s =>
        {
            using var strokePaint = s.CreatePaint();
            DrawLine(context, orientation, font, strokePaint, line, x, y, true, maxLineWidth);
        });
    }

    private float GetLetterSpacingWithStroke()
    {
        return (float)LetterSpacing + GetStrokeMargin();
    }

    /// <summary>
    /// 绘制单行文本；如果设置了字间距，则按 Rune 逐个绘制。
    /// </summary>
    private void DrawLine(SKCanvas context, Orientation orientation, SKFont font, SKPaint paint, string line, float x, float y, bool isStroke = false, float? maxLineWidth = null)
    {
        if (orientation == Orientation.Horizontal)
            DrawHorizontalLine(context, font, paint, line, x, y, isStroke, GetLetterSpacingWithStroke());
        else
            DrawVerticalLine(context, font, paint, line, x, y, isStroke, GetLetterSpacingWithStroke(), maxLineWidth);
    }

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
    /// 获取本 Run 描边带来的额外边距（取最大描边宽度）。
    /// </summary>
    public float GetStrokeMargin()
    {
        return Strokes?.GetMaxStrokeWidth() ?? 0;
    }

    /// <summary>
    /// 计算文本在可用宽度下生成的最终行列表。返回值为 (content, lineLength, lineHeight, isNewLine, ascent) 的列表。
    /// </summary>
    internal List<(string, float, float,bool,float)> BuildLines(double firstLineAvailableLength, double innerAvailableLength)
    {
        var text = Text;
        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface);  

        float ascent = font.Metrics.Ascent;

        var lines = new List<(string, float, float,bool,float)>();

        if (string.IsNullOrEmpty(text))
        {
            lines.Add((string.Empty, 0, 0,false, ascent));
            return lines;
        }

        var paragraphs = text.Split('\n');

        foreach (var paragraph in paragraphs)
        {
            if (innerAvailableLength <= 0)
            {
                lines.Add((string.Empty, 0, 0,false, ascent));
                continue;
            }

            if (paragraph.Length == 0)
            {
                lines.Add((string.Empty, 0, 0,false, ascent));   
                continue;
            }

            // 按宽度不断切分，生成多行。
            var rest = paragraph;
            while (rest.Length > 0)
            {
                if(Math.Abs((firstLineAvailableLength - innerAvailableLength)) < 0.0001)
                {
                    int count = FitPrefix(rest, (float)innerAvailableLength, font);
                    if (count <= 0)
                        count = 1;

                    var str = SubstringByRunes(rest, count);
                    var lineLength = (float)MeasureLineLength(str, font);
                    var maxLineWidth = (float)MeasureLineHeight(str, font);
                    lines.Add((str, lineLength, maxLineWidth, true, ascent));
                    rest = RemovePrefixByRunes(rest, count);
                }
                else
                {
                    bool isNewLine = lines.Count > 0;
                    int count1 = isNewLine ? 0 : MustFitPrefix(rest, (float)firstLineAvailableLength, font);
                    int count2 = FitPrefix(rest, (float)innerAvailableLength, font);
                    int count = 0;

                    if (count1 <= 0)
                    {
                        isNewLine = true;
                        if (count2 <= 0)
                        {
                            count = 1;
                        }
                        else
                        {
                            count = count2;
                        }
                    }
                    else
                    {
                        count = count1;
                    }

                    var str = SubstringByRunes(rest, count);
                    var lineLength = (float)MeasureLineLength(str, font);
                    var maxLineWidth = (float)MeasureLineHeight(str, font);
                    lines.Add((str, lineLength, maxLineWidth, isNewLine, ascent));
                    rest = RemovePrefixByRunes(rest, count);
                }
            }
        }

        return lines;
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

    private int MustFitPrefix(string value, float maxLength, SKFont font)
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

        var lastPrefix = SubstringByRunes(value, best);
        var lastWidth = (float)MeasureLineLength(lastPrefix, font);
        if(lastWidth  <= maxLength) 
            return best;
        else
            return best - 1;
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
    /// 获得单行文本的高度（横向为字体度量高度，纵向为最大字符宽度）。
    /// </summary>
    /// <param name="text"></param>
    /// <param name="font"></param>
    /// <returns></returns>
    private double MeasureLineHeight(string text, SKFont font)
    {
        if (Orientation == Orientation.Horizontal)
        {
            var metrics = font.Metrics;
            float lineHeight = Math.Abs(metrics.Ascent - metrics.Descent + metrics.Leading);
            return lineHeight + GetStrokeMargin();
        }
        else
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
            return maxWidth + GetStrokeMargin();
        }
    }
}