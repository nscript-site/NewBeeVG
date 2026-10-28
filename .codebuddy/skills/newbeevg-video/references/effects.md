# 特效

## 1. 渲染流程

`NBVisual` 每帧顺序：

1. `OnFrameUpdate` 事件（状态更新）；
2. 布局与排列；
3. `Content`（背景、子元素、自身绘制；若设置了 Shader，则直接用 Shader 作为 Content）；
4. `BitmapFilters`（先渲染到临时位图，再逐级处理，可能改变尺寸）；
5. `FrameMask`（把遮罩位图与上一步结果混合）；
6. `Filters` / `ColorFilters` / `Opacity` / `RenderTransform`。

理解顺序很重要：

- 先模糊再裁剪，与先裁剪再模糊结果不同；
- `FrameMask` 的遮罩基于 `ExtendBounds`（BitmapFilter 之后可能变化的边界）；
- 设置 `Shaders` 会替换组件原生 Content，文字/图片本身不再绘制。

## 2. Shaders

```csharp
public static T Shaders<T>(this T widget, params NBShader?[] shaders)
    where T : NBVisual;
```

`NBShader`：

```csharp
public abstract class NBShader
{
    public abstract SKShader? CreateShader(NBDrawContext ctx, SKRect rect);
}
```

`Shaders` 静态类常用方法：

| 方法 | 用途 |
| --- | --- |
| `AlphaLinearGradient(double p)` | 循环移动的透明度渐变（扫光） |
| `AlphaLinearGradient(start, end, alphas, positions)` | 自定义起止点的透明度渐变 |
| `LinearGradientOnRect(colors, positions, direction, tile)` | 矩形内线性渐变 |
| `AlphaLinearGradientOnRect(alphas, positions, direction, tile)` | 矩形内透明度渐变 |
| `RadialGradientOnRect(colors, positions, tile)` | 矩形内径向渐变 |
| `FromTexture(bitmap, tileX, tileY)` | 用位图作为纹理 |
| `FromTexture(func, tileX, tileY)` | 动态生成纹理 |

```csharp
Rect(400, 200)
    .OnFrame(e => e.Sender.Shaders(
        Shaders.LinearGradientOnRect(
            [SKColors.Red, SKColors.Green, SKColors.Blue],
            [0, e.pf, 1])))
    .Align(0, 0)
    .AsClip(30, name: "gradient");
```

## 3. BitmapFilters

```csharp
public static T BitmapFilters<T>(this T widget, params NBBitmapFilter?[] filters)
    where T : NBVisual;
```

自定义滤镜：

```csharp
public abstract class NBBitmapFilter
{
    public abstract (SKBitmap?, SKPoint) Filter(
        NBDrawContext ctx, SKRect rect, SKBitmap? bitmap);
}
```

- 返回处理后的位图；
- 如果尺寸变化，返回相对原图的偏移点；
- 多个滤镜按传入顺序执行；
- 适合在整帧画面上叠加编号、噪点、扫描线等。

```csharp
VStack([...])
    .BitmapFilters(new DemoBitmapFilter())
    .AsClip(40, name: "code");

internal class DemoBitmapFilter : NBBitmapFilter
{
    public override (SKBitmap?, SKPoint) Filter(
        NBDrawContext ctx, SKRect rect, SKBitmap? bitmap)
    {
        if (bitmap == null) return (null, new SKPoint(0, 0));

        using var canvas = new SKCanvas(bitmap);
        using var font = new SKFont(SKTypeface.Default, 40);
        using var paint = new SKPaint { Color = SKColors.Red };
        canvas.DrawText((ctx.frame + 1).ToString(), 50, 50, font, paint);

        return (bitmap, new SKPoint(0, 0));
    }
}
```

## 4. FrameMask

```csharp
public static T FrameMask<T>(this T widget, NBFrameMask? mask)
    where T : NBVisual;
```

```csharp
public abstract class NBFrameMask
{
    public abstract SKBitmap? BuildMaskBitmap(NBDrawContext ctx, SKRect rect);
    public SKBlendMode FrameMaskBlendMode { get; set; } = SKBlendMode.SrcOut;
}
```

