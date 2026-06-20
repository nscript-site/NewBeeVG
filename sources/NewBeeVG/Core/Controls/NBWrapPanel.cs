/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG;

public class NBWrapPanel : NBPanel
{
    public double ItemSpacing { get; set; } = 20;

    public double LineSpacing { get; set; } = 20;

    public int ItemsAlignment { get; set; } = -1;

    public double ItemWidth { get; set; } = double.NaN;

    public double ItemHeight { get; set; } = double.NaN;

    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    protected override Size MeasureOverride(Size constraint)
    {
        double itemWidth = ItemWidth;
        double itemHeight = ItemHeight;
        double itemSpacing = ItemSpacing;
        double lineSpacing = LineSpacing;
        var orientation = Orientation;
        var children = Children;
        var curLineSize = new UVSize(orientation);
        var panelSize = new UVSize(orientation);
        var uvConstraint = new UVSize(orientation, constraint.Width, constraint.Height);
        bool itemWidthSet = !double.IsNaN(itemWidth);
        bool itemHeightSet = !double.IsNaN(itemHeight);
        bool itemExists = false;
        bool lineExists = false;

        var childConstraint = new Size(
            itemWidthSet ? itemWidth : constraint.Width,
            itemHeightSet ? itemHeight : constraint.Height);

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];
            // Flow passes its own constraint to children
            child.TryMeasure(childConstraint);

            // This is the size of the child in UV space
            UVSize childSize = new UVSize(orientation,
                itemWidthSet ? itemWidth : child.DesiredSize.Width,
                itemHeightSet ? itemHeight : child.DesiredSize.Height);

            var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
            if (MathUtilities.GreaterThan(curLineSize.U + childSize.U + nextSpacing, uvConstraint.U)) // Need to switch to another line
            {
                panelSize.U = Math.Max(curLineSize.U, panelSize.U);
                panelSize.V += curLineSize.V + (lineExists ? lineSpacing : 0);
                curLineSize = childSize;

                itemExists = child.IsVisible;
                lineExists = true;
            }
            else // Continue to accumulate a line
            {
                curLineSize.U += childSize.U + nextSpacing;
                curLineSize.V = Math.Max(childSize.V, curLineSize.V);

                itemExists |= child.IsVisible; // keep true
            }
        }

        // The last line size, if any should be added
        panelSize.U = Math.Max(curLineSize.U, panelSize.U);
        panelSize.V += curLineSize.V + (lineExists ? lineSpacing : 0);

        // Go from UV space to W/H space
        return new Size(panelSize.Width, panelSize.Height);
    }

    protected override void ArrangeChilds(Point origin, Size finalSize)
    {
        double itemWidth = ItemWidth;
        double itemHeight = ItemHeight;
        double itemSpacing = ItemSpacing;
        double lineSpacing = LineSpacing;
        var orientation = Orientation;
        bool isHorizontal = orientation == Orientation.Horizontal;
        var children = Children;
        int firstInLine = 0;
        double accumulatedV = 0;
        double itemU = isHorizontal ? itemWidth : itemHeight;
        var curLineSize = new UVSize(orientation);
        var uvFinalSize = new UVSize(orientation, finalSize.Width, finalSize.Height);
        bool itemWidthSet = !double.IsNaN(itemWidth);
        bool itemHeightSet = !double.IsNaN(itemHeight);
        bool itemExists = false;
        bool lineExists = false;

        for (int i = 0; i < children.Count; ++i)
        {
            var child = children[i];
            var childSize = new UVSize(orientation,
                itemWidthSet ? itemWidth : child.DesiredSize.Width,
                itemHeightSet ? itemHeight : child.DesiredSize.Height);

            var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
            if (MathUtilities.GreaterThan(curLineSize.U + childSize.U + nextSpacing, uvFinalSize.U)) // Need to switch to another line
            {
                accumulatedV += lineExists ? lineSpacing : 0; // add spacing to arrange line first
                ArrangeLine(curLineSize.V, firstInLine, i);
                accumulatedV += curLineSize.V; // add the height of the line just arranged
                curLineSize = childSize;

                firstInLine = i;

                itemExists = child.IsVisible;
                lineExists = true;
            }
            else // Continue to accumulate a line
            {
                curLineSize.U += childSize.U + nextSpacing;
                curLineSize.V = Math.Max(childSize.V, curLineSize.V);

                itemExists |= child.IsVisible; // keep true
            }
        }

        // Arrange the last line, if any
        if (firstInLine < children.Count)
        {
            accumulatedV += lineExists ? lineSpacing : 0; // add spacing to arrange line first
            ArrangeLine(curLineSize.V, firstInLine, children.Count);
        }

        return;

        void ArrangeLine(double lineV, int start, int end)
        {
            bool useItemU = isHorizontal ? itemWidthSet : itemHeightSet;
            double u = 0;
            if (ItemsAlignment >= 0)
            {
                double totalU = -itemSpacing;
                for (int i = start; i < end; ++i)
                {
                    totalU += GetChildU(i) + (!children[i].IsVisible ? 0 : itemSpacing);
                }

                u = ItemsAlignment switch
                {
                    0 => (uvFinalSize.U - totalU) / 2,
                    > 0 => uvFinalSize.U - totalU,
                    < 0 => 0
                };
            }

            for (int i = start; i < end; ++i)
            {
                double layoutSlotU = GetChildU(i);
                children[i].TryArrange(isHorizontal ? new(origin.X + u, origin.Y + accumulatedV, layoutSlotU, lineV) : new(origin.X + accumulatedV, origin.Y + u, lineV, layoutSlotU));
                u += layoutSlotU + (!children[i].IsVisible ? 0 : itemSpacing);
            }

            return;
            double GetChildU(int i) => useItemU ? itemU :
                isHorizontal ? children[i].DesiredSize.Width : children[i].DesiredSize.Height;
        }
    }

    private struct UVSize
    {
        internal UVSize(Orientation orientation, double width, double height)
        {
            U = V = 0d;
            _orientation = orientation;
            Width = width;
            Height = height;
        }

        internal UVSize(Orientation orientation)
        {
            U = V = 0d;
            _orientation = orientation;
        }

        internal double U;
        internal double V;
        private Orientation _orientation;

        internal double Width
        {
            get => _orientation == Orientation.Horizontal ? U : V;
            set { if (_orientation == Orientation.Horizontal) U = value; else V = value; }
        }
        internal double Height
        {
            get => _orientation == Orientation.Horizontal ? V : U;
            set { if (_orientation == Orientation.Horizontal) V = value; else U = value; }
        }
    }
}
