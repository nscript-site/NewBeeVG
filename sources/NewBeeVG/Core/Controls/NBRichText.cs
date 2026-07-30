using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// 富文本控件，包含多个 NBTextRun，支持独立样式。
/// </summary>
public class NBRichText : NBLayoutable, IPaddingable, IOrientation
{
    public Orientation Orientation { get; set; } = Orientation.Horizontal;
    public bool RightToLeft { get; set; } = false;
    public Thickness Padding { get; set; }

    public bool IsWrapText { get; set; } = false;
    public bool IsTrimming { get; set; } = false;
    public int? MaxLines { get; set; }
    public NBTextAlign TextAlign { get; set; } = NBTextAlign.LeftOrTop;

    /// <summary>行高；NaN 时自动取各 Run 的最大值。</summary>
    public float LineHeight { get; set; } = float.NaN;

    /// <summary>字间距（全局）。</summary>
    public float LetterSpacing { get; set; } = 0f;

    /// <summary>文本运行列表。</summary>
    public List<NBTextRun> Runs { get; } = new List<NBTextRun>();

    // -------- 内部数据 --------
    private struct CharInfo
    {
        public string RuneText;       // 单个 Unicode 字符
        public NBTextRun Run;         // 所属 Run
        public SKFont Font;           // 对应字体（需手动释放）
        public float CharWidth;       // 字符宽度（横向）
        public float CharHeight;      // 字符高度（纵向基于度量）
        public float LineHeight;      // 该字符所在行高（取 Run 的行高 + 描边）
        public float StrokeMargin;    // 描边边距
    }

