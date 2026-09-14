# 使用特效

## NBVisual 的渲染流程

NBVisual 是 NewBeeVG 中的基础可视元素类，提供了基本的渲染、布局和事件处理功能。它可以包含子元素，并支持各种视觉效果，如滤镜、颜色滤镜、着色器和位图滤镜。

NBVisua 的生命周期：
- 先调用 OnFrameUpdate 事件处理器（如果有），用于更新元素的状态。
- 再进行布局和排列
- 再进行渲染，渲染顺序为：Content -> Bitmap Filters -> FrameMask -> Filters/ColorFilters/Opacity/RenderTransform。

详细渲染流程：
- 先渲染 Content（包括背景、子元素和装饰物），子类可通过重写 RenderContent 方法来实现自定义内容的渲染。如果设置了 Shader，则直接使用 Shader 作为渲染 Content。
- 如果设置了 Bitmap Filters， 应用 Bitmap Filters, 如果存在 BitmapFilters，则会先将 Content 渲染到一个临时位图上，然后对该位图应用 BitmapFilters，最后将处理后的位图绘制到目标画布上。有的 BitmapFilter 可能会改变位图的大小，因此最终绘制的 Bounds (这里称之为 ExtendBounds ) 可能和布局安排的 Bounds 不一致。
- 如果存在 FrameMask，则会将上述流程得到的 Content 渲染到一个临时位图上，然后将 FrameMask 的遮罩位图绘制到该位图上，最后将处理后的位图绘制到目标画布上。FrameMask 构建 Mask 时输入的 rect 是 ExtendBounds。
- 最后应用滤镜、颜色滤镜 和 Opacity、RenderTransform，得到最终的内容。

通过综合应用 Shaders, Bitmap Filters, FrameMask, Filters, ColorFilters, Opacity/RenderTransform，可以实现各种特效。

## Shader

## BitmapFilters

BitmapFilters 需要继承自 NBBitmapFilter，该类定义如下:

```csharp
/// <summary>
/// Bitmap Filter 的基类。
/// </summary>
public abstract class NBBitmapFilter
{
    /// <summary>
    /// 对给定的位图进行滤镜处理。如果生成的图的大小和原图不一致，则返回的偏移点表示新图相对于
    /// 原图的偏移量。如果一致，则偏移点为 (0, 0)。
    /// </summary>
    /// <param name="ctx">绘图上下文。</param>
    /// <param name="rect">要处理的矩形区域。</param>
    /// <param name="bitmap">要处理的位图。</param>
    /// <returns>返回处理后的位图和偏移点。</returns>
    public abstract (SKBitmap?, SKPoint) Filter(NBDrawContext ctx, SKRect rect, SKBitmap? bitmap);
}
```

可以设置多个 BitmapFilters，来实现自定义特效。 NBVisual 会将渲染出来的 Content SKBitmap，交给 BitmapFilters 处理，将生成的 SKBitmap 传入后续渲染流程。

示例如下, [bmpfilter.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/bmpfilter.cs) 通过 DemoBitmapFilter 在每帧图像上绘制帧编号:

```csharp
#!/usr/bin/env dotnet

VStack([
    TextBlock("Code").Font(80, SKColors.Orange).Align(0,0),
    TypstFile("./typst/code1.typ")
    .MaxHeight(800).Align(0,0)
]).BitmapFilters(new DemoBitmapFilter()).Align(0, 0).AsClip(out var clip1, frames: 40, name: "code");

run(stage(1920, 1080, bg: SKColors.White), [clip1]);

internal class DemoBitmapFilter : NBBitmapFilter
{
    public override (SKBitmap?, SKPoint) Filter(NBDrawContext ctx, SKRect rect, SKBitmap? bitmap)
    {
        if(bitmap == null) return (null, new SKPoint(0, 0));
        var c = new SKCanvas(bitmap);
        c.DrawText((ctx.frame + 1).ToString(), 50, 50, new SKFont(SKTypeface.Default, 40), new SKPaint() { Color = SKColors.Red });
        return (bitmap, new SKPoint(0, 0));
    }
}
```