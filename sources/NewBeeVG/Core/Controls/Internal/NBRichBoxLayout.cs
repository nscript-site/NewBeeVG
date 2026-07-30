using SkiaSharp;

namespace NewBeeVG;

internal class NBRichTextLineInfo
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Height { get; set; } = 0;
    public float Length { get; set; } = 0;
    public List<NBTextRunClipInfo> Clips { get; set; } = new List<NBTextRunClipInfo>();
    public bool RTL { get; set; }
    public Orientation Orientation { get; set; }

    internal float GetLetterSpacing()
    {
        return Clips.Count == 0 ? 0 : Clips[Clips.Count - 1].Run.LetterSpacing;
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

        void FlushCurrent()
        {
            lines.Add(current);

            NBRichTextLineInfo last = current;
            current = CreateLineInfo();

            float delta = lineheight;
            if (float.IsNaN(delta))
                delta = last.Height + linespacing;

            if (Owner.Orientation == Orientation.Horizontal)
            {
                current.Y = last.Y + delta;
            }
            else
            {
                if (Owner.RightToLeft == false)
                {
                    current.X = last.X + delta;
                }
                else
                {
                    current.X = last.X - delta;
                }
            }
        }

        foreach (var run in runs)
        {
            var availableLengthFirstLine = availableLength - current.Length;
            var currentLetterSpacing = Math.Max(current.GetLetterSpacing(), run.LetterSpacing);
            availableLengthFirstLine += currentLetterSpacing;
            availableLengthFirstLine = Math.Max(0, availableLengthFirstLine);
            run.Clips.Clear();
            var list = run.BuildLines(availableLengthFirstLine, availableLength);
            foreach (var line in list)
            {
                bool isNewLine = line.Item4;
                var text = line.Item1;
                var length = line.Item2;
                var maxHeight = line.Item3;

                if (length <= 0) continue;
                if(isNewLine == true)
                {
                    if(current.IsEmpty() == false)
                        FlushCurrent();
                }

                double letterSpacing = 0;
                if(current.IsEmpty() == false)
                    letterSpacing = Math.Max(current.GetLetterSpacing(), run.LetterSpacing);

                current.Length += (float)letterSpacing + length;
                current.Height = Math.Max(current.Height, maxHeight);

                var clip = new NBTextRunClipInfo() { Run = run, Text = text };                
                if(Owner.Orientation  == Orientation.Horizontal)
                {
                    clip.X = current.X + current.Length - length;
                    clip.Y = current.Y;
                }
                else
                {
                    clip.X = current.Length;
                    clip.Y = current.Y + current.Length - length;
                }
                clip.Height = maxHeight;
                clip.Length = length;

                run.Clips.Add(clip);
                current.Clips.Add(clip);
            }
        }

        if(current.IsEmpty() == false)
            lines.Add(current);

        NBRichBoxLineLayoutInfo layout = new NBRichBoxLineLayoutInfo(lines);
        return layout;
    }
}