    // -------- 测量 --------
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        if (Orientation == Orientation.Horizontal)
            return MeasureHorizontal(availableSize, padding);
        else
            return MeasureVertical(availableSize, padding);
    }

    private Size MeasureHorizontal(Size availableSize, Thickness padding)
    {
        float innerWidth = GetInnerAvailableWidth(availableSize.Width, padding);
        if (Runs.Count == 0) return new Size(padding.Left + padding.Right, padding.Top + padding.Bottom);

        using (var chars = BuildCharSequence())
        {
            var lines = BuildLines(chars, innerWidth);
            float maxWidth = 0;
            float totalHeight = 0;
            foreach (var line in lines)
            {
                float lineWidth = MeasureLineWidth(line);
                if (lineWidth > maxWidth) maxWidth = lineWidth;
                float lineHeight = line.Max(c => c.LineHeight);
                totalHeight += lineHeight;
            }
            return new Size(
                maxWidth + padding.Left + padding.Right,
                totalHeight + padding.Top + padding.Bottom);
        }
    }

    private Size MeasureVertical(Size availableSize, Thickness padding)
    {
        float innerHeight = GetInnerAvailableHeight(availableSize.Height, padding);
        if (Runs.Count == 0) return new Size(padding.Left + padding.Right, padding.Top + padding.Bottom);

        using (var chars = BuildCharSequence())
        {
            var lines = BuildLines(chars, innerHeight); // 纵向“行”即水平行（实际为列）
            float totalWidth = 0;
            float maxHeight = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                float colWidth = line.Max(c => c.CharWidth) + GetStrokeMargin(line);
                float colHeight = MeasureLineLength(line); // 列高度
                if (colHeight > maxHeight) maxHeight = colHeight;
                totalWidth += colWidth;
                if (i < lines.Count - 1) totalWidth += LetterSpacing; // 列间距
            }
            return new Size(
                totalWidth + padding.Left + padding.Right,
                maxHeight + padding.Top + padding.Bottom);
        }
    }

    // -------- 绘制 --------
    protected override void RenderContent(SKCanvas context)
    {
        if (Orientation == Orientation.Horizontal)
            RenderHorizontal(context);
        else
            RenderVertical(context);
    }

    private void RenderHorizontal(SKCanvas context)
    {
        if (Runs.Count == 0) return;
        var bounds = Bounds;
        var padding = Padding;
        float eps = 0.001f;
        float innerLeft = bounds.Left + (float)padding.Left;
        float innerTop = bounds.Top + (float)padding.Top;
        float innerWidth = (float)Math.Max(0, eps + bounds.Width - padding.Left - padding.Right);
        float innerHeight = (float)Math.Max(0, eps + bounds.Height - padding.Top - padding.Bottom);
        if (innerWidth <= 0 || innerHeight <= 0) return;

        using (var chars = BuildCharSequence())
        {
            var lines = BuildLines(chars, innerWidth);

            // 最大行数限制
            if (MaxLines.HasValue && MaxLines.Value > 0 && lines.Count > MaxLines.Value)
            {
                lines = lines.Take(MaxLines.Value).ToList();
                if (IsTrimming && innerWidth > 0 && lines.Any())
                {
                    var last = lines.Last();
                    lines[lines.Count - 1] = TrimLineToWidth(last, innerWidth);
                }
            }

            context.Save();
            context.ClipRect(bounds);

            float y = innerTop;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                float lineHeight = line.Any() ? line.Max(c => c.LineHeight) : 0;
                if (y + lineHeight > bounds.Bottom) break;

                float lineWidth = MeasureLineWidth(line);
                float x = GetLineX(innerLeft, innerWidth, lineWidth);

                foreach (var ch in line)
                {
                    float charY = y + lineHeight * 0.5f + ch.Font.Metrics.CapHeight * 0.5f; // 近似垂直居中
                    DrawChar(context, ch, x, charY);
                    x += ch.CharWidth + LetterSpacing;
                }
                y += lineHeight;
            }

            context.Restore();
        }
    }

    private void RenderVertical(SKCanvas context)
    {
        if (Runs.Count == 0) return;
        var bounds = Bounds;
        var padding = Padding;
        float eps = 0.001f;
        float innerLeft = bounds.Left + (float)padding.Left;
        float innerTop = bounds.Top + (float)padding.Top;
        float innerWidth = (float)Math.Max(0, eps + bounds.Width - padding.Left - padding.Right);
        float innerHeight = (float)Math.Max(0, eps + bounds.Height - padding.Top - padding.Bottom);
        if (innerWidth <= 0 || innerHeight <= 0) return;

        using (var chars = BuildCharSequence())
        {
            var columns = BuildLines(chars, innerHeight);
            if (MaxLines.HasValue && MaxLines.Value > 0 && columns.Count > MaxLines.Value)
            {
                columns = columns.Take(MaxLines.Value).ToList();
                if (IsTrimming && innerHeight > 0 && columns.Any())
                {
                    var last = columns.Last();
                    columns[columns.Count - 1] = TrimLineToHeight(last, innerHeight);
                }
            }

            context.Save();
            context.ClipRect(bounds);

            bool rtl = RightToLeft;
            float currentX = rtl ? innerLeft + innerWidth : innerLeft;

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                float colWidth = col.Any() ? col.Max(c => c.CharWidth) + GetStrokeMargin(col) : 0;
                float colHeight = MeasureLineLength(col);
                float x = rtl ? currentX - colWidth : currentX;

                // 垂直对齐
                float y = GetLineY(innerTop, innerHeight, colHeight);

                foreach (var ch in col)
                {
                    float charX = x + (colWidth - ch.CharWidth) * 0.5f; // 居中
                    DrawChar(context, ch, charX, y);
                    y += ch.CharHeight + LetterSpacing;
                }

                if (rtl) currentX -= (colWidth + LetterSpacing);
                else currentX += (colWidth + LetterSpacing);
            }

            context.Restore();
        }
    }

    private void DrawChar(SKCanvas context, CharInfo ch, float x, float y)
    {
        var run = ch.Run;
        var font = ch.Font;
        string rune = ch.RuneText;

        var strokes = run.Strokes;
        if (strokes != null && !strokes.IsEmpty())
        {
            if (run.StrokesFirst)
            {
                foreach (var s in strokes)
                    DrawStrokeChar(context, font, s, rune, x, y);
                using (var fill = run.CreateFillPaint())
                    context.DrawText(rune, x, y, font, fill);
            }
            else
            {
                using (var fill = run.CreateFillPaint())
                    context.DrawText(rune, x, y, font, fill);
                foreach (var s in strokes)
                    DrawStrokeChar(context, font, s, rune, x, y);
            }
        }
        else
        {
            using (var fill = run.CreateFillPaint())
                context.DrawText(rune, x, y, font, fill);
        }
    }

    private void DrawStrokeChar(SKCanvas context, SKFont font, NBStroke stroke, string rune, float x, float y)
    {
        using (var paint = stroke.CreatePaint())
        {
            if (paint.Style == SKPaintStyle.Stroke)
            {
                // 使用路径避免描边空洞
                using (var path = font.GetTextPath(rune, new SKPoint(x, y)))
                using (var fillPath = paint.GetFillPath(path))
                    context.DrawPath(fillPath, paint);
            }
            else
            {
                context.DrawText(rune, x, y, font, paint);
            }
        }
    }

    // -------- 字符序列构建 --------
    private DisposableCharSequence BuildCharSequence()
    {
        var list = new List<CharInfo>();
        foreach (var run in Runs)
        {
            if (string.IsNullOrEmpty(run.Text)) continue;

            using (var tf = run.CreateTypeface())
            using (var font = run.CreateFont(tf))
            {
                float baseLineHeight = run.GetBaseLineHeight(font);
                float strokeMargin = run.GetStrokeMargin();
                float lineHeight = float.IsNaN(LineHeight) ? baseLineHeight + strokeMargin : LineHeight + strokeMargin;

                foreach (var rune in run.Text.EnumerateRunes())
                {
                    string s = rune.ToString();
                    float w = font.MeasureText(s);
                    float h = Math.Abs(font.Metrics.Descent - font.Metrics.Ascent) + font.Metrics.Leading;

                    list.Add(new CharInfo
                    {
                        RuneText = s,
                        Run = run,
                        Font = run.CreateFont(tf), // 注意：需要手动释放，因此使用 DisposableCharSequence
                        CharWidth = w,
                        CharHeight = h,
                        LineHeight = lineHeight,
                        StrokeMargin = strokeMargin
                    });
                }
            }
        }
        return new DisposableCharSequence(list);
    }

    // 辅助：释放序列中的 Font
    private class DisposableCharSequence : List<CharInfo>, IDisposable
    {
        public DisposableCharSequence(List<CharInfo> items) : base(items) { }
        public void Dispose()
        {
            foreach (var item in this)
                item.Font?.Dispose();
        }
    }

    // -------- 换行 / 分列 --------
    private List<List<CharInfo>> BuildLines(List<CharInfo> chars, float maxLength)
    {
        var lines = new List<List<CharInfo>>();
        if (chars.Count == 0) return lines;

        var currentLine = new List<CharInfo>();
        float currentLength = 0;

        foreach (var ch in chars)
        {
            float charAdvance = ch.CharWidth + LetterSpacing;
            // 处理换行符（将 Run 内文本的 \n 视为强制换行）
            if (ch.RuneText == "\n")
            {
                lines.Add(currentLine);
                currentLine = new List<CharInfo>();
                currentLength = 0;
                continue;
            }

            if (!IsWrapText || float.IsPositiveInfinity(maxLength) || maxLength <= 0)
            {
                currentLine.Add(ch);
            }
            else
            {
                if (currentLength + ch.CharWidth > maxLength && currentLine.Count > 0)
                {
                    lines.Add(currentLine);
                    currentLine = new List<CharInfo>();
                    currentLength = 0;
                }
                currentLine.Add(ch);
                currentLength += charAdvance;
            }
        }
        if (currentLine.Count > 0) lines.Add(currentLine);
        return lines;
    }

    // 横向测量行宽
    private float MeasureLineWidth(List<CharInfo> line)
    {
        if (line.Count == 0) return 0;
        float width = 0;
        foreach (var c in line)
            width += c.CharWidth + LetterSpacing;
        return width - LetterSpacing; // 移除末尾间距
    }

    // 纵向测量列高
    private float MeasureLineLength(List<CharInfo> line)
    {
        if (line.Count == 0) return 0;
        float length = 0;
        foreach (var c in line)
            length += c.CharHeight + LetterSpacing;
        return length - LetterSpacing;
    }

    // 横向截断
    private List<CharInfo> TrimLineToWidth(List<CharInfo> line, float maxWidth)
    {
        const string ellipsis = "…";
        if (line.Count == 0) return new List<CharInfo>();

        var lastRun = line.Last().Run;
        using (var tf = lastRun.CreateTypeface())
        using (var font = lastRun.CreateFont(tf))
        {
            float ellipsisWidth = font.MeasureText(ellipsis);
            float available = maxWidth - ellipsisWidth;
            if (available <= 0) return new List<CharInfo>();

            var trimmed = new List<CharInfo>();
            float current = 0;
            foreach (var ch in line)
            {
                if (current + ch.CharWidth > available) break;
                trimmed.Add(ch);
                current += ch.CharWidth + LetterSpacing;
            }

            // 添加省略号（使用最后一个有效字符的样式）
            var lastChar = trimmed.Count > 0 ? trimmed.Last() : line.First();
            trimmed.Add(new CharInfo
            {
                RuneText = ellipsis,
                Run = lastChar.Run,
                Font = lastChar.Font, // 注意：这里共享引用，Dispose 时不要重复释放
                CharWidth = ellipsisWidth,
                CharHeight = lastChar.CharHeight,
                LineHeight = lastChar.LineHeight,
                StrokeMargin = lastChar.StrokeMargin
            });
            return trimmed;
        }
    }

    // 纵向截断（高度）
    private List<CharInfo> TrimLineToHeight(List<CharInfo> line, float maxHeight)
    {
        const string ellipsis = "…";
        if (line.Count == 0) return new List<CharInfo>();

        var lastRun = line.Last().Run;
        using (var tf = lastRun.CreateTypeface())
        using (var font = lastRun.CreateFont(tf))
        {
            float ellipsisHeight = Math.Abs(font.Metrics.Ascent - font.Metrics.Descent) + font.Metrics.Leading;
            float available = maxHeight - ellipsisHeight;
            if (available <= 0) return new List<CharInfo>();

            var trimmed = new List<CharInfo>();
            float current = 0;
            foreach (var ch in line)
            {
                if (current + ch.CharHeight > available) break;
                trimmed.Add(ch);
                current += ch.CharHeight + LetterSpacing;
            }

            var lastChar = trimmed.Count > 0 ? trimmed.Last() : line.First();
            trimmed.Add(new CharInfo
            {
                RuneText = ellipsis,
                Run = lastChar.Run,
                Font = lastChar.Font,
                CharWidth = lastChar.CharWidth,
                CharHeight = ellipsisHeight,
                LineHeight = lastChar.LineHeight,
                StrokeMargin = lastChar.StrokeMargin
            });
            return trimmed;
        }
    }

    // -------- 对齐计算 --------
    private float GetLineX(float left, float totalWidth, double lineWidth)
    {
        return TextAlign switch
        {
            NBTextAlign.Center => left + (float)((totalWidth - lineWidth) / 2.0),
            NBTextAlign.RightOrBottom => left + (float)(totalWidth - lineWidth),
            _ => left
        };
    }

    private float GetLineY(float top, float totalHeight, double lineHeight)
    {
        return TextAlign switch
        {
            NBTextAlign.Center => top + (float)((totalHeight - lineHeight) / 2.0),
            NBTextAlign.RightOrBottom => top + (float)(totalHeight - lineHeight),
            _ => top
        };
    }

    private float GetStrokeMargin(List<CharInfo> line)
    {
        return line.Any() ? line.Max(c => c.StrokeMargin) : 0;
    }

    // -------- 辅助方法 --------
    private static float GetInnerAvailableWidth(double available, Thickness pad)
    {
        if (double.IsPositiveInfinity(available)) return float.PositiveInfinity;
        return (float)Math.Max(0, available - pad.Left - pad.Right);
    }

    private static float GetInnerAvailableHeight(double available, Thickness pad)
    {
        if (double.IsPositiveInfinity(available)) return float.PositiveInfinity;
        return (float)Math.Max(0, available - pad.Top - pad.Bottom);
    }
}

public static partial class NBExtentions
{
    public static TWidget AddRun<TWidget>(this TWidget rich, NBTextRun run) where TWidget : NBRichText
    {
        rich.Runs.Add(run);
        return rich;
    }

    public static TWidget AddRun<TWidget>(this TWidget rich, string text,
        string fontFamily = "Arial", float fontSize = 40,
        SKColor? foreground = null,
        SKFontStyleWeight weight = SKFontStyleWeight.Normal,
        SKFontStyleWidth width = SKFontStyleWidth.Normal,
        SKFontStyleSlant slant = SKFontStyleSlant.Upright,
        NBStrokeCollection strokes = null) where TWidget : NBRichText
    {
        var run = new NBTextRun
        {
            Text = text,
            FontFamily = fontFamily,
            FontSize = fontSize,
            FontWeight = weight,
            FontWidth = width,
            FontSlant = slant,
            Foreground = foreground ?? SKColors.Black,
            Strokes = strokes ?? new NBStrokeCollection()
        };
        rich.Runs.Add(run);
        return rich;
    }
}