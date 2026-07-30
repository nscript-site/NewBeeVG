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

    internal static void DrawHorizontalLine(SKCanvas context, SKFont font, SKPaint paint, string line, float x, float y, bool isStroke, float letterSpacingWithStroke)
    {
        if (string.IsNullOrEmpty(line))
            return;

        float currentX = x;

        foreach (var rune in line.EnumerateRunes())
        {
            var runeText = rune.ToString();
            DrawRune(context, font, paint, runeText, currentX, y, isStroke);
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
}
