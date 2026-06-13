/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/
using Avalonia.Logging;
using NewBeeVG.Core.Controls.Layout;

namespace NewBeeVG;

public class NBLayoutable : NBVisual
{
    public int? HAlign { get; set; }
    public int? VAlign { get; set; }
    public double Width { get; set; } = double.NaN;
    public double Height { get; set; } = double.NaN;
    public double MinWidth { get; set; } = 0;
    public double MaxWidth { get; set; } = double.PositiveInfinity;
    public double MinHeight { get; set; } = 0;
    public double MaxHeight { get; set; } = double.PositiveInfinity;
    public Thickness Margin { get; set; } = new Thickness();


    /// <summary>
    /// Gets or sets a value that determines whether the element should be snapped to pixel
    /// boundaries at layout time.
    /// </summary>
    public bool UseLayoutRounding { get; set; }

    /// <summary>
    /// Gets the size that this element computed during the measure pass of the layout process.
    /// </summary>
    public Size DesiredSize
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets a value indicating whether the control's layout measure is valid.
    /// </summary>
    public bool IsMeasureValid
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets a value indicating whether the control's layouts arrange is valid.
    /// </summary>
    public bool IsArrangeValid
    {
        get;
        private set;
    }

    private bool _measuring;
    private Size? _previousMeasure;
    private Rect? _previousArrange;
    private EventHandler? _layoutUpdated;
    private bool _isAttachingToVisualTree;

    /// <summary>
    /// Gets the available size passed in the previous layout pass, if any.
    /// </summary>
    internal Size? PreviousMeasure => _previousMeasure;

    /// <summary>
    /// Gets the layout rect passed in the previous layout pass, if any.
    /// </summary>
    internal Rect? PreviousArrange => _previousArrange;

    private static bool ValidateDimension(double value) => double.IsNaN(value) || ValidateMinimumDimension(value);
    private static bool ValidateMinimumDimension(double value) => !double.IsPositiveInfinity(value) && ValidateMaximumDimension(value);
    private static bool ValidateMaximumDimension(double value) => value >= 0;

    private static bool ValidateThickness(Thickness value) => double.IsFinite(value.Left) && double.IsFinite(value.Top) && double.IsFinite(value.Right) && double.IsFinite(value.Bottom);

    /// <summary>
    /// Carries out a measure of the control.
    /// </summary>
    /// <param name="availableSize">The available size for the control.</param>
    public void Measure(Size availableSize)
    {
        if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
        {
            throw new InvalidOperationException("Cannot call Measure using a size with NaN values.");
        }

        if (!IsMeasureValid || _previousMeasure != availableSize)
        {
            //using var activity = Diagnostic.MeasuringLayoutable()?
            //    .AddTag(Diagnostic.Tags.Control, this);

            var previousDesiredSize = DesiredSize;
            var desiredSize = default(Size);

            IsMeasureValid = true;

            try
            {
                _measuring = true;
                desiredSize = MeasureCore(availableSize);
            }
            finally
            {
                _measuring = false;
            }

            if (IsInvalidSize(desiredSize))
            {
                throw new InvalidOperationException("Invalid size returned for Measure.");
            }

            DesiredSize = desiredSize;
            _previousMeasure = availableSize;

            Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Measure requested {DesiredSize}", DesiredSize);

            if (DesiredSize != previousDesiredSize)
            {
                this.GetVisualParentLayoutable()?.ChildDesiredSizeChanged(this);
            }
        }
    }

    protected NBLayoutable? GetVisualParentLayoutable()
    {
        if (this.VisualParent == null) return null;
        else if(this.VisualParent is NBLayoutable layoutable) return layoutable;
        else return null;
    }

    /// <summary>
    /// Arranges the control and its children.
    /// </summary>
    /// <param name="rect">The control's new bounds.</param>
    public void Arrange(Rect rect)
    {
        if (IsInvalidRect(rect))
        {
            throw new InvalidOperationException("Invalid Arrange rectangle.");
        }

        if (!IsMeasureValid)
        {
            Measure(_previousMeasure ?? rect.Size);
        }

        if (!IsArrangeValid || _previousArrange != rect)
        {
            //using var activity = Diagnostic.ArrangingLayoutable()?
            //    .AddTag(Diagnostic.Tags.Control, this);

            Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Arrange to {Rect} ", rect);

            IsArrangeValid = true;
            ArrangeCore(rect);
            _previousArrange = rect;
        }
    }

