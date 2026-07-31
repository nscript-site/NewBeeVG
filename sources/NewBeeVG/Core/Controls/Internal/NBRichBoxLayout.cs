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
        return Clips.Count == 0 ? 0 : (float)Clips[Clips.Count - 1].Run.LetterSpacing;
    }

    public SKRect GetBound()
    {
        if (Orientation == Orientation.Horizontal)
            return new SKRect(X, Y, X + Length, Y + Height);
        else
            return new SKRect(X, Y, X + Height, Y + Length);
    }

    public bool IsEmpty() { return Length == 0; }

    public void UpdateCrossAxis(NBTextAlign align)
    {
        foreach (var c in Clips) 
            c.UpdateCrossAxis(align, Height);
    }

    public void ChangeCrossAxis(float delta)
    {
        if (Orientation == Orientation.Horizontal)
        {
            Y += delta;
            foreach (var c in Clips)
                c.Y += delta;
        }
        else
        {
            X += delta;
            foreach (var c in Clips)
                c.X += delta;
        }
    }

    public void ChangeMainAxis(float delta)
    {
        if (Orientation == Orientation.Horizontal)
        {
            X += delta;
            foreach(var c in Clips)
                c.X += delta;
        }
        else
        {
            Y += delta;
            foreach (var c in Clips)
                c.Y += delta;
        }
    }
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
    public NBRichBoxLineLayoutInfo(List<NBRichTextLineInfo> lines, NBRichText owner)
    {
        Lines = lines;
        UpdateBounds();
        UpdateMainAxis(owner.TextAlign);
        UpdateCrossAxis(owner.CrossAxisAlign);
        UpdateCrossAxisWhenRTL(owner);
    }

    internal void UpdateCrossAxisWhenRTL(NBRichText owner)
    {
        if(owner.RightToLeft == false || owner.Orientation == Orientation.Horizontal) return;
        float min = 0;
        foreach (var line in Lines)
        {
            min = Math.Min(min, line.X);
        }
        if (min < 0)
        {
            foreach (var line in Lines)
            {
                line.ChangeCrossAxis(-min);
            }
        }
    }

    internal void UpdateCrossAxis(NBTextAlign align)
    {
        foreach (var line in Lines)
        {
            line.UpdateCrossAxis(align);
        }
    }

    internal void UpdateMainAxis(NBTextAlign align)
    {
        float max = 0;
        foreach (var line in Lines)
        {
            max = Math.Max(max, line.Length);
        }
        foreach (var line in Lines)
        {
            float delta = (max - line.Length)*0.5f;
            if (delta <= 0) continue;
            line.ChangeMainAxis(delta);
        }
    }

    internal void UpdateBounds()
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

    internal void PrintLines()
    {
        Console.WriteLine("==== NBRichBoxLineLayoutInfo ====");
        foreach (var line in Lines)
        {
            Console.WriteLine($"Line: X={line.X}, Y={line.Y}, Length={line.Length}, Height={line.Height}, Clips={line.Clips.Count}");
            foreach (var clip in line.Clips)
            {
                Console.WriteLine($"  Clip: Text='{clip.Text}', X={clip.X}, Y={clip.Y}, Length={clip.Length}, Height={clip.Height}");
            }
        }
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
        float linespacing = (float)Owner.LineSpacing;
        float lineheight = (float)Owner.LineHeight;

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
                    // For RTL vertical, we invoke AdjustLinesWhenRTLVertical
                }
            }
        }

        foreach (var run in runs)
        {
            var availableLengthFirstLine = availableLength - current.Length;
            if(current.IsEmpty() == false)
                availableLengthFirstLine -= Math.Max(current.GetLetterSpacing(), (float)run.LetterSpacing);

            availableLengthFirstLine = Math.Max(0, availableLengthFirstLine);
            availableLengthFirstLine = Math.Min(availableLengthFirstLine, availableLength);
            run.Clips.Clear();
            var list = run.BuildLines(availableLengthFirstLine, availableLength);
            foreach (var line in list)
            {
                bool isNewLine = line.Item4;
                var text = line.Item1;
                var length = line.Item2;
                var maxHeight = line.Item3;
                var ascent = line.Item5;

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
                clip.Orientation = Owner.Orientation;
                if (Owner.Orientation  == Orientation.Horizontal)
                {
                    clip.X = current.X + current.Length - length;
                    clip.Y = current.Y;
                    clip.DeltaX = run.GetStrokeMargin() * 0.5f;
                    clip.DeltaY = run.GetStrokeMargin() * 0.5f - ascent;
                }
                else
                {
                    clip.X = current.X;
                    clip.Y = current.Y + current.Length - length;
                    //clip.DeltaX = run.GetStrokeMargin() * 0.5f;
                    clip.DeltaY = run.GetStrokeMargin() * 0.5f - ascent;
                }
                clip.Height = maxHeight;
                clip.Length = length;

                run.Clips.Add(clip);
                current.Clips.Add(clip);
            }
        }

        if(current.IsEmpty() == false)
            lines.Add(current);

        AdjustLinesWhenRTLVertical(lines, Owner);

        NBRichBoxLineLayoutInfo layout = new NBRichBoxLineLayoutInfo(lines, Owner);

#if DEBUG
        layout.PrintLines();
#endif

        return layout;
    }

    private void AdjustLinesWhenRTLVertical(List<NBRichTextLineInfo> lines, NBRichText owner)
    {
        if (Owner.Orientation == Orientation.Horizontal || Owner.RightToLeft == false) return;

        float linespacing = (float)Owner.LineSpacing;
        float lineheight = (float)Owner.LineHeight;

        for (int i = 1; i < lines.Count; i++)
        {
            NBRichTextLineInfo last = lines[i - 1];
            NBRichTextLineInfo current = lines[i];

            float delta = lineheight;
            if (float.IsNaN(delta))
                delta = current.Height + linespacing;

            current.ChangeCrossAxis(last.X - delta - current.X);
        }
    }
}
