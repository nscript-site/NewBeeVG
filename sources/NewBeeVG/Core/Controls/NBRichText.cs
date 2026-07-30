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

    public NBTextAlign CrossAxisAlign { get; set; } = NBTextAlign.LeftOrTop;

    /// <summary>行高；NaN 时自动取各 Run 的最大值。</summary>
    public float LineHeight { get; set; } = float.NaN;

    /// <summary>
    /// 如果设置了行高，则该值无效；如果未设置行高，则该值为额外的行间距。
    /// </summary>
    public float LineSpacing { get; set; } = 0f;

    /// <summary>字间距（全局）。</summary>
    public float LetterSpacing { get; set; } = 0f;

    /// <summary>文本运行列表。</summary>
    protected internal List<NBTextRun> Runs { get; } = new List<NBTextRun>();

    public void Add(NBTextRun run)
    {
        Runs.Add(run);
        VisualChildren.Add(run);
    }

    private NBRichBoxLayout? _layout = null;
    internal NBRichBoxLayout Layout
    {
        get
        {
            if(_layout == null)
                _layout = new NBRichBoxLayout(this);
            return _layout;
        }
    }

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

    NBRichBoxLineLayoutInfo? _lines;

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var run in Runs)
        {
            if(run.Orientation != this.Orientation)
                run.Orientation = this.Orientation;
        }

        _lines = Layout.Measure(availableSize, Padding);
        var s = _lines.Bound.Size;
        return new Size(s.Width + Padding.Left + Padding.Right, s.Height + Padding.Top + Padding.Bottom);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        SKPoint origin = new SKPoint((float)(finalRect.X + Padding.Left), (float)(finalRect.Y + Padding.Top));
        foreach(var item in Runs)
        {
            item.UpdateLayout(origin);
        }
    }
}

public static partial class NBExtentions
{
    public static TWidget AddRun<TWidget>(this TWidget rich, NBTextRun run) where TWidget : NBRichText
    {
        rich.Add(run);
        return rich;
    }

    public static TWidget CrossAxisAlign<TWidget>(this TWidget rich, int align) where TWidget : NBRichText
    {
        rich.CrossAxisAlign = align.ToNBTextAlign();
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
        rich.Add(run);
        return rich;
    }
}