# 使用特效

## NBVisual 的渲染流程

NBVisual 是 NewBeeVG 中的基础可视元素类，提供了基本的渲染、布局和事件处理功能。它可以包含子元素，并支持各种视觉效果，如滤镜、颜色滤镜、着色器和位图滤镜。

NBVisual 的生命周期：
- 先调用 OnFrameUpdate 事件处理器（如果有），用于更新元素的状态。
- 再进行布局和排列
- 再进行渲染，渲染顺序为：Content -> Bitmap Filters -> FrameMask -> Filters/ColorFilters/Opacity/RenderTransform。

详细渲染流程：
- 先渲染 Content（包括背景、子元素和装饰物），子类可通过重写 RenderContent 方法来实现自定义内容的渲染。如果设置了 Shader，则直接使用 Shader 作为渲染 Content。
- 如果设置了 Bitmap Filters， 应用 Bitmap Filters, 如果存在 BitmapFilters，则会先将 Content 渲染到一个临时位图上，然后对该位图应用 BitmapFilters，最后将处理后的位图绘制到目标画布上。有的 BitmapFilter 可能会改变位图的大小，因此最终绘制的 Bounds (这里称之为 ExtendBounds ) 可能和布局安排的 Bounds 不一致。
- 如果存在 FrameMask，则会将上述流程得到的 Content 渲染到一个临时位图上，然后将 FrameMask 的遮罩位图绘制到该位图上，最后将处理后的位图绘制到目标画布上。FrameMask 构建 Mask 时输入的 rect 是 ExtendBounds。
- 最后应用滤镜、颜色滤镜 和 Opacity、RenderTransform，得到最终的内容。

通过综合应用 Shaders, Bitmap Filters, FrameMask, Filters, ColorFilters, Opacity/RenderTransform，可以实现各种特效。

## Content

如果未设置 Shader, 则会调用自身的 `RenderContent` 方法及子组件的 `RenderContent` 方法，生成 Content 图像。

## Shader

可通过 `Shaders` 扩展方法为组件设置 Shader，如果设置了 Shader，则直接使用 Shader 生成的图像，作为该组件的 Content，不再调用该组件的原生 Content 渲染逻辑。扩展方法原型: 

```csharp
public static T Shaders<T>(this T widget, params NBShader?[] shaders) where T : NBVisual;
public static T Styles<T>(this T t, params Action<NBVisual>?[] styles) where T : NBVisual;
```

其中，`NBShader` 的原型为:

```csharp
public abstract class NBShader
{
    public abstract SKShader? CreateShader(NBDrawContext ctx, SKRect rect);
    ......
}
```

