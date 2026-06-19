/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG;

public class NBCanvas : NBPanel
{
    /// <summary>
    /// Gets the value of the Left attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <returns>The control's left coordinate.</returns>
    public static double GetLeft(NBVisual element)
    {
        return element.PositionInCanvas?.Left ?? double.NaN;
    }

    /// <summary>
    /// Sets the value of the Left attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The left value.</param>
    public static void SetLeft(NBVisual element, double value)
    {
        if (element.PositionInCanvas == null)
        {
            element.PositionInCanvas = new NBPosition();
        }
        element.PositionInCanvas = new NBPosition
        {
            Left = value,
            Top = element.PositionInCanvas.Value.Top,
            Right = element.PositionInCanvas.Value.Right,
            Bottom = element.PositionInCanvas.Value.Bottom
        };
    }

    /// <summary>
    /// Gets the value of the Top attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <returns>The control's top coordinate.</returns>
    public static double GetTop(NBVisual element)
    {
        return element.PositionInCanvas?.Top ?? double.NaN;
    }

    /// <summary>
    /// Sets the value of the Top attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The top value.</param>
    public static void SetTop(NBVisual element, double value)
    {
        if (element.PositionInCanvas == null)
        {
            element.PositionInCanvas = new NBPosition();
        }
        element.PositionInCanvas = new NBPosition
        {
            Left = element.PositionInCanvas.Value.Left,
            Top = value,
            Right = element.PositionInCanvas.Value.Right,
            Bottom = element.PositionInCanvas.Value.Bottom
        };
    }

    /// <summary>
    /// Gets the value of the Right attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <returns>The control's right coordinate.</returns>
    public static double GetRight(NBVisual element)
    {
        return element.PositionInCanvas?.Right ?? double.NaN;
    }

    /// <summary>
    /// Sets the value of the Right attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The right value.</param>
    public static void SetRight(NBVisual element, double value)
    {
        if (element.PositionInCanvas == null)
        {
            element.PositionInCanvas = new NBPosition();
        }
        element.PositionInCanvas = new NBPosition
        {
            Left = element.PositionInCanvas.Value.Left,
            Top = element.PositionInCanvas.Value.Top,
            Right = value,
            Bottom = element.PositionInCanvas.Value.Bottom
        };
    }

    /// <summary>
    /// Gets the value of the Bottom attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <returns>The control's bottom coordinate.</returns>
    public static double GetBottom(NBVisual element)
    {
        return element.PositionInCanvas?.Bottom ?? double.NaN;
    }

    /// <summary>
    /// Sets the value of the Bottom attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The bottom value.</param>
    public static void SetBottom(NBVisual element, double value)
    {
        if (element.PositionInCanvas == null)
        {
            element.PositionInCanvas = new NBPosition();
        }
        element.PositionInCanvas = new NBPosition
        {
            Left = element.PositionInCanvas.Value.Left,
            Top = element.PositionInCanvas.Value.Top,
            Right = element.PositionInCanvas.Value.Right,
            Bottom = value
        };
    }

    /// <summary>
    /// Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        foreach (NBVisual child in Children)
        {
            if(child is NBLayoutable layoutableChild)
            {
                layoutableChild.Measure(availableSize);
            }
        }

        return new Size();
    }

    /// <summary>
    /// Arranges a single child.
    /// </summary>
    /// <param name="child">The child to arrange.</param>
    /// <param name="finalSize">The size allocated to the canvas.</param>
    protected virtual void ArrangeChild(NBVisual child, Size finalSize)
    {
        double x = 0.0;
        double y = 0.0;
        double elementLeft = GetLeft(child);

        if (!double.IsNaN(elementLeft))
        {
            x = elementLeft;
        }
        else
        {
            // Arrange with right.
            double elementRight = GetRight(child);
            if (!double.IsNaN(elementRight))
            {
                x = finalSize.Width - elementRight - child.DesiredSize.Width;
            }
        }

        double elementTop = GetTop(child);
        if (!double.IsNaN(elementTop))
        {
            y = elementTop;
        }
        else
        {
            double elementBottom = GetBottom(child);
            if (!double.IsNaN(elementBottom))
            {
                y = finalSize.Height - elementBottom - child.DesiredSize.Height;
            }
        }

        child.TryArrange(new Rect(new Point(x, y), child.DesiredSize));
    }

    /// <summary>
    /// Arranges the control's children.
    /// </summary>
    /// <param name="finalSize">The size allocated to the control.</param>
    /// <returns>The space taken.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (NBVisual child in Children)
        {
            ArrangeChild(child, finalSize);
        }

        return finalSize;
    }
}
