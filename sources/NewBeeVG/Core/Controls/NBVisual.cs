/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using Avalonia.Collections;
using Avalonia.Rendering;
using NewBeeVG.Layout;
using SkiaSharp;

namespace NewBeeVG;

public class NBVisual
{
    public bool IsVisible { get; set; } = true;
    public double Opacity { get; set; } = 1.0;
    public ITransform? RenderTransform { get; set; }
    public Rect Bounds { get; set; }
    public bool ClipToBounds { get; set; } = false;

    private NBVisual? _visualParent;
    internal NBVisual? VisualParent => _visualParent;

    internal bool IsAttachedToVisualTree { get; set; }

    /// <summary>
    /// Gets the control's child visuals.
    /// </summary>
    protected internal IAvaloniaList<NBVisual> VisualChildren { get; }

    public virtual void Render(SKCanvas context)
    {

    }

    public void InvalidateVisual()
    {
    }

    internal ILayoutRoot? GetLayoutRoot()
    {
        return null;
    }

    internal INBLayoutManager? GetLayoutManager()
    {
        return null;
    }
}
