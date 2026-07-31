using SkiaSharp;

namespace NewBeeVG;

internal static class NBTextUtils
{
    internal static void DrawRune(SKCanvas context, SKFont font, SKPaint paint, string rune, float x, float y, bool isStroke)
    {
        if (isStroke == false)
            context.DrawText(rune, x, y, font, paint);
        else
        {
            using var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                StrokeCap = paint.StrokeCap,
                StrokeJoin = paint.StrokeJoin
            };

            using var path = font.GetTextPath(rune, new SKPoint(x, y));
            using var fillPath = strokePaint.GetFillPath(path);
            context.DrawPath(fillPath, paint);
        }
    }

    internal static void DrawHorizontalLine(SKCanvas context, SKFont font, SKPaint paint, string line, float x, float y, bool isStroke, float letterSpacingWithStroke, float? maxLineWidth = null)
    {
        if (string.IsNullOrEmpty(line))
            return;

        float currentX = x;
        var metrics = font.Metrics;
        var w = Math.Ceiling(metrics.Descent - metrics.Ascent + metrics.Leading);
        foreach (var rune in line.EnumerateRunes())
        {
            var runeText = rune.ToString();
            float offset = 0;

            if (maxLineWidth != null)
            {
                offset = (float)(maxLineWidth.Value - w) * 0.5f; // 居中对齐
            }

            DrawRune(context, font, paint, runeText, currentX, y + offset, isStroke);
            currentX += (float)font.MeasureText(runeText) + letterSpacingWithStroke;
        }
    }

    internal static void DrawVerticalLine(SKCanvas context, SKFont font, SKPaint paint, string line, float x, float y, bool isStroke, float letterSpacingWithStroke, float? maxLineWidth = null)
    {
        if (string.IsNullOrEmpty(line))
            return;

        float currentY = y;
        var metrics = font.Metrics;
        var lineHeight = Math.Abs(metrics.Ascent - metrics.Descent + metrics.Leading);

        foreach (var rune in line.EnumerateRunes())
        {
            var runeText = rune.ToString();
            float offset = 0;
            if (maxLineWidth != null)
            {
                var w = font.MeasureText(runeText);
                offset = (float)(maxLineWidth.Value - w) * 0.5f; // 居中对齐
            }
            DrawRune(context, font, paint, runeText, x + offset, currentY, isStroke);
            currentY += lineHeight + letterSpacingWithStroke;
        }
    }

    /// <summary>
    /// 按 Rune 数量删除前缀，返回剩余文本。
    /// </summary>
    internal static string RemovePrefixByRunes(string value, int runeCount)
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

    /// <summary>
    /// 按 Rune 数量截取前缀，不会截断代理项。
    /// </summary>
    internal static string SubstringByRunes(string value, int runeCount)
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
    /// 统一文本换行符，便于后续处理。
    /// </summary>
    internal static string NormalizeText(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    /// <summary>
    /// 计算字符串中 Rune 的数量。
    /// </summary>
    internal static int CountRunes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        int count = 0;
        foreach (var _ in value.EnumerateRunes())
            count++;

        return count;
    }
}