`FrameMasks` 类提供常用遮罩构造（如 `RectExpandMask()`）。

```csharp
Panel([
    TextBlock("求关注").Styles(style1),
    TextBlock("求关注").Styles(style2).FrameMask(FrameMasks.RectExpandMask())
]).AsClip(30, name: "qiuguanzhu");
```

- 默认 `SrcOut`：遮罩区域“挖掉”；
- 可通过 `FrameMaskBlendMode` 改为其他混合模式；
- 遮罩本身按帧构建，可做动态扫过、扩散等效果。

## 5. Filters（图像滤镜）

```csharp
public static T Filters<T>(this T widget, params NBImageFilter?[] filters)
    where T : NBVisual;

public abstract class NBImageFilter
{
    public abstract SKImageFilter? CreateFilter();
}
```

`Filters` 类常用：

| 类别 | 方法 |
| --- | --- |
| 模糊 / 锐化 | `Blur`、`Sharpen` |
| 阴影 | `DropShadow` |
| 边缘 / 浮雕 | `EdgeDetect`、`Emboss` |
| 形态学 | `Dilate`、`Erode` |
| 位移 | `Offset` |
| 矩阵 | `MatrixTransform` |
| 光照 | `DistantLitDiffuse`、`DistantLitSpecular`、`PointLitDiffuse`、`PointLitSpecular`、`SpotLitDiffuse`、`SpotLitSpecular` |
| 其他 | `Magnifier`、`Tile`、`ColorFilter`、`InnerGlow`、`BlendMode` |

```csharp
VStack([...])
    .Filters(Filters.DropShadow(SKColors.Black, 10, 10, 5, 5))
    .AsClip(10, name: "shadow");
```

## 6. ColorFilters（调色）

```csharp
public static T ColorFilters<T>(this T widget, params NBColorFilter?[] filters)
    where T : NBVisual;

public abstract class NBColorFilter
{
    public abstract SKColorFilter? CreateFilter();
}
```

`ColorFilters` 类常用：

| 类别 | 方法 |
| --- | --- |
| 灰度 / 黑白 | `Gray`、`BlackAndWhite` |
| 亮度 / 对比度 | `Brightness`、`Contrast`、`BrightnessContrast` |
| 饱和度 / 色相 | `Saturation`、`HueRotation` |
| 颜色平衡 / 色调 | `ColorBalance`、`Tint`、`ColorOverlay` |
| 反相 / 复古 | `Invert`、`Sepia` |
| 曝光 / Gamma | `Exposure`、`Gamma` |
| 其他 | `Posterize`、`Lighting`、`Table` |

```csharp
TextBlock("Text")
    .ColorFilters(ColorFilters.Saturation(0))
    .AsClip(20, name: "gray");
```

## 7. Opacity / RenderTransform

```csharp
public static T Opacity<T>(this T widget, double opacity) where T : NBVisual;
public static T RenderTransform<T>(this T widget, SKMatrix? m = null) where T : NBVisual;
```

```csharp
TextBlock("淡入")
    .OnFrame(e =>
    {
        float p = (float)Easing.SineInOut(e.p);
        e.Sender.Opacity(0.5 + 0.5 * p);
        e.Sender.RenderTransform(SKMatrix.CreateScale(1 + p, 1 + p));
    })
    .AsClip(30, name: "scale");
```

- `RenderTransform` 只影响渲染，不改变布局位置；
- 变换原点默认在画布左上角；围绕中心旋转/缩放时，先平移再变换，或使用 `SKMatrix.CreateRotationDegrees(deg, cx, cy)`；
- 传 `null` 可清除变换。

## 8. 组合建议

- 扫光文字：`Shaders(Shaders.AlphaLinearGradient(e.p))` 或自定透明渐变；
- 打字机 / 逐字出现：`FrameMask` + 逐字宽度；
- 卡片投影：`Filters.DropShadow(...)`；
- 整体色调：`ColorFilters`；
- 动态模糊：每帧改 `Filters.Blur(radius, radius)`；
- **不要使用已废弃的 `NBLayer`**：用 `Panel` + `FrameMask` / `Shaders` / `BitmapFilters` 替代。