[Shaders.cs](https://github.com/nscript-site/NewBeeVG/blob/main/sources/NewBeeVG/Core/Effects/Shaders.cs) 类下，封装了常用的 Shaders:

```csharp
public static class Shaders
{
    public static NBShader AlphaLinearGradient(double p)
    { 
        return new NBRectAlphaLinearGradientShader([0, 0, 1, 1], [0 - 0.4f, ((float)p - 0.4f) / 0.6f, 0.1f + ((float)p - 0.4f) / 0.6f, 1 + 0.2f]); 
    }

    public static NBShader AlphaLinearGradient(SKPoint start, SKPoint end, float[] alphas, float[] positions)
    {
        Func<SKRect, (SKPoint start, SKPoint end)> func = _ => (start, end);
        return new NBAlphaLinearGradientShader(func,alphas,positions);
    }

    public static NBShader LinearGradientOnRect(SKColor[] colors, float[] positions, NBRectDirection direction = NBRectDirection.LeftToRight, SKShaderTileMode tile = SKShaderTileMode.Clamp)
    {
        return new NBRectLinearGradientShader(colors, positions, direction, tile);
    }

    public static NBShader AlphaLinearGradientOnRect(float[] alphas, float[] positions, NBRectDirection direction = NBRectDirection.LeftToRight, SKShaderTileMode tile = SKShaderTileMode.Clamp)
    {
        return new NBRectAlphaLinearGradientShader(alphas, positions, direction, tile);
    }

    public static NBShader RadialGradientOnRect(SKColor[] colors, float[] positions, SKShaderTileMode tile = SKShaderTileMode.Clamp)
    {
        return new NBRadialGradientShader(r => (r.Center, r.MaxRadius), colors, positions, tile);
    }

    public static NBShader FromTexture(SKBitmap bitmap, SKShaderTileMode tileX = SKShaderTileMode.Clamp, SKShaderTileMode tileY = SKShaderTileMode.Clamp)
    {
        return new NBBitmapShader((ctx, rect) => bitmap, tileX, tileY);
    }

    public static NBShader FromTexture(Func<NBDrawContext, SKRect, SKBitmap> bitmapFunc, SKShaderTileMode tileX = SKShaderTileMode.Clamp, SKShaderTileMode tileY = SKShaderTileMode.Clamp)
    {
        return new NBBitmapShader(bitmapFunc, tileX, tileY);
    }

    public static NBShader FromTexture(Func<SKRect, SKBitmap> bitmapFunc, SKShaderTileMode tileX = SKShaderTileMode.Clamp, SKShaderTileMode tileY = SKShaderTileMode.Clamp)
    {
        return new NBBitmapShader((ctx, rect) => bitmapFunc(rect), tileX, tileY);
    }
}
```

Shader 的示例参考 [shaders.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/shaders.cs):

```csharp
#!/usr/bin/env dotnet

Rect(400,200)
.OnFrame(e=>e.Sender.Shaders(Shaders.LinearGradientOnRect([ SKColors.Red, SKColors.Green, SKColors.Blue],[0, e.pf, 1])))
.Align(0, 0)
.AsClip(out var clip1, frames: 30, name: "animate");

run(stage(1920, 1080, bg: SKColors.White), [clip1]);
```

Shader 的一个重要使用场景，是构建后面文档里的 FrameMask。

## BitmapFilters

可通过 `BitmapFilters` 扩展方法为组件设置 BitmapFilters。扩展方法原型: 

```csharp
public static T BitmapFilters<T>(this T widget, params NBBitmapFilter?[] filters) where T : NBVisual;
```

其中，`NBBitmapFilter` 的原型为:

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

使用示例如下, [bmpfilter.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/bmpfilter.cs) 通过 DemoBitmapFilter 在每帧图像上绘制帧编号:

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

## FrameMask

可通过 `FrameMask` 扩展方法为组件设置 FrameMask，如果设置了 FrameMask，则会将 FrameMask 作为 Content 的蒙版，加入渲染流程。扩展方法原型:  

```csharp
public static T FrameMask<T>(this T widget, NBFrameMask? mask) where T : NBVisual;
```

其中，`NBShader` 的原型为:

```csharp
public abstract class NBFrameMask
{
    public abstract SKBitmap? BuildMaskBitmap(NBDrawContext ctx, SKRect rect);

    public SKBlendMode FrameMaskBlendMode { get; set; } = SKBlendMode.SrcOut;
}
```

其中，`BuildMaskBitmap` 构建蒙版图像, `FrameMaskBlendMode` 设置蒙版的混合模式。

[FrameMasks.cs]((https://github.com/nscript-site/NewBeeVG/blob/main/sources/NewBeeVG/Core/Effects/FrameMasks.cs) 类下，封装了常用的 FrameMask 构建方法。

使用示例参考 [framemasks.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/framemasks.cs):

```csharp
#!/usr/bin/env dotnet

var style1 = (NBVisual v) => {
    v.As<NBText>()?
        .FontSize(80).Foreground(SKColors.Black).Align(0, 0)
        .LetterSpacing(10 + 24).Padding(100);
};

var style2 = (NBVisual v) => {
    v.As<NBText>()?
        .FontSize(80).AddStroke(SKColors.Orange, 12).AddStroke(SKColors.Red, 10)
        .Foreground(SKColors.Black).Align(0, 0)
        .LetterSpacing(10).Padding(100);
};

Panel([
    TextBlock("求关注").Styles(style1),
    TextBlock("求关注").Styles(style2).FrameMask(FrameMasks.RectExpandMask())
])
.AsClip(out var clip1, 30, name: "qiuguanzhu");

run(stage(1920, 1080, bg: SKColors.White), [clip1]);
```

## Filters

可通过 `Filters` 扩展方法为组件设置 Filters。扩展方法原型:  

```csharp
public static T Filters<T>(this T widget, params NBImageFilter?[] filters) where T : NBVisual;
```

其中，`NBImageFilter` 的原型为:

```csharp
public abstract class NBImageFilter
{
    public abstract SKImageFilter? CreateFilter();
}
```

[Filters.cs](https://github.com/nscript-site/NewBeeVG/blob/main/sources/NewBeeVG/Core/Effects/Filters.cs) 类下，封装了常用的 Filters.

使用示例参考 [filters.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/filters.cs):

```csharp
#!/usr/bin/env dotnet

// ===================== 定义所有滤镜 =====================
var filters = new List<(string name, NBImageFilter filter)>();

// 1. 模糊
filters.Add(("Blur", Filters.Blur(5, 5)));

// 2. 投影阴影
filters.Add(("DropShadow", Filters.DropShadow(SKColors.Black, 10, 10, 5, 5)));

// 3. 锐化
filters.Add(("Sharpen", Filters.Sharpen(1.5f)));

// 4. 边缘检测
filters.Add(("EdgeDetect", Filters.EdgeDetect()));

// 5. 浮雕
filters.Add(("Emboss", Filters.Emboss()));

// 6. 膨胀（半径 3）
filters.Add(("Dilate", Filters.Dilate(3, 3)));

// 7. 腐蚀（半径 3）
filters.Add(("Erode", Filters.Erode(3, 3)));

// 8. 偏移（向右下偏移 50px）
filters.Add(("Offset", Filters.Offset(50, 50)));

// 9. 矩阵变换（旋转 15°）
var matrix = SKMatrix.CreateRotationDegrees(15, 300, 150); // 假设图片宽600高300，中心旋转
filters.Add(("MatrixTransform", Filters.MatrixTransform(matrix, SKSamplingOptions.Default)));

// 10. 远距离漫反射光照（光源从左上前方）
filters.Add(("DistantLitDiffuse", Filters.DistantLitDiffuse(
    new SKPoint3(0.5f, -0.5f, 1), SKColors.White, 0.5f, 1.0f)));

// 11. 远距离镜面光照
filters.Add(("DistantLitSpecular", Filters.DistantLitSpecular(
    new SKPoint3(0.5f, -0.5f, 1), SKColors.White, 0.5f, 1.0f, 30f)));

// 12. 点光源漫反射（位置在右上角）
filters.Add(("PointLitDiffuse", Filters.PointLitDiffuse(
    new SKPoint3(500, 0, 200), SKColors.White, 0.5f, 1.0f)));

// 13. 点光源镜面反射
filters.Add(("PointLitSpecular", Filters.PointLitSpecular(
    new SKPoint3(500, 0, 200), SKColors.White, 0.5f, 1.0f, 30f)));

// 14. 聚光灯漫反射（位置在左上，目标中心）
filters.Add(("SpotLitDiffuse", Filters.SpotLitDiffuse(
    new SKPoint3(0, 0, 300), new SKPoint3(300, 150, 0),
    20f, (float)(Math.PI / 4), SKColors.White, 0.5f, 1.0f)));

// 15. 聚光灯镜面反射
filters.Add(("SpotLitSpecular", Filters.SpotLitSpecular(
    new SKPoint3(0, 0, 300), new SKPoint3(300, 150, 0),
    20f, (float)(Math.PI / 4), SKColors.White, 0.5f, 1.0f, 30f)));

// 16. 放大镜（在图像中央区域放大2倍）
var lensRect = SKRect.Create(150, 50, 300, 200); // 假设图片600x300
filters.Add(("Magnifier", Filters.Magnifier(lensRect, 2.0f, 20f, SKSamplingOptions.Default)));

// 17. 平铺（将左上角1/4区域平铺到整个图像）
var src = SKRect.Create(0, 0, 300, 150);
var dst = SKRect.Create(0, 0, 600, 300);
var identity = Filters.Blur(0, 0);
filters.Add(("Tile", Filters.Tile(src, dst, identity)));

// 18. 颜色滤镜（使用 Lighting 增加红色调）
var colorFilter = SKColorFilter.CreateLighting(SKColors.White, SKColors.Red);
filters.Add(("ColorFilter", Filters.ColorFilter(colorFilter)));

// 19. 内发光（白色，模糊半径10）
filters.Add(("InnerGlow", Filters.InnerGlow(SKColors.White, 10, 0)));

NBDrawingClip[] BuildClips()
{
    var list = new List<NBDrawingClip>();
    foreach (var filter in filters)
    {
        Console.WriteLine(filter.Item1);
        var image = Image("./assets/snows.jpg")
                            .Size(600, 300)
                            .Stretch(Stretch.Fill)
                            .Align(0, 0);

        var text = TextBlock("Text").Font(64, SKColors.Black).Align(0, 0);

        var panel = VStack([
            TextBlock(filter.name).Font(40, SKColors.White).Margin(50,50,0,0),
            image,
            text
        ]).Filters(filter.filter);

        panel.AsClip(out var clip, frames: 10, name: filter.name);
        list.Add(clip);
    }
    return list.ToArray();
}

run(stage(1920, 1080, bg: SKColors.Orange), BuildClips());
```


## ColorFilters

可通过 `ColorFilters` 扩展方法为组件设置 ColorFilters。扩展方法原型:  

```csharp
public static T ColorFilters<T>(this T widget, params NBColorFilter?[] filters) where T : NBVisual;
```

其中，`NBColorFilter` 的原型为:

```csharp
public abstract class NBColorFilter
{
    public abstract SKColorFilter? CreateFilter();
}
```

[ColorFilters.cs](https://github.com/nscript-site/NewBeeVG/blob/main/sources/NewBeeVG/Core/Effects/ColorFilters.cs) 类下，封装了常用的 ColorFilters.

使用示例参考 [colorfilters.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/colorfilters.cs):

```csharp
#!/usr/bin/env dotnet

// ===================== 定义所有颜色滤镜 =====================
var filters = new List<(string name, NBColorFilter filter)>();

// 1. 灰度
filters.Add(("Gray", ColorFilters.Gray()));

// 2. 亮度 +0.3（变亮）
filters.Add(("Brightness +0.3", ColorFilters.Brightness(0.3f)));

// 3. 亮度 -0.3（变暗）
filters.Add(("Brightness -0.3", ColorFilters.Brightness(-0.3f)));

// 4. 对比度 1.5（增强对比度）
filters.Add(("Contrast 1.5", ColorFilters.Contrast(1.5f)));

// 5. 对比度 0.5（降低对比度）
filters.Add(("Contrast 0.5", ColorFilters.Contrast(0.5f)));

// 6. 亮度+0.2 对比度1.3（组合）
filters.Add(("BrightnessContrast", ColorFilters.BrightnessContrast(0.2f, 1.3f)));

// 7. 饱和度 0（完全灰度）
filters.Add(("Saturation 0", ColorFilters.Saturation(0f)));

// 8. 饱和度 2.0（高饱和）
filters.Add(("Saturation 2.0", ColorFilters.Saturation(2.0f)));

// 9. 色相旋转 90°
filters.Add(("HueRotation 90°", ColorFilters.HueRotation(90f)));

// 10. 色相旋转 180°
filters.Add(("HueRotation 180°", ColorFilters.HueRotation(180f)));

// 11. 颜色平衡：红增1.5，绿1.0，蓝0.5（偏暖）
filters.Add(("ColorBalance Warm", ColorFilters.ColorBalance(1.5f, 1.0f, 0.5f)));

// 12. 颜色平衡：红0.5，绿1.0，蓝1.5（偏冷）
filters.Add(("ColorBalance Cool", ColorFilters.ColorBalance(0.5f, 1.0f, 1.5f)));

// 13. 色调（Tint）红色，强度0.5
filters.Add(("Tint Red 0.5", ColorFilters.Tint(SKColors.Red, 0.5f)));

// 14. 色调（Tint）蓝色，强度0.8
filters.Add(("Tint Blue 0.8", ColorFilters.Tint(SKColors.Blue, 0.8f)));

// 15. 反转（负片）
filters.Add(("Invert", ColorFilters.Invert()));

// 16. 复古 Sepia（完全）
filters.Add(("Sepia 1.0", ColorFilters.Sepia(1.0f)));

// 17. 复古 Sepia（强度0.5，混合原图）
filters.Add(("Sepia 0.5", ColorFilters.Sepia(0.5f)));

// 18. 曝光 +1 EV（变亮）
filters.Add(("Exposure +1", ColorFilters.Exposure(1.0f)));

// 19. 曝光 -1 EV（变暗）
filters.Add(("Exposure -1", ColorFilters.Exposure(-1.0f)));

// 20. Gamma 0.8（变亮）
filters.Add(("Gamma 0.8", ColorFilters.Gamma(0.8f)));

// 21. Gamma 2.2（变暗）
filters.Add(("Gamma 2.2", ColorFilters.Gamma(2.2f)));

// 22. 颜色叠加：Multiply 蓝色
filters.Add(("Overlay Blue (Multiply)", ColorFilters.ColorOverlay(SKColors.Blue, SKBlendMode.Multiply)));

// 23. 颜色叠加：Screen 红色
filters.Add(("Overlay Red (Screen)", ColorFilters.ColorOverlay(SKColors.Red, SKBlendMode.Screen)));

// 24. 光照：乘法 (1.2,0.8,0.6) 加法 (0.1,0,0)
var mul = new SKColor((byte)(0.2f * 255), (byte)(0.8f * 255), (byte)(0.6f * 255));
var add = new SKColor(25, 0, 0);
filters.Add(("Lighting", ColorFilters.Lighting(mul, add)));

// 25. 色调分离（Posterize）4级
filters.Add(("Posterize 4", ColorFilters.Posterize(4)));

// 26. 色调分离（Posterize）8级
filters.Add(("Posterize 8", ColorFilters.Posterize(8)));

// 27. 黑白（高对比度）
filters.Add(("BlackAndWhite", ColorFilters.BlackAndWhite()));

// 28. 自定义 Table（反转亮度，相当于底片效果）
byte[] invertTable = new byte[256];
for (int i = 0; i < 256; i++) invertTable[i] = (byte)(255 - i);
filters.Add(("Table Invert", ColorFilters.Table(invertTable)));

// 29. 自定义 Table（增加对比度曲线）
byte[] contrastTable = new byte[256];
for (int i = 0; i < 256; i++)
{
    float v = i / 255f;
    v = (v - 0.5f) * 1.5f + 0.5f;
    v = Math.Clamp(v, 0f, 1f);
    contrastTable[i] = (byte)(v * 255);
}
filters.Add(("Table Contrast", ColorFilters.Table(contrastTable)));

// ===================== 构建预览 Clips =====================
NBDrawingClip[] BuildClips()
{
    var list = new List<NBDrawingClip>();
    foreach (var (name, filter) in filters)
    {
        var image = Image("./assets/snows.jpg")
            .Size(600, 300)
            .ColorFilters(filter)   // 应用颜色滤镜
            .Stretch(Stretch.Fill)
            .Align(0, 0);

        var text = TextBlock("Text").Font(64, SKColors.Black).ColorFilters(filter).Align(0, 0);

        var panel = VStack([
            TextBlock(name).Font(40, SKColors.White).Margin(50,50,0,0),
            image,
            text
        ]);

        panel.AsClip(out var clip, frames: 20, name: name);
        list.Add(clip);
    }
    return list.ToArray();
}

run(stage(1920, 1080, bg: SKColors.Orange), BuildClips());
```

## Opacity/RenderTransform

可以通过 `Opacity` 和 `RenderTransform` 等扩展方法来设置 `NBVisual` 的透明度及渲染矩阵变换。

```csharp
public static T Opacity<T>(this T widget, double opacity) where T : NBVisual;
public static T RenderTransform<T>(this T widget, SKMatrix? m = null) where T : NBVisual;
```

使用示例参考 [transforms.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/transforms.cs):

```csharp
#!/usr/bin/env dotnet

NBVisual Build(string name, Action<NBVisual> opacity, Action<NBVisual> transform)
{
    Panel([
        TextBlock(name).Align(0,0).Margin(20).Styles(opacity, transform)
    ]).Margin(200)
    .Background(SKColors.Red).Id("panel").Styles(transform).Ref(out var content);
    return content;
}

var s_opacity = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.Opacity(0.5 + 0.5 * v);
    });
};

var s_scale = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.RenderTransform(SKMatrix.CreateScale(1 + v, 1 + v));
    });
};

var s_translate = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.RenderTransform(SKMatrix.CreateTranslation(0, v * 100));
    });
};

var s_rotation = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.RenderTransform(SKMatrix.CreateRotation(v));
    });
};

Build("scale", s_opacity, s_scale).AsClip(out var clip1, 30, name: "scale");
Build("translate", s_opacity, s_translate).AsClip(out var clip2, 30, name: "translate");
Build("rotation", s_opacity, s_rotation).AsClip(out var clip3, 30, name: "rotation");

run(stage(bg: SKColors.Orange), [clip1,clip2,clip3]);
```