namespace NewBeeVG;

public class NBStack : NBPanel
{
    public double Spacing { get; set; } = 10;

    public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>
    /// General StackPanel layout behavior is to grow unbounded in the "stacking" direction (Size To Content).
    /// Children in this dimension are encouraged to be as large as they like.  In the other dimension,
    /// StackPanel will assume the maximum size of its children.
    /// </summary>
    /// <param name="availableSize">Constraint</param>
    /// <returns>Desired size</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size stackDesiredSize = new Size();
        var children = Children;
        Size layoutSlotSize = availableSize;
        bool fHorizontal = (Orientation == Orientation.Horizontal);
        double spacing = Spacing;
        bool hasVisibleChild = false;

        //
        // Initialize child sizing and iterator data
        // Allow children as much size as they want along the stack.
        //
        if (fHorizontal)
        {
            layoutSlotSize = layoutSlotSize.WithWidth(Double.PositiveInfinity);
        }
        else
        {
            layoutSlotSize = layoutSlotSize.WithHeight(Double.PositiveInfinity);
        }

        //
        //  Iterate through children.
        //  While we still supported virtualization, this was hidden in a child iterator (see source history).
        //
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            // Get next child.
            var child = children[i];

            bool isVisible = child.IsVisible;

            if (isVisible && !hasVisibleChild)
            {
                hasVisibleChild = true;
            }

            // Measure the child.
            child.TryMeasure(layoutSlotSize);

            Size childDesiredSize = child.DesiredSize;

            // Accumulate child size.
            if (fHorizontal)
            {
                stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width + (isVisible ? spacing : 0) + childDesiredSize.Width);
                stackDesiredSize = stackDesiredSize.WithHeight(Math.Max(stackDesiredSize.Height, childDesiredSize.Height));
            }
            else
            {
                stackDesiredSize = stackDesiredSize.WithWidth(Math.Max(stackDesiredSize.Width, childDesiredSize.Width));
                stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height + (isVisible ? spacing : 0) + childDesiredSize.Height);
            }
        }

        if (fHorizontal)
        {
            stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width - (hasVisibleChild ? spacing : 0));
        }
        else
        {
            stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height - (hasVisibleChild ? spacing : 0));
        }

        return stackDesiredSize;
    }

    protected override void ArrangeChilds(Point origin, Size size)
    {
        var finalSize = size;
        var children = Children;
        bool fHorizontal = (Orientation == Orientation.Horizontal);
        Rect rcChild = new Rect(origin, finalSize);
        double previousChildSize = 0.0;
        var spacing = Spacing;

        //
        // Arrange and Position Children.
        //
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (!child.IsVisible)
            {
                continue;
            }

            if (fHorizontal)
            {
                rcChild = rcChild.WithX(rcChild.X + previousChildSize);
                previousChildSize = child.DesiredSize.Width;
                rcChild = rcChild.WithWidth(previousChildSize);
                rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
                previousChildSize += spacing;
            }
            else
            {
                rcChild = rcChild.WithY(rcChild.Y + previousChildSize);
                previousChildSize = child.DesiredSize.Height;
                rcChild = rcChild.WithHeight(previousChildSize);
                rcChild = rcChild.WithWidth(Math.Max(finalSize.Width, child.DesiredSize.Width));
                previousChildSize += spacing;
            }

            ArrangeChild(child, rcChild, finalSize, Orientation);
        }
    }

    internal virtual void ArrangeChild(
        NBVisual child,
        Rect rect,
        Size panelSize,
        Orientation orientation)
    {
        child.TryArrange(rect);
    }
}

public static partial class NBExtentions
{
    public static T Spacing<T>(this T decorator, double spacing) where T : NBStack
    {
        decorator.Spacing = spacing;
        return decorator;
    }
}