    /// <summary>
    /// Invalidates the measurement of the control and queues a new layout pass.
    /// </summary>
    public void InvalidateMeasure()
    {
        if (IsMeasureValid)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Invalidated measure");

            IsMeasureValid = false;
            IsArrangeValid = false;

            if (IsAttachedToVisualTree)
            {
                this.GetLayoutManager()?.InvalidateMeasure(this);
                InvalidateVisual();
            }
            OnMeasureInvalidated();
        }
    }

    /// <summary>
    /// Invalidates the arrangement of the control and queues a new layout pass.
    /// </summary>
    public void InvalidateArrange()
    {
        if (IsArrangeValid)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Invalidated arrange");

            IsArrangeValid = false;
            this.GetLayoutManager()?.InvalidateArrange(this);
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Called when a child control's desired size changes.
    /// </summary>
    /// <param name="control">The child control.</param>
    internal void ChildDesiredSizeChanged(NBLayoutable control)
    {
        if (!_measuring)
        {
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Called by InvalidateMeasure
    /// </summary>
    protected virtual void OnMeasureInvalidated()
    {
    }

    /// <summary>
    /// The default implementation of the control's measure pass.
    /// </summary>
    /// <param name="availableSize">The size available to the control.</param>
    /// <returns>The desired size for the control.</returns>
    /// <remarks>
    /// This method calls <see cref="MeasureOverride(Size)"/> which is probably the method you
    /// want to override in order to modify a control's arrangement.
    /// </remarks>
    protected virtual Size MeasureCore(Size availableSize)
    {
        if (IsVisible)
        {
            var margin = Margin;
            var useLayoutRounding = UseLayoutRounding;
            var scale = 1.0;

            if (useLayoutRounding)
            {
                scale = NBLayoutHelper.GetLayoutScale(this);
                margin = NBLayoutHelper.RoundLayoutThickness(margin, scale);
            }

            var minMax = new MinMax(this);

            var constrainedSize = NBLayoutHelper.ApplyLayoutConstraints(
                minMax,
                availableSize.Deflate(margin));

            var isContainer = false;
            NBContainerSizing containerSizing = NBContainerSizing.Normal;

            if (this is NBContainer container)
            {
                isContainer = true;
                containerSizing = container.Sizing;
                var queryProvider = container.QueryProvider;
                if(queryProvider != null && containerSizing != NBContainerSizing.Normal)
                {
                    queryProvider.SetSize(constrainedSize.Width, constrainedSize.Height, containerSizing);
                }
            }

            var measured = MeasureOverride(constrainedSize);

            var width = MathUtilities.Clamp(measured.Width, minMax.MinWidth, minMax.MaxWidth);
            var height = MathUtilities.Clamp(measured.Height, minMax.MinHeight, minMax.MaxHeight);

            if (isContainer)
            {
                switch (containerSizing)
                {
                    case NBContainerSizing.Width:
                        width = double.IsInfinity(constrainedSize.Width) ? width : constrainedSize.Width;
                        break;
                    case NBContainerSizing.Height:
                        width = measured.Width;
                        height = double.IsInfinity(constrainedSize.Height) ? height : constrainedSize.Height;
                        break;
                    case NBContainerSizing.WidthAndHeight:
                        width = double.IsInfinity(constrainedSize.Width) ? width : constrainedSize.Width;
                        height = double.IsInfinity(constrainedSize.Height) ? height : constrainedSize.Height;
                        break;
                }
            }

            if (useLayoutRounding)
            {
                (width, height) = LayoutHelper.RoundLayoutSizeUp(new Size(width, height), scale);
            }

            width += margin.Left + margin.Right;
            height += margin.Top + margin.Bottom;

            if (width > availableSize.Width)
                width = availableSize.Width;

            if (height > availableSize.Height)
                height = availableSize.Height;

            if (width < 0)
                width = 0;

            if (height < 0)
                height = 0;

            return new Size(width, height);
        }
        else
        {
            return new Size();
        }
    }

    /// <summary>
    /// Measures the control and its child elements as part of a layout pass.
    /// </summary>
    /// <param name="availableSize">The size available to the control.</param>
    /// <returns>The desired size for the control.</returns>
    protected virtual Size MeasureOverride(Size availableSize)
    {
        double width = 0;
        double height = 0;

        var visualChildren = VisualChildren;
        var visualCount = visualChildren.Count;

        for (var i = 0; i < visualCount; i++)
        {
            var visual = visualChildren[i];

            if (visual is NBLayoutable layoutable)
            {
                layoutable.Measure(availableSize);
                var childSize = layoutable.DesiredSize;

                if (childSize.Width > width)
                    width = childSize.Width;

                if (childSize.Height > height)
                    height = childSize.Height;
            }
        }

        return new Size(width, height);
    }

    /// <summary>
    /// The default implementation of the control's arrange pass.
    /// </summary>
    /// <param name="finalRect">The control's new bounds.</param>
    /// <remarks>
    /// This method calls <see cref="ArrangeOverride(Size)"/> which is probably the method you
    /// want to override in order to modify a control's arrangement.
    /// </remarks>
    protected virtual void ArrangeCore(Rect finalRect)
    {
        if (IsVisible)
        {
            var useLayoutRounding = UseLayoutRounding;
            var scale = NBLayoutHelper.GetLayoutScale(this);

            var margin = Margin;
            var originX = finalRect.X + margin.Left;
            var originY = finalRect.Y + margin.Top;

            // Margin has to be treated separately because the layout rounding function is not linear
            // f(a + b) != f(a) + f(b)
            // If the margin isn't pre-rounded some sizes will be offset by 1 pixel in certain scales.
            if (useLayoutRounding)
            {
                margin = LayoutHelper.RoundLayoutThickness(margin, scale);
            }


            var availableWidthMinusMargins = finalRect.Width - margin.Left - margin.Right;
            if (availableWidthMinusMargins < 0)
                availableWidthMinusMargins = 0;

            var availableHeightMinusMargins = finalRect.Height - margin.Top - margin.Bottom;
            if (availableHeightMinusMargins < 0)
                availableHeightMinusMargins = 0;

            var availableSizeMinusMargins = new Size(availableWidthMinusMargins, availableHeightMinusMargins);
            var horizontalAlignment = HAlign;
            var verticalAlignment = VAlign;
            var size = availableSizeMinusMargins;

            if (horizontalAlignment != null)
            {
                size = size.WithWidth(Math.Min(size.Width, DesiredSize.Width - margin.Left - margin.Right));
            }

            if (verticalAlignment != null)
            {
                size = size.WithHeight(Math.Min(size.Height, DesiredSize.Height - margin.Top - margin.Bottom));
            }

            size = NBLayoutHelper.ApplyLayoutConstraints(new MinMax(this), size);

            if (useLayoutRounding)
            {
                size = LayoutHelper.RoundLayoutSizeUp(size, scale);
                availableSizeMinusMargins = LayoutHelper.RoundLayoutSizeUp(availableSizeMinusMargins, scale);
            }

            size = ArrangeOverride(size).Constrain(size);

            switch (horizontalAlignment)
            {
                case 0:
                case null:
                    originX += (availableSizeMinusMargins.Width - size.Width) / 2;
                    break;
                case >0:
                    originX += availableSizeMinusMargins.Width - size.Width;
                    break;
            }

            switch (verticalAlignment)
            {
                case 0:
                case null:
                    originY += (availableSizeMinusMargins.Height - size.Height) / 2;
                    break;
                case >0:
                    originY += availableSizeMinusMargins.Height - size.Height;
                    break;
            }

            var origin = new Point(originX, originY);

            if (useLayoutRounding)
            {
                origin = LayoutHelper.RoundLayoutPoint(origin, scale);
            }

            Bounds = new Rect(origin, size);
        }
    }

    /// <summary>
    /// Positions child elements as part of a layout pass.
    /// </summary>
    /// <param name="finalSize">The size available to the control.</param>
    /// <returns>The actual size used.</returns>
    protected virtual Size ArrangeOverride(Size finalSize)
    {
        var arrangeRect = new Rect(finalSize);

        var visualChildren = VisualChildren;
        var visualCount = visualChildren.Count;

        for (var i = 0; i < visualCount; i++)
        {
            var visual = visualChildren[i];

            if (visual is NBLayoutable layoutable)
            {
                layoutable.Arrange(arrangeRect);
            }
        }

        return finalSize;
    }

    /// <summary>
    /// Tests whether any of a <see cref="Rect"/>'s properties include negative values,
    /// a NaN or Infinity.
    /// </summary>
    /// <param name="rect">The rect.</param>
    /// <returns>True if the rect is invalid; otherwise false.</returns>
    private static bool IsInvalidRect(Rect rect)
    {
        return MathUtilities.IsNegativeOrNonFinite(rect.Width) ||
            MathUtilities.IsNegativeOrNonFinite(rect.Height) ||
            !MathUtilities.IsFinite(rect.X) ||
            !MathUtilities.IsFinite(rect.Y);
    }

    /// <summary>
    /// Tests whether any of a <see cref="Size"/>'s properties include negative values,
    /// a NaN or Infinity.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <returns>True if the size is invalid; otherwise false.</returns>
    private static bool IsInvalidSize(Size size)
    {
        return MathUtilities.IsNegativeOrNonFinite(size.Width) ||
            MathUtilities.IsNegativeOrNonFinite(size.Height);
    }

    /// <summary>
    /// Ensures neither component of a <see cref="Size"/> is negative.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <returns>The non-negative size.</returns>
    private static Size NonNegative(Size size)
    {
        return new Size(Math.Max(size.Width, 0), Math.Max(size.Height, 0));
    }
}
