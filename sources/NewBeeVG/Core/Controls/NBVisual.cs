/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using Avalonia.Collections;
using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// NBVisual 是 NewBeeVG 中的基础可视元素类，提供了基本的渲染、布局和事件处理功能。它可以包含子元素，并支持各种
/// 视觉效果，如滤镜、颜色滤镜、着色器和位图滤镜。
/// 
/// 生命周期：
///     - 先调用 OnFrameUpdate 事件处理器（如果有），用于更新元素的状态。
///     - 再进行布局和排列
///     - 再进行渲染，渲染顺序为：Content -> BitmapFilters -> FrameMask -> Filters/ColorFilters/Opacity/RenderTransform。
/// 
/// 渲染逻辑：
///     - 先渲染 Content（包括背景、子元素和装饰物），子类可通过重写 RenderContent 方法来实现自定义内容的渲染。
///     - 然后应用 BitmapFilters, 如果存在 BitmapFilters，则会先将 Content 渲染到一个临时位图上，然后对该位
///       图应用 BitmapFilters，最后将处理后的位图绘制到目标画布上。有的 BitmapFilter 可能会改变位图的大小，
///       因此最终绘制的 Bounds (这里称之为 ExtendBounds ) 可能后布局安排的 Bounds 不一致。
///     - 然后应用 FrameMask，如果存在 FrameMask，则会先将 Content 渲染到一个临时位图上，然后将 FrameMask 的
///       遮罩位图绘制到该位图上，最后将处理后的位图绘制到目标画布上。FrameMask 构建 Mask 时输入的 rect 是 
///       ExtendBounds。
///     - 然后应用滤镜、颜色滤镜 和 Opacity、RenderTransform。
/// </summary>
public class NBVisual
{
    public string? Id { get; set; }
    public bool IsVisible { get; set; } = true;
    public double Opacity { get; set; } = 1.0;
    public SKMatrix? RenderTransform { get; set; }
    public SKRect Bounds { get; set; }
    public SKPath? ClipPath { get; set; }
    public bool ClipToBounds { get; set; } = false;

    public NBImageFilterCollection Filters { get; private set; } = new NBImageFilterCollection();
    public NBColorFilterCollection ColorFilters { get; private set; } = new NBColorFilterCollection();
    public NBShaderCollection Shaders { get; private set; } = new NBShaderCollection();

    public NBFrameMask? FrameMask { get; set; }

    public SKBlendMode FrameMaskBlendMode { get; set; } = SKBlendMode.SrcOut;

    public NBBitmapFilterCollection BitmapFilters { get; private set; } = new NBBitmapFilterCollection();

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
        if (useOpacityLayer || Filters.IsEmpty() == false || ColorFilters.IsEmpty() == false)
        {
            byte alpha = (byte)Math.Clamp(Opacity * 255.0, 0, 255);
            using var layerPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(alpha), 
                IsAntialias = true,
                ImageFilter = Filters.GetComposeFilter(),
                ColorFilter = ColorFilters.GetComposeFilter()
            };

