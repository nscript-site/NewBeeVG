# 组件目录

> 不包含已废弃的 `NBLayer`。需要层叠效果时用 `Panel`，需要遮罩时用 `FrameMask`。

## 1. 布局组件

### Grid / HGrid / VGrid

```csharp
public static NBGrid Grid(NBVisual[]? childs = null, string? rowDef = null, string? colDef = null);
public static NBGrid HGrid(string colDef, NBVisual?[]? childs);
public static NBGrid VGrid(string rowDef, NBVisual?[]? childs);
```

```csharp
HGrid("*,200,0.5*,*,2*,Auto", [
    null,
    Rect(0, 100, SKColors.Green, 20).Align(null, 0),
    Rect(0, 100, SKColors.Red, 20).Align(null, -1),
    Rect(0, 100, SKColors.Blue, 20).Align(null, 0),
    Rect(0, 100, SKColors.Green, 20).Align(null, 1),
    Rect(100, 100, SKColors.Blue, 20).Align(0, 0),
]).AsClip(30, name: "clip");
```

- 尺寸字符串规则见 `layout.md`；
- 子元素与行列一一对应，可传 `null`；
- 超出定义数量的子元素被忽略。

### HStack / VStack

```csharp
public static NBStack HStack(NBVisual[]? childs);
public static NBStack VStack(NBVisual[]? childs);
```

```csharp
HStack([
    Rect(200, 200, fill: SKColors.Green),
    Rect(200, 200, fill: SKColors.Blue),
]).Spacing(20).Align(0, 0);
```

- `.Spacing(n)` 控制子元素间距；
- Stack 的子元素尺寸可固定，也可由 `Align(null, ...)` 拉伸。

### Panel

```csharp
public static NBPanel Panel(NBVisual[]? childs = null);
```

- 所有子元素叠放在同一区域；
- 后添加的绘制在上层；
- 常用于背景 + 内容 + 装饰的叠加。

```csharp
Panel([
    Rect().Align(null, null).Background(SKColors.Black),
    TextBlock("标题").FontSize(120).Align(0, 0),
]);
```

### WrapPanel

```csharp
public static NBWrapPanel WrapPanel(NBVisual[]? childs = null, bool isHorizontal = true);
```

- 自动换行；
- `isHorizontal = false` 时纵向排列并换列。

## 2. 内容组件

### Image

```csharp
public static NBImage Image(string source, float? width = null, float? height = null);
public static NBImage Image(byte[] data, float? width = null, float? height = null);
public static NBImage Image(SKBitmap source, float? width = null, float? height = null);
public static NBImage Image(string basePath, string fileName, float? width = null, float? height = null);
```

```csharp
Image("./assets/photo.jpg")
    .Size(600, 300)
    .Stretch(Stretch.Fill)
    .Align(0, 0);
```

- `.Size(w, h)` / `.Stretch(...)` 控制尺寸与拉伸方式；
- 路径相对当前工作目录；file-based app 建议使用相对脚本的路径。

### SVG

```csharp
public static NBSvg SVG(Stream stream, float? width = null, float? height = null);
public static NBSvg SVG(string content, float? width = null, float? height = null);
public static NBSvg SVG(string basePath, string fileName, float? width = null, float? height = null);
```

```csharp
SVG("""
<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200">
  <circle cx="100" cy="100" r="80" fill="#FF6B4A" />
</svg>
""").Size(200, 200).Align(0, 0);
```

### Audio

```csharp
public static NBAudioClip AudioFile(string path);
```

```csharp
// 链接到 clip1 的第 0 帧
AudioFile("./Assets/audio01.mp3")
    .AsClip("audio").LinkTo("clip1");

// 链接到 clip1 的第 10 帧
AudioFile("./Assets/audio01.mp3")
    .AsClip("audio").LinkTo("clip1", 10);
```

- 音频片段不绘制画面，只提供音轨；
- `.AsClip("audio")` 负责登记名称；
- `.LinkTo("clip1", offset)` 让音频起点跟随 `clip1` 的起点并加上偏移；
- 目标 clip 必须已创建并登记名称；
- 时长在 `Prepare` 时按音频文件自动计算。

### Lottie

```csharp
public static NBLottie LottieFile(
    string path, float? width = null, float? height = null,
    double speed = 1.0, NBAnimateLoopMode loop = NBAnimateLoopMode.Loop);
```

- 支持 `.json` / `.zip`（以实际实现为准）；
- `speed` 控制播放速度；
- `loop` 可选 `Loop` / `AutoReverse` / 不循环。

### Typst

```csharp
public static NBTypst TypstFile(string path, float? width = null, float? height = null);
public static NBTypstMath TypstMath(string content, float? fontSize = null, int? pageMargin = 10);
public static NBTypstCode TypstCode(string content, int pageWidth = 600, string? lang = null,
    bool showLang = true, float? fontSize = null, int? pageMargin = 10);
```

- `TypstFile`：渲染 `.typ` 文件；
- `TypstMath`：数学公式；
- `TypstCode`：代码高亮块，适合技术视频；
- 支持 `.MaxHeight(...)` 限制尺寸。

