using NewBeeVG.Core.Controls.Layout;

namespace NewBeeVG;

/// <summary>
/// Base class for controls which decorate a single child control.
/// </summary>
public class NBDecorator : NBLayoutable
{
    public NBLayoutable? Child { get; set { ChildChanged(field, value); field = value;  } }

    public Thickness Padding { get; set; }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        return NBLayoutHelper.MeasureChild(Child, availableSize, Padding);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        return NBLayoutHelper.ArrangeChild(Child, finalSize, Padding);
    }

    private void ChildChanged(NBLayoutable? oldChild, NBLayoutable? newChild)
    {
        if (oldChild != null)
        {
            VisualChildren.Remove(oldChild);
        }

        if (newChild != null)
        {
            VisualChildren.Add(newChild);
        }
    }
}

public static class NBDecorator_Extentions
{
    public static T Child<T>(this T decorator, NBLayoutable child) where T : NBDecorator
    {
        decorator.Child = child;
        return decorator;
    }

    public static T Padding<T>(this T decorator, double left, double top, double right, double bottom) where T : NBDecorator
    {
        decorator.Padding = new Thickness(left, top, right, bottom);
        return decorator;
    }

    public static T Padding<T>(this T decorator, double uniform) where T : NBDecorator
    {
        decorator.Padding = new Thickness(uniform);
        return decorator;
    }

    public static T Padding<T>(this T decorator, double horizontal, double vertical) where T : NBDecorator
    {
        decorator.Padding = new Thickness(horizontal, vertical);
        return decorator;
    }
}
