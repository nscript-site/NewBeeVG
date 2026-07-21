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
    public NBVisual? Child { get; set { ChildChanged(field, value); field = value;  } }

    public Thickness Padding { get; set; }

    ///// <inheritdoc/>
    //protected override Size MeasureOverride(Size availableSize)
    //{
    //    if (Child == null) return new Size();

    //    if(Child is NBLayoutable layoutableChild)
    //    {
    //        NBLayoutHelper.MeasureChild(layoutableChild, availableSize, Padding);
    //    } 
    //    else
    //    {
    //        Child.TryMeasure(availableSize);
    //    }
    //    return Child.DesiredSize;
    //}

    ///// <inheritdoc/>
    //protected override Size ArrangeOverride(Size finalSize)
    //{
    //    if (Child == null) return new Size();

    //    if (Child is NBLayoutable layoutableChild)
    //    {
    //        NBLayoutHelper.ArrangeChild(layoutableChild, finalSize, Padding);
    //    }
    //    else
    //    {
    //    }

    //    return Child.DesiredSize;
    //}

    private void ChildChanged(NBVisual? oldChild, NBVisual? newChild)
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
