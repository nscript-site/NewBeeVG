# 排版与布局

## 1. 容器一览

| 方法 | 创建 | 说明 |
| --- | --- | --- |
| `Grid(childs, rowDef, colDef)` | `NBGrid` | 通用网格 |
| `HGrid(colDef, childs)` | `NBGrid` | 单行、多列 |
| `VGrid(rowDef, childs)` | `NBGrid` | 单列、多行 |
| `HStack(childs)` | `NBStack` | 水平堆叠 |
| `VStack(childs)` | `NBStack` | 垂直堆叠 |
| `Panel(childs)` | `NBPanel` | 层叠，后绘制的在上层 |
| `WrapPanel(childs, isHorizontal)` | `NBWrapPanel` | 空间不足时换行 |

原型：

```csharp
public static NBGrid Grid(NBVisual[]? childs = null, string? rowDef = null, string? colDef = null);
public static NBGrid HGrid(string colDef, NBVisual?[]? childs);
public static NBGrid VGrid(string rowDef, NBVisual?[]? childs);

public static NBStack HStack(NBVisual[]? childs);
public static NBStack VStack(NBVisual[]? childs);

public static NBPanel Panel(NBVisual[]? childs = null);
public static NBWrapPanel WrapPanel(NBVisual[]? childs = null, bool isHorizontal = true);
```

## 2. Grid 尺寸字符串

示例：`"*,200,0.5*,*,2*,Auto"` 表示 6 列：

| 项 | 含义 |
| --- | --- |
| `*` | `1*`，按权重分配剩余空间 |
| `200` | 固定 200 像素 |
| `0.5*` | 权重 0.5 |
| `2*` | 权重 2 |
| `Auto` | 由内容（该行/列最大子元素）决定 |

- 子元素按顺序对应列/行；
- 元素不足时用 `null` 补齐；
- 元素超出定义数量时，多余元素被忽略；
- `null` 子元素仍占一个位置。

## 3. Align

```csharp
public static TCtrl Align<TCtrl>(this TCtrl ctrl,
    int? hAlign = null, int? vAlign = null)
    where TCtrl : NBLayoutable;
```

| 取值 | 水平方向 | 垂直方向 |
| --- | --- | --- |
| `null` | 拉伸填满 | 拉伸填满 |
| `< 0` | 左对齐 | 上对齐 |
| `0` | 居中 | 居中 |
| `> 0` | 右对齐 | 下对齐 |

组合示例：

```csharp
.Align(0, 0)       // 居中
.Align(-1, -1)     // 左上
.Align(1, -1)      // 右上
.Align(1, 1)       // 右下
.Align(null, 0)    // 横向拉伸，垂直居中
.Align(0, null)    // 水平居中，纵向拉伸
.Align(null, null) // 双向拉伸
```

注意：`null` 不是“默认居中”，而是拉伸；需要居中必须显式写 `0`。

## 4. 完整排版示例

```csharp
#!/usr/bin/env dotnet

HGrid("*,*,*,*,Auto", [
    Rect(200, 200, fill: SKColors.Green).Align(0, 0),
    Rect(200, 200, fill: SKColors.Green).Align(-1, -1),
    Rect(200, 200, fill: SKColors.Green).Align(null, 0),
    Rect(0, 0, fill: SKColors.Green).Align(null, null),
    Rect(200, 200, fill: SKColors.Green).Align(0, -1),
])
.Margin(10)
.Background(SKColors.LightCoral)
.AsClip(out var hgrid, 30, name: "hgrid");

VGrid("*,*,*,*,Auto", [
    Rect(200, 200, fill: SKColors.Green).Align(0, 0),
    Rect(200, 200, fill: SKColors.Green).Align(-1, -1),
    Rect(200, 200, fill: SKColors.Green).Align(0, null),
    Rect(0, 0, fill: SKColors.Green).Align(null, null),
    Rect(200, 200, fill: SKColors.Green).Align(0, -1),
])
.Margin(10)
.Background(SKColors.LightCoral)
.AsClip(out var vgrid, 30, name: "vgrid");

HStack([
    Rect(200, 200, fill: SKColors.Green).Align(0, -1),
    Rect(200, 200, fill: SKColors.Green).Align(0, null),
])
.Spacing(20)
.Background(SKColors.LightCoral)
.AsClip(out var hstack, 30, name: "hstack");

VStack([
    Rect(200, 200, fill: SKColors.Green).Align(0, -1),
    Rect(200, 200, fill: SKColors.Green).Align(null, 1),
])
.Spacing(20)
.Background(SKColors.LightCoral)
.AsClip(out var vstack, 30, name: "vstack");

Panel([
    Rect(400, 400, fill: SKColors.Red).Align(-1, -1),
    Rect(200, 200, fill: SKColors.Green).Align(-1, -1),
    Rect(200, 200, fill: SKColors.Blue).Align(1, -1),
    Rect(200, 200, fill: SKColors.Bisque).Align(null, 1),
])
.AsClip(out var panel, 30, name: "panel");

WrapPanel([
    Rect(200, 200, fill: SKColors.Green),
    Rect(200, 200, fill: SKColors.Green),
    Rect(200, 200, fill: SKColors.Green),
    Rect(200, 200, fill: SKColors.Green),
    Rect(200, 200, fill: SKColors.Green),
    Rect(200, 200, fill: SKColors.Green),
])
.Align(0, 0)
.AsClip(out var wpanel, 30, name: "wpanel");

run(stage(1920, 1080, bg: SKColors.Orange),
    [hgrid, vgrid, hstack, vstack, panel, wpanel]);
```

## 5. 常用布局模式

### 全屏背景 + 内容

```csharp
Panel([
    Rect().Align(null, null).Background(SKColors.Black),  // 铺满
    VStack([
        TextBlock("标题").FontSize(120).Align(0, 0),
    ]).Align(0, 0),
]);
```

### 页眉 / 内容 / 页脚

```csharp
VGrid("Auto,*,Auto", [
    TextBlock("Header").Align(-1, -1).Margin(40),
    VStack([...]).Align(0, 0),
    TextBlock("Footer").Align(1, 1).Margin(40),
]);
```

### 左右分栏，内容自适应

```csharp
HGrid("400,*,300", [
    TextBlock("左侧固定 400"),
    VStack([...]),                 // 中间剩余宽度
    TextBlock("右侧固定 300"),
]);
```

### 等宽双栏（两个 SkiaAnim）

```csharp
HGrid("*,*", [
    SkiaAnim(anim1),
    SkiaAnim(anim2),
]);
```

## 6. 尺寸与间距

- `.Margin(left, top, right, bottom)`；
- `.Padding(...)`；
- `.Spacing(n)`：Stack 子元素间距；
- `.Width(...)` / `.Height(...)` / `.Size(w, h)`；
- `.MaxHeight(...)` / `.MaxWidth(...)`；
- `.Background(SKColor)`；
- `.BorderBrush(...)` / `.BorderThickness(...)` / `.CornerRadius(...)`；
- `Rect(width, height, fill, cornerRadius)`。

数值单位是设备独立像素（DIP），与 `stage` 的宽高使用同一坐标系。
