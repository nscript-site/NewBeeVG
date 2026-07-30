using SkiaSharp;
using static NewBeeVG.NBTextUtils;

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

    internal List<NBTextRunClipInfo> Clips { get; set; } = new List<NBTextRunClipInfo>();

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
        SKPoint origin = new SKPoint(Bounds.Left, Bounds.Top);
        foreach (var clip in Clips)
        {
            using var paint = CreateFillTextPaint();
            using var typeface = CreateTypeface();
            using var font = CreateFont(typeface);
            var line = clip.Text;
            float x = clip.X + origin.X;
            float y = clip.Y + origin.Y;
            if (Strokes.IsEmpty() == false)
            {
                if (StrokesFirst == true)
                {
                    DrawStrokes(context, clip.Orientation, font, line, x, y);
                    DrawLine(context, clip.Orientation, font, paint, line, x, y);
                }
                else
                {
                    DrawLine(context, clip.Orientation, font, paint, line, x, y);
                    DrawStrokes(context, clip.Orientation, font, line, x, y);
                }
            }
            else
            {
                DrawLine(context, clip.Orientation, font, paint, line, x, y);
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
        return LetterSpacing + GetStrokeMargin();
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
}