/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using NewBeeVG.Core.Controls.Layout;

namespace NewBeeVG;

/// <summary>
/// Base class for controls which decorate a single child control.
/// </summary>
public class NBDecorator : NBLayoutable, IPaddingable
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

public static partial class NBExtentions
{
    public static T Child<T>(this T decorator, NBLayoutable child) where T : NBDecorator
    {
        decorator.Child = child;
        return decorator;
    }
}