            context.SaveLayer(layerPaint);
        }

        if(FrameMask != null)
        {
            RenderContentWithBitmapFilters(context, FrameMask);
        }
        else
        {
            RenderContentWithBitmapFilters(context);
        }

        if (useOpacityLayer)
        {
            context.Restore();
        }

        context.Restore();
    }

    protected void RenderContentWithBitmapFilters(SKCanvas context)
    {
        if(BitmapFilters.IsEmpty)
        {
            RenderContent(context);
            return;
        }

        var (currentBitmap, offset) = BuildContentBitmap(context);
        using var result = RenderContentBitmap(context, (currentBitmap, offset));
    }

    protected SKBitmap? RenderContentBitmap(SKCanvas context, (SKBitmap?, SKPoint) bitmapInfo)
    {
        var (currentBitmap, offset) = bitmapInfo;
        if (currentBitmap != null)
        {
            var width = (float)currentBitmap.Width;
            var height = (float)currentBitmap.Height;
            if (width > 0 && height > 0)
            {
                var sourceRect = new SKRect(0, 0, width, height);
                var dstRect = GetExtendBounds(currentBitmap, offset);
                using var paint = new SKPaint { };
                context.DrawBitmap(currentBitmap, sourceRect, dstRect, paint);
            }
        }
        return currentBitmap;
    }

    protected SKRect GetExtendBounds(SKBitmap bmp, SKPoint offset)
    {
        return new SKRect(Bounds.Left + offset.X, Bounds.Top + offset.Y, Bounds.Left + offset.X + bmp.Width, Bounds.Top + offset.Y + bmp.Height);
    }

    protected (SKBitmap?, SKPoint) BuildContentBitmap(SKCanvas context)
    {
        var size = new SKSize(Bounds.Width, Bounds.Height);
        if (size.Width <= 0 || size.Height <= 0) return(null,new SKPoint());
        var srcBitmap = new SKBitmap((int)size.Width, (int)size.Height);
        using var srcCanvas = new SKCanvas(srcBitmap);
        srcCanvas.Translate(-Bounds.Left, -Bounds.Top); // 将绘制原点移动到 Bounds 的左上角
        SKBitmap? output = null;
        RenderContent(srcCanvas);
        
        using var _ = srcBitmap.DisposableIf(()=> output != srcBitmap);  // 避免内存泄露，如果 output != srcBitmap，则在 using 结束时释放 srcBitmap

        var (currentBitmap, offset) = BitmapFilters.Filter(NBDrawContext.CurrentOrDefault, Bounds, srcBitmap);
        output = currentBitmap;
        return (currentBitmap, offset);
    }

    /// <summary>
    /// 绘制内容。一般子类会重写此方法来绘制自己的内容。默认实现会绘制背景、子元素和装饰物。
    /// </summary>
    /// <param name="context"></param>
    protected virtual void RenderContent(SKCanvas context)
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

    protected void RenderContentWithBitmapFilters(SKCanvas context, NBFrameMask mask)
    {
        var size = new SKSize(Bounds.Width, Bounds.Height);
        if (size.Width <= 0 || size.Height <= 0) return;

        var (currentBitmap, offset) = BuildContentBitmap(context);
        using var srcBitmap = currentBitmap;

        if (srcBitmap == null) return;

        var newBounds = GetExtendBounds(srcBitmap, offset);

        using var maskBitmap = mask.BuildMaskBitmap(NBDrawContext.CurrentOrDefault, newBounds);
        if(maskBitmap == null)
        {
            RenderContentBitmap(context, (srcBitmap, offset));
            return;
        }

        var targetBitmap = new SKBitmap(srcBitmap.Width, srcBitmap.Height);

        using var targetCanvas = new SKCanvas(targetBitmap);
        targetCanvas.Clear(SKColors.Transparent); // 确保目标位图初始透明
        targetCanvas.DrawBitmap(maskBitmap, new SKPoint(0, 0));

        using var p = new SKPaint
        {
            BlendMode = FrameMaskBlendMode,
            IsAntialias = true // 抗锯齿，边缘更平滑
        };

        // 绘制遮罩位图（尺寸和目标图一致，保证覆盖）
        targetCanvas.DrawBitmap(srcBitmap, new SKPoint(0, 0), p);

        RenderContentBitmap(context, (targetBitmap, offset));
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

    public T? As<T>() where T : NBVisual
    {
        return this as T;
    }

    public NBLayoutable? AsLayoutable()
    {
        return this as NBLayoutable;
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

    public static T ClearOnFrames<T>(this T widget) where T : NBVisual
    {
        widget.OnFrameUpdated = null;
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

    public static T RenderTransform<T>(this T widget, SKMatrix? m = null) where T : NBVisual
    {
        widget.RenderTransform = m;
        return widget;  
    }

    public static T ClipPath<T>(this T widget, SKPath? path = null) where T : NBVisual
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

    public static T Filters<T>(this T widget, params SKImageFilter?[] filters) where T : NBVisual
    {
        widget.Filters.ClearFilters();

        if (filters == null || filters.Length == 0)
        {
        }
        else if (filters.Length == 1)
        {
            widget.Filters.AddFilter(new NBSimpleImageFilter(filters[0]));
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
                widget.Filters.AddFilter(new NBSimpleImageFilter(list[0]));
            }
            else if (list.Count > 1)
            {
                var f0 = list[0];
                for(int i = 1; i < list.Count; i++)
                {
                    // Compose filters in order, the last filter is applied first
                    f0 = SKImageFilter.CreateCompose(list[i], f0);
                }
                widget.Filters.AddFilter(new NBSimpleImageFilter(f0));
            }
        }
        return widget;
    }

    public static T Filters<T>(this T widget, params NBImageFilter?[] filters) where T : NBVisual
    {
        widget.Filters.ClearFilters();

        if (filters != null)
        {
            foreach (var item in filters)
            {
                if (item != null)
                {
                    widget.Filters.AddFilter(item);
                }
            }
        }
        return widget;
    }

    public static T ColorFilters<T>(this T widget, params SKColorFilter?[] filters) where T : NBVisual
    {
        widget.ColorFilters.ClearFilters();

        if (filters == null || filters.Length == 0)
        {
        }
        else if (filters.Length == 1)
        {
            widget.ColorFilters.AddFilter(new NBSimpleColorFilter(filters[0]));
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
                widget.ColorFilters.AddFilter(new NBSimpleColorFilter(list[0]));
            }
            else if (list.Count > 1)
            {
                var f0 = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    // Compose color filters in order, the last filter is applied first
                    f0 = SKColorFilter.CreateCompose(list[i], f0);
                }
                widget.ColorFilters.AddFilter(new NBSimpleColorFilter(f0));
            }
        }
        return widget;
    }

    public static T ColorFilters<T>(this T widget, params NBColorFilter?[] filters) where T : NBVisual
    {
        widget.ColorFilters.ClearFilters();

        if (filters != null)
        {
            foreach (var item in filters)
            {
                if (item != null)
                {
                    widget.ColorFilters.AddFilter(item);
                }
            }
        }
        return widget;
    }

    public static T Shaders<T>(this T widget, params Func<SKRect, SKShader>?[] shaderFuncs) where T : NBVisual
    {
        widget.Shaders.ClearShaders();

        if (shaderFuncs == null || shaderFuncs.Length == 0)
        {
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

    public static T Shaders<T>(this T widget, params NBShader?[] shaders) where T : NBVisual
    {
        widget.Shaders.ClearShaders();

        if (shaders != null)
        {
            foreach (var item in shaders)
            {
                if (item != null)
                {
                    widget.Shaders.AddShader(item);
                }
            }
        }
        return widget;
    }

    public static T Styles<T>(this T t, params Action<NBVisual?>[]? styles) where T : NBVisual
    {
        if (styles != null)
        {
            foreach (var style in styles)
            {
                style?.Invoke(t);
            }
        }
        return t;
    }

    public static T FrameMask<T>(this T widget, NBFrameMask? mask) where T : NBVisual
    {
        widget.FrameMask = mask;
        return widget;
    }

    public static T FrameMaskBlend<T>(this T widget, SKBlendMode blendMode) where T : NBVisual
    {
        widget.FrameMaskBlendMode = blendMode;
        return widget;
    }

    public static T BitmapFilters<T>(this T widget, params NBBitmapFilter?[] filters) where T : NBVisual
    {
        widget.BitmapFilters.ClearFilters();
        if (filters != null)
        {
            foreach (var item in filters)
            {
                if (item != null)
                {
                    widget.BitmapFilters.AddFilter(item);
                }
            }
        }
        return widget;
    }
}