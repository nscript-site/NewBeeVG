using SkiaSharp;

namespace NewBeeVG;

internal class NBRichTextLineInfo
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Height { get; set; } = 0;
    public float Length { get; set; } = 0;
    public List<NBTextRunClipInfo> Clips { get; set; } = new List<NBTextRunClipInfo>();
    public float FilledLength { get; set; } = 0;
    public bool RTL { get; set; }
    public Orientation Orientation { get; set; }

    public NBTextRunReceiveResult TryReceive(NBTextRun clip, string content, float availableLength)
    {
        if(Orientation == Orientation.Horizontal)
        {
            return TryReceiveHorizontal(clip, content, availableLength);
        }
        else
        {
            return TryReceiveVertical(clip, content, availableLength);
        }
    }

    private float GetLetterSpacing()
    {
        return Clips.Count == 0 ? 0 : Clips[Clips.Count - 1].Run.LetterSpacing;
    }

    private NBTextRunReceiveResult TryReceiveHorizontal(NBTextRun clip, string content, float availableLength)
    {
        var r = new NBTextRunReceiveResult();
        if(availableLength <= FilledLength && IsEmpty() == false)
        {
            r.Input = content;
            r.Output = content;
            r.Received = false;
            return r;
        }

        var runes = content.EnumerateRunes();
        var input = "";
        var output = "";
        foreach(var run in runes)
        {
            input += run.ToString();
            output = content.Substring(input.Length);
        }

        return r;
    }

    private NBTextRunReceiveResult TryReceiveVertical(NBTextRun clip, string content, float availableLength)
    {
        throw new NotImplementedException();
    }


    public SKRect GetBound()
    {
        if (Orientation == Orientation.Horizontal)
            return new SKRect(X, Y, X + Length, Y + Height);
        else
            return new SKRect(X, Y, X + Height, Y + Length);
    }

    public bool IsEmpty() { return Length == 0; }
}

internal class NBTextRunReceiveResult
{
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public bool Received { get; set; } = false;
}

internal class NBRichBoxLineLayoutInfo
{
    public SKRect Bound { get; internal set; }
    public List<NBRichTextLineInfo> Lines { get; init; }

    public NBRichBoxLineLayoutInfo(List<NBRichTextLineInfo> lines)
    {
        Lines = lines;
        UpdateBounds();
    }

    private void UpdateBounds()
    {
        SKRect? maxRect = null;
        foreach (var clip in Lines)
        {
            var b = clip.GetBound();
            var x = b.Left;
            var y = b.Top;
            b = new SKRect(x, y, x + b.Width, y + b.Height);
            if (maxRect == null) maxRect = b;
            else maxRect = SKRect.Union(maxRect.Value, b);
        }
        if(maxRect != null) this.Bound = maxRect.Value;
    }
}

internal class NBRichBoxLayout
{
    NBRichText Owner;
    public NBRichBoxLayout(NBRichText owner)
    {
        Owner = owner;
    }

    internal NBRichBoxLineLayoutInfo Measure(Size availableSize, Thickness padding)
    {
        if (Owner.Orientation == Orientation.Horizontal)
        {
            return MeasureHorizontal(availableSize, padding);
        }
        else
        {
            return MeasureVertical(availableSize, padding);
        }
    }

    internal NBRichBoxLineLayoutInfo MeasureHorizontal(Size availableSize, Thickness padding)
    {
        var innerAvailableLength = NBLayoutable.GetInnerAvailableWidth(availableSize.Width, padding);
        return BuildLines(Owner.Runs, (float)innerAvailableLength);
    }

    internal NBRichBoxLineLayoutInfo MeasureVertical(Size availableSize, Thickness padding)
    {
        var innerAvailableLength = NBLayoutable.GetInnerAvailableHeight(availableSize.Height, padding);
        return BuildLines(Owner.Runs, (float)innerAvailableLength);
    }

    private NBRichTextLineInfo CreateLineInfo()
    {
        return new NBRichTextLineInfo() {  RTL = this.Owner.RightToLeft, Orientation = this.Owner.Orientation };
    }

    private NBRichBoxLineLayoutInfo BuildLines(List<NBTextRun> runs, float availableLength)
    {
        float linespacing = Owner.LineSpacing;
        float lineheight = Owner.LineHeight;

        var lines = new List<NBRichTextLineInfo>();
        NBRichTextLineInfo current = CreateLineInfo();
        foreach (var run in runs)
        {
            var content = run.Text;
            while(!String.IsNullOrEmpty(content))
            {
                var result = current.TryReceive(run, content, availableLength);
                if (result.Received)
                {
                    content = result.Output;
                }
                else
                {
                    lines.Add(current);
                    NBRichTextLineInfo last = current;
                    current = CreateLineInfo();

                    float delta = lineheight;
                    if(delta == float.NaN)
                        delta = last.Height + linespacing;

                    if (Owner.Orientation == Orientation.Horizontal)
                    {
                        current.Y = last.Y + delta;
                    }
                    else
                    {
                        if(Owner.RightToLeft == false)
                        {
                            current.X = last.X + delta;
                        }
                        else
                        {
                            current.X = last.X - delta;
                        }
                    }
                }
            }
        }

        if(current.IsEmpty() == false)
            lines.Add(current);

        NBRichBoxLineLayoutInfo layout = new NBRichBoxLineLayoutInfo(lines);
        return layout;
    }
}