### Text / TextBlock / RichText

详见 `texts-and-style.md`。

```csharp
TextBlock("标题", 120, SKColors.White).Align(0, 0);
RichText([
    TextRun("普通", 60, SKColors.Black),
    TextRun("强调", 80, SKColors.Orange),
]);
```

## 3. 形状组件

### Rect

```csharp
public static NBRect Rect(double width = 100, double height = 100,
    SKColor? fill = null, double cornerRadius = 0);
```

```csharp
Rect(400, 200, SKColors.Orange, 20).Align(0, 0);
Rect().Align(null, null).Background(SKColors.Black); // 铺满背景
```

### Ellipse

```csharp
public static NBEllipse Ellipse(double width = 100, double height = 100, SKColor? fill = null);
```

```csharp
Ellipse(300, 300, SKColors.SkyBlue).Align(0, 0);
```

### VecPath

```csharp
public static NBPath VecPath(Action<SKPath> onCreate, SKColor? fill = null, NBBorder? border = null);
public static NBPath VecPath(SKColor? fill = null, NBBorder? border = null);
public static NBPath VecPath(SKPath path, SKColor? fill = null, NBBorder? border = null);
public static NBPath VecPath(SKPath p0, SKPath p1, float t, SKColor? fill = null, NBBorder? border = null);
public static NBPath VecPath(Action<SKPath> onCreateStart, Action<SKPath> onCreateEnd,
    float t, SKColor? fill = null, NBBorder? border = null);

public static SKPath RectPath(float x, float y, float width, float height);
public static SKPath CirclePath(float x, float y, float radius);
```

固定路径：

```csharp
VecPath(CirclePath(100, 100, 80), SKColors.Green).Align(0, 0);
```

路径补间（`t` 为 0..1）：

```csharp
VecPath(
    RectPath(0, 0, 200, 200),
    CirclePath(100, 100, 100),
    e.pf,
    fill: SKColors.Orange
).Align(0, 0);
```

在 `OnFrame` 中动态改路径：

```csharp
VecPath(SKColors.Green).Ref(out var v)
    .OnFrame(e => v.Path(RectPath(0, 0, e.pf * 200, e.pf * 200)))
    .Align(0, 0);
```

## 4. 3D 组件（NewBeeVG.ThreeD）

```csharp
Canvas3D(800, 800, SKColors.White)
    .Camera(PerspectiveCamera(Vec3(10, 12, -25)))
    .Nodes([
        GroundGrid(10, 1, SKColors.DimGray, 1),
        Line3D(Vec3(), Vec3(0, 0, 10), SKColors.Red, 5)
            .OnFrameT(e => e.Sender.End = Vec3(0, 0, 10 * e.pf)),
    ])
    .Align(0, 0);
```

| 方法 / 类型 | 用途 |
| --- | --- |
| `Canvas3D(w, h, bg)` | 3D 画布，可作为普通可视节点参与布局 |
| `PerspectiveCamera(Vec3(...))` | 透视相机 |
| `Vec3(x, y, z)` | 三维点 / 向量 |
| `GroundGrid(size, step, color, thickness)` | 地面网格 |
| `Line3D(from, to, color, thickness)` | 三维线段 |
| `.Ref(out var canvas)` | 取得画布引用，便于 `OnFrame` 中旋转/缩放 |
| `.OnFrame(e => ...)` | 每帧回调，如 `canvas.RotateCamera(...)` |
| `.OnFrameT(e => ...)` | 带进度的逐帧回调，`e.pf` 常用 |

- 3D 节点可自由混入 `HStack` / `VStack` / `HGrid` / `Panel`；
- 与普通组件一样通过 `.AsClip(...)` 进入时间线；
- 复杂的相机 / 节点动画建议配合 `Easing` 使用。

## 5. 组件通用链式方法

| 方法 | 作用 |
| --- | --- |
| `.Align(h, v)` | 对齐 |
| `.Margin(l, t, r, b)` | 外边距 |
| `.Padding(...)` | 内边距 |
| `.Size(w, h)` / `.Width(w)` / `.Height(h)` | 尺寸 |
| `.MaxWidth(w)` / `.MaxHeight(h)` | 最大尺寸 |
| `.Background(color)` | 背景 |
| `.BorderBrush(...)` / `.BorderThickness(...)` / `.CornerRadius(...)` | 边框 |
| `.Opacity(v)` | 透明度 |
| `.RenderTransform(matrix)` | 渲染变换 |
| `.Shaders(...)` / `.BitmapFilters(...)` / `.FrameMask(...)` / `.Filters(...)` / `.ColorFilters(...)` | 特效 |
| `.OnFrame(e => ...)` | 逐帧回调 |
| `.Styles(...)` | 复用样式 |
| `.Ref(out var x)` | 取得实例引用 |
| `.Id("name")` | 设置标识（配合 Bind） |
| `.AsClip(...)` | 转成片段 |
| `.Scrollable()` | 可滚动（预览用） |
