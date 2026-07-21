/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using Avalonia.Collections;
using SkiaSharp;

namespace NewBeeVG;

public class NBVisual
{
    public string? Id { get; set; }
    public bool IsVisible { get; set; } = true;
    public double Opacity { get; set; } = 1.0;
    public SKMatrix? RenderTransform { get; set; }
    public SKRect Bounds { get; set; }
    public SKPath? ClipPath { get; set; }
    public bool ClipToBounds { get; set; } = false;

    public SKImageFilter? Filter { get; set; }
    public SKColorFilter? ColorFilter { get; set; }
    public NBShaderCollection Shaders { get; private set; } = new NBShaderCollection();

    public string? BoundedId { get; set; }

    public Action<NBFrameUpdateEvent>? OnFrameUpdated { get; set; }

    /// <summary>
    /// 在 Canvas 中的位置。
    /// </summary>
    public NBPosition? PositionInCanvas { get; set; }

    public NBParamsInGrid? ParamsInGrid { get; set; }

    /// <summary>
    /// Gets the size that this element computed during the measure pass of the layout process.
    /// </summary>
    public Size DesiredSize
    {
        get;
        protected set;
    }
    /// <summary>
    /// Gets the control's child visuals.
    /// </summary>
    protected internal IAvaloniaList<NBVisual> VisualChildren { get; } = new AvaloniaList<NBVisual>();

    public void FireOnFrameUpdated(NBFrameUpdateEvent e)
    {
        if(OnFrameUpdated != null)
        {
            e.Sender = this;
            OnFrameUpdated.Invoke(e);
        }

        foreach (var child in VisualChildren)
        {
            child.FireOnFrameUpdated(e);
        }
    }

    public NBVisual? Find(string? id)
    {
        if (id == null) return null;

        if (this.Id == id)
        {
            return this;
        }

        foreach (var child in VisualChildren)
        {
            var found = child.Find(id);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    public List<NBVisual>? FindBounded()
    {
        List<NBVisual>? match = null;

        if (BoundedId != null)
        {
            if(match == null) match = new List<NBVisual>();
            match.Add(this);
        }

        foreach (var item in this.VisualChildren)
        {
            var found = item.FindBounded();
            if(found != null)
            {
                if(match == null) match = new List<NBVisual>();
                match.AddRange(found);
            }
        }
        return match;
    }

    /// <summary>
    /// 绘制背景
    /// </summary>
    /// <param name="context"></param>
    protected virtual void RenderBackground(SKCanvas context)
    {
    }

    /// <summary>
    /// 绘制装饰物
    /// </summary>
    /// <param name="context"></param>
    protected virtual void RenderDecorations(SKCanvas context)
    {
    }

    public void Render(SKCanvas context)
    {
        context.Save();

        if (this.RenderTransform != null)
        {
            var bounds = this.Bounds;
            var centerX = bounds.Left + bounds.Width / 2;
            var centerY = bounds.Top + bounds.Height / 2;
            context.Concat(SKMatrix.CreateTranslation(centerX, centerY));
            context.Concat(this.RenderTransform.Value);
            context.Concat(SKMatrix.CreateTranslation(-centerX, -centerY));
        }

        if (ClipPath != null)
        {
            context.ClipPath(ClipPath, SKClipOperation.Intersect, true);
        }
        else if (ClipToBounds)
        {
            context.ClipRect(Bounds);
        }

        bool useOpacityLayer = Opacity < 1.0;
        if (useOpacityLayer || Filter != null || ColorFilter != null)
        {
            byte alpha = (byte)Math.Clamp(Opacity * 255.0, 0, 255);
            using var layerPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(alpha), 
                IsAntialias = true,
                ImageFilter = Filter,
                ColorFilter = ColorFilter
            };

            context.SaveLayer(layerPaint);
        }

        RenderCore(context);

        if (useOpacityLayer)
        {
            context.Restore();
        }

        context.Restore();
    }

    protected virtual void RenderCore(SKCanvas context)
    {
        RenderBackground(context);

        foreach (var child in VisualChildren)
        {
            if (child.IsVisible)
            {
                context.Save();
                child.Render(context);
                context.Restore();
            }
        }

        RenderDecorations(context);
    }

    internal protected virtual void TryMeasure(Size availableSize)
    {
        if(this is NBLayoutable layoutable)
        {
            layoutable.Measure(availableSize);
        }
    }

    internal protected virtual void TryArrange(Rect rect)
    {
        if(this is NBLayoutable layoutable)
        {
            layoutable.Arrange(rect);
        }
    }

    public void TryInvalidateMeasure()
    {
        if (this is NBLayoutable layoutable)
        {
            layoutable.InvalidateMeasure();
        }

        foreach(var item in VisualChildren)
        {
            item.TryInvalidateMeasure();
        }
    }

    internal protected void TryArrange(Point offset, Rect rect)
    {
        var offsetRect = new Rect(new Point(offset.X + rect.X, offset.Y + rect.Y), rect.Size);
        TryArrange(offsetRect);
    }

    public void Col(int column)
    {
        if(ParamsInGrid == null)
        {
            ParamsInGrid = new NBParamsInGrid();
        }
        ParamsInGrid.Column = column;
    }

    public void Row(int row)
    {
        if (ParamsInGrid == null)
        {
            ParamsInGrid = new NBParamsInGrid();
        }
        ParamsInGrid.Row = row;
    }

    public void ColSpan(int colSpan)
    {
        if (ParamsInGrid == null)
        {
            ParamsInGrid = new NBParamsInGrid();
        }
        ParamsInGrid.ColumnSpan = colSpan;
    }

    public void RowSpan(int rowSpan)
    {
        if (ParamsInGrid == null)
        {
            ParamsInGrid = new NBParamsInGrid();
        }
        ParamsInGrid.RowSpan = rowSpan;
    }
}

public static partial class NBExtentions
{
    public static T Ref<T>(this T widget, out T value) where T : NBVisual
    {
        value = widget;
        return widget;
    }

