/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using Avalonia.Collections;
using NewBeeVG.Layout;
using SkiaSharp;

namespace NewBeeVG;

public class NBVisual
{
    public bool IsVisible { get; set; } = true;
    public double Opacity { get; set; } = 1.0;
    public SKMatrix? RenderTransform { get; set; }
    public SKRect Bounds { get; set; }
    public SKPath? ClipPath { get; set; }
    public bool ClipToBounds { get; set; } = false;

    private NBVisual? _visualParent;
    internal NBVisual? VisualParent => _visualParent;

    internal bool IsAttachedToVisualTree { get; set; }

    /// <summary>
    /// Gets the control's child visuals.
    /// </summary>
    protected internal IAvaloniaList<NBVisual> VisualChildren { get; } = new AvaloniaList<NBVisual>();

    protected virtual void BeforeRenderChildren(SKCanvas context)
    {
    }

    protected virtual void AfterRenderChildren(SKCanvas context)
    {
    }

    public void Render(SKCanvas context)
    {
        context.Save();

        if (ClipPath != null)
        {
            context.ClipPath(ClipPath, SKClipOperation.Intersect, true);
        }
        else if (ClipToBounds)
        {
            context.ClipRect(Bounds);
        }

        bool useOpacityLayer = Opacity < 1.0;
        if (useOpacityLayer)
        {
            byte alpha = (byte)Math.Clamp(Opacity * 255.0, 0, 255);
            using var layerPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(alpha)
            };

            context.SaveLayer(layerPaint);
        }

        BeforeRenderChildren(context);

        foreach (var child in VisualChildren)
        {
            if (child.IsVisible)
            {
                context.Save();
                if (child.RenderTransform != null)
                {
                    context.Concat(child.RenderTransform.Value);
                }
                child.Render(context);
                context.Restore();
            }
        }

        AfterRenderChildren(context);

        if (useOpacityLayer)
        {
            context.Restore();
        }

        context.Restore();
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
