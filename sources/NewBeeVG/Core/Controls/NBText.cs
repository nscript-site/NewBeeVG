using SkiaSharp;

namespace NewBeeVG;

public class NBText : NBLayoutable
{
    public Thickness Padding { get; set; }

    public string Text { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Arial";

    public float FontSize { get; set; } = 14f;

    public SKFontStyleWeight FontWeight { get; set; } = SKFontStyleWeight.Normal;

    public SKFontStyleWidth FontWidth { get; set; } = SKFontStyleWidth.Normal;

    public SKFontStyleSlant FontSlant { get; set; } = SKFontStyleSlant.Upright;

    public bool IsWrapText { get; set; } = false;

    public bool IsTrimming { get; set; } = false;

    public SKColor Foreground { get; set; } = SKColors.Black;

    public float LineHeight { get; set; } = float.NaN;

    public float LetterSpacing { get; set; } = 0f;

    public int? MaxLines { get; set; }

    public SKTextAlign TextAlign { get; set; } = SKTextAlign.Left;

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = Text ?? string.Empty;
        var padding = Padding;

        var innerAvailableWidth = double.IsPositiveInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Width - padding.Left - padding.Right);

        var fontStyle = new SKFontStyle((int)FontWeight, (int)FontWidth, FontSlant);
        using var typeface = SKTypeface.FromFamilyName(FontFamily, fontStyle);
        using var font = new SKFont(typeface, FontSize);

        var metrics = font.Metrics;
        var lineHeight = double.IsNaN(LineHeight)
            ? Math.Ceiling(metrics.Descent - metrics.Ascent + metrics.Leading)
            : LineHeight;

        if (lineHeight < 0)
            lineHeight = 0;

        var lines = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            lines.Add(string.Empty);
        }
        else
        {
            var paragraphs = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (var paragraph in paragraphs)
            {
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

                var rest = paragraph;
                while (rest.Length > 0)
                {
                    int count = FitPrefix(rest, (float)innerAvailableWidth, font);
                    if (count <= 0)
                        count = 1;

                    lines.Add(rest.Substring(0, count));
                    rest = rest.Substring(count);
                }
            }
        }

        if (MaxLines.HasValue && MaxLines.Value > 0 && lines.Count > MaxLines.Value)
        {
            lines = lines.GetRange(0, MaxLines.Value);

            if (IsTrimming && !double.IsPositiveInfinity(innerAvailableWidth) && innerAvailableWidth > 0)
            {
                lines[^1] = TrimToWidth(lines[^1], (float)innerAvailableWidth, font);
            }
        }

        double contentWidth = 0;
        foreach (var line in lines)
        {
            contentWidth = Math.Max(contentWidth, MeasureLineWidth(line, font));
        }

        double contentHeight = lines.Count * lineHeight;

        return new Size(
            contentWidth + padding.Left + padding.Right,
            contentHeight + padding.Top + padding.Bottom);

        double MeasureLineWidth(string value, SKFont f)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            double width = f.MeasureText(value);

            if (LetterSpacing != 0 && value.Length > 1)
                width += LetterSpacing * (value.Length - 1);

            return width;
        }

        int FitPrefix(string value, float maxWidth, SKFont f)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            if (MeasureLineWidth(value, f) <= maxWidth)
                return value.Length;

            int lo = 1;
            int hi = value.Length;
            int best = 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                float width = (float)MeasureLineWidth(value.Substring(0, mid), f);

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

        string TrimToWidth(string value, float maxWidth, SKFont f)
        {
            const string ellipsis = "…";

            if (MeasureLineWidth(ellipsis, f) > maxWidth)
                return string.Empty;

            if (MeasureLineWidth(value, f) <= maxWidth)
                return value;

            float ellipsisWidth = (float)MeasureLineWidth(ellipsis, f);
            float prefixWidth = maxWidth - ellipsisWidth;

            if (prefixWidth <= 0)
                return ellipsis;

            int count = FitPrefix(value, prefixWidth, f);
            return value.Substring(0, count) + ellipsis;
        }
    }

    protected override void BeforeRenderChildren(SKCanvas context)
    {
        base.BeforeRenderChildren(context);
    }

    protected override void AfterRenderChildren(SKCanvas context)
    {
        RenderText(context);
    }

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

        var fontStyle = new SKFontStyle((int)FontWeight, (int)FontWidth, FontSlant);
        using var typeface = SKTypeface.FromFamilyName(FontFamily, fontStyle);
        using var font = new SKFont(typeface, FontSize);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = Foreground
        };

        var metrics = font.Metrics;
        var lineHeight = double.IsNaN(LineHeight)
            ? Math.Ceiling(metrics.Descent - metrics.Ascent + metrics.Leading)
            : LineHeight;

        if (lineHeight < 0)
            lineHeight = 0;

        var lines = BuildLines(Text, innerWidth, font);

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
            var line = lines[i];
            var lineWidth = MeasureLineWidth(line, font);
            var x = GetLineX(innerLeft, innerWidth, lineWidth);
            var y = innerTop + (float)(i * lineHeight) - metrics.Ascent;

            context.DrawText(line, x, y, font, paint);

            if (innerTop + (i + 1) * lineHeight > bounds.Bottom)
                break;
        }

        context.Restore();

        List<string> BuildLines(string textValue, double maxWidth, SKFont f)
        {
            var result = new List<string>();

            var normalized = textValue.Replace("\r\n", "\n").Replace('\r', '\n');
            var paragraphs = normalized.Split('\n');

            foreach (var paragraph in paragraphs)
            {
                if (!IsWrapText || double.IsPositiveInfinity(maxWidth))
                {
                    result.Add(paragraph);
                    continue;
                }

                if (maxWidth <= 0)
                {
                    result.Add(string.Empty);
                    continue;
                }

                if (paragraph.Length == 0)
                {
                    result.Add(string.Empty);
                    continue;
                }

                var rest = paragraph;
                while (rest.Length > 0)
                {
                    int count = FitPrefix(rest, (float)maxWidth, f);
                    if (count <= 0)
                        count = 1;

                    result.Add(rest.Substring(0, count));
                    rest = rest.Substring(count);
                }
            }

            return result;
        }

        double MeasureLineWidth(string value, SKFont f)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            double width = f.MeasureText(value);

            if (LetterSpacing != 0 && value.Length > 1)
                width += LetterSpacing * (value.Length - 1);

            return width;
        }

        int FitPrefix(string value, float maxWidth, SKFont f)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            if (MeasureLineWidth(value, f) <= maxWidth)
                return value.Length;

            int lo = 1;
            int hi = value.Length;
            int best = 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                float width = (float)MeasureLineWidth(value.Substring(0, mid), f);

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

        string TrimToWidth(string value, float maxWidth, SKFont f)
        {
            const string ellipsis = "…";

            if (MeasureLineWidth(ellipsis, f) > maxWidth)
                return string.Empty;

            if (MeasureLineWidth(value, f) <= maxWidth)
                return value;

            float ellipsisWidth = (float)MeasureLineWidth(ellipsis, f);
            float prefixWidth = maxWidth - ellipsisWidth;

            if (prefixWidth <= 0)
                return ellipsis;

            int count = FitPrefix(value, prefixWidth, f);
            return value.Substring(0, count) + ellipsis;
        }

        float GetLineX(float left, float width, double lineWidth)
        {
            return TextAlign switch
            {
                SKTextAlign.Center => left + (float)((width - lineWidth) / 2.0),
                SKTextAlign.Right => left + (float)(width - lineWidth),
                _ => left
            };
        }
    }
}