    public static NBDrawingClip AsClip<T>(this T widget, out NBDrawingClip value, int frames, int? start = null, string name = "clip") where T : NBVisual
    {
        value = Methods.clip(widget, name: name, frames: frames, start: start); 
        return value;
    }

    public static T OnFrame<T>(this T widget, Action<NBFrameUpdateEvent>? onFrame) where T : NBVisual
    {
        if(onFrame != null)
        {
            widget.OnFrameUpdated += onFrame;
        }
        return widget;
    }

    public static T Id<T>(this T widget, string id) where T : NBVisual
    {
        widget.Id = id;
        return widget;
    }

    public static T Bind<T>(this T widget, string boundedId) where T : NBVisual
    {
        widget.BoundedId = boundedId;
        return widget;
    }

    public static T RenderTransform<T>(this T widget, SKMatrix? m) where T : NBVisual
    {
        widget.RenderTransform = m;
        return widget;  
    }

    public static T ClipPath<T>(this T widget, SKPath? path) where T : NBVisual
    {
        widget.ClipPath = path;
        return widget;
    }

    public static T ClipToBounds<T>(this T widget, bool clipToBounds) where T : NBVisual
    {
        widget.ClipToBounds = clipToBounds;
        return widget;
    }

    public static T Opacity<T>(this T widget, double opacity) where T : NBVisual
    {
        widget.Opacity = opacity;
        return widget;
    }

    public static T Filter<T>(this T widget, params SKImageFilter?[] filters) where T : NBVisual
    {
        if(filters == null || filters.Length == 0)
        {
            widget.Filter = null;
        }
        else if (filters.Length == 1)
        {
            widget.Filter = filters[0];
        }
        else if (filters.Length > 1)
        {
            var list = new List<SKImageFilter>();
            foreach (var filter in filters)
            {
                if (filter != null)
                {
                    list.Add(filter);
                }
            }

            if (list.Count == 1)
            {
                widget.Filter = list[0];
            }
            else if (list.Count > 1)
            {
                var f0 = list[0];
                for(int i = 1; i < list.Count; i++)
                {
                    // Compose filters in order, the last filter is applied first
                    f0 = SKImageFilter.CreateCompose(list[i], f0);
                }
                widget.Filter = f0;
            }
        }
        return widget;
    }

    public static T ColorFilter<T>(this T widget, params SKColorFilter?[] filters) where T : NBVisual
    {
        if (filters == null || filters.Length == 0)
        {
            widget.ColorFilter = null;
        }
        else if (filters.Length == 1)
        {
            widget.ColorFilter = filters[0];
        }
        else if (filters.Length > 1)
        {
            var list = new List<SKColorFilter>();
            foreach (var filter in filters)
            {
                if (filter != null)
                {
                    list.Add(filter);
                }
            }
            if (list.Count == 1)
            {
                widget.ColorFilter = list[0];
            }
            else if (list.Count > 1)
            {
                var f0 = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    // Compose color filters in order, the last filter is applied first
                    f0 = SKColorFilter.CreateCompose(list[i], f0);
                }
                widget.ColorFilter = f0;
            }
        }
        return widget;
    }

    public static T Shader<T>(this T widget, params Func<SKRect, SKShader>?[] shaderFuncs) where T : NBVisual
    {
        if (shaderFuncs == null || shaderFuncs.Length == 0)
        {
            widget.Shaders.ClearShaders();
            return widget;
        }


        foreach (var shaderFunc in shaderFuncs)
        {
            if (shaderFunc != null)
            {
                widget.Shaders.AddShader(new NBFuncShader(shaderFunc));
            }
        }
        return widget;
    }

    public static T Shader<T>(this T widget, params NBShader?[] shaders) where T : NBVisual
    {
        if (shaders == null || shaders.Length == 0)
        {
            widget.Shaders.ClearShaders();
            return widget;
        }


        foreach (var shaderFunc in shaders)
        {
            if (shaderFunc != null)
            {
                widget.Shaders.AddShader(shaderFunc);
            }
        }
        return widget;
    }
}