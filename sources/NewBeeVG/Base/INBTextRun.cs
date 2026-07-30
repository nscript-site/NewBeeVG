using SkiaSharp;

namespace NewBeeVG;

public interface INBTextRun
{
    public string Text { get; set; }

    public string FontFamily { get; set; }
    public float FontSize { get; set; }
    public SKFontStyleWeight FontWeight { get; set; }
    public SKFontStyleWidth FontWidth { get; set; }
    public SKFontStyleSlant FontSlant { get; set; }

    public SKColor Foreground { get; set; }

    public bool StrokesFirst { get; set; }

    public NBStrokeCollection Strokes { get; }

    /// <summary>
    /// 行高；如果为 NaN，则自动按字体度量计算。
    /// </summary>
    public float LineHeight { get; set; }

    /// <summary>
    /// 字间距。
    /// </summary>
    public float LetterSpacing { get; set; }
}

public static partial class NBExtentions
{
    public static TWidget FontSize<TWidget>(this TWidget widget, float fontSize) where TWidget : INBTextRun
    {
        widget.FontSize = fontSize;
        return widget;
    }

    public static TWidget Font<TWidget>(this TWidget widget, float size, SKColor? color = null) where TWidget : INBTextRun
    {
        widget.FontSize = size;
        if (color.HasValue)
        {
            widget.Foreground = color.Value;
        }
        return widget;
    }

    public static TWidget Text<TWidget>(this TWidget widget, string text) where TWidget : INBTextRun
    {
        widget.Text = text;
        return widget;
    }

    public static TWidget FontFamily<TWidget>(this TWidget widget, string fontFamily) where TWidget : INBTextRun
    {
        widget.FontFamily = fontFamily;
        return widget;
    }

    public static TWidget FontWeight<TWidget>(this TWidget widget, SKFontStyleWeight fontWeight) where TWidget : INBTextRun
    {
        widget.FontWeight = fontWeight;
        return widget;
    }

    public static TWidget FontWidth<TWidget>(this TWidget widget, SKFontStyleWidth fontWidth) where TWidget : INBTextRun
    {
        widget.FontWidth = fontWidth;
        return widget;
    }

    public static TWidget FontSlant<TWidget>(this TWidget widget, SKFontStyleSlant fontSlant) where TWidget : INBTextRun
    {
        widget.FontSlant = fontSlant;
        return widget;
    }

    public static TWidget Foreground<TWidget>(this TWidget widget, SKColor color) where TWidget : INBTextRun
    {
        widget.Foreground = color;
        return widget;
    }

    public static TWidget StrokeFirst<TWidget>(this TWidget widget, bool strokesFirst) where TWidget : INBTextRun
    {
        widget.StrokesFirst = strokesFirst;
        return widget;
    }

    public static TWidget Strokes<TWidget>(this TWidget widget, SKColor color, float width, SKStrokeCap cap = SKStrokeCap.Round, SKStrokeJoin join = SKStrokeJoin.Bevel) where TWidget : INBTextRun
    {
        var stroke = new NBStroke { StrokeCap = cap, StrokeColor = color, StrokeJoin = join, StrokeWidth = width * 2 };
        widget.Strokes.ClearStrokes();
        widget.Strokes.AddStroke(stroke);
        return widget;
    }

    public static TWidget AddStroke<TWidget>(this TWidget widget, SKColor color, float width, SKStrokeCap cap = SKStrokeCap.Round, SKStrokeJoin join = SKStrokeJoin.Bevel) where TWidget : INBTextRun
    {
        var stroke = new NBStroke { StrokeCap = cap, StrokeColor = color, StrokeJoin = join, StrokeWidth = width * 2 };
        widget.Strokes.AddStroke(stroke);
        return widget;
    }

    public static TWidget ClearStrokes<TWidget>(this TWidget widget) where TWidget : INBTextRun
    {
        widget.Strokes.ClearStrokes();
        return widget;
    }

    public static TWidget Fg<TWidget>(this TWidget widget, SKColor color) where TWidget : INBTextRun
    {
        widget.Foreground = color;
        return widget;
    }

    public static TWidget LineHeight<TWidget>(this TWidget widget, float lineHeight) where TWidget : INBTextRun
    {
        widget.LineHeight = lineHeight;
        return widget;
    }

    public static TWidget LetterSpacing<TWidget>(this TWidget widget, float letterSpacing) where TWidget : INBTextRun
    {
        widget.LetterSpacing = letterSpacing;
        return widget;
    }
}