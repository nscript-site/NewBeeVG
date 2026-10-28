# 文字与样式

## 1. 普通文本：`NBText`

```csharp
public static NBText TextBlock(
    string text = "", float fontSize = 40, SKColor? color = null,
    string? fontFamily = null, bool wrap = true, int textAlign = -1);

public static NBText Text(
    string text = "", float fontSize = 40, SKColor? color = null,
    string? fontFamily = null, bool wrap = true, int textAlign = -1);
```

- `Text` 与 `TextBlock` 等价；
- `color` 默认黑色；
- `wrap` 控制自动换行；
- `textAlign`：`-1` 左、`0` 中、`1` 右。

```csharp
TextBlock("标题", 120, SKColors.White);
Text("副标题", 48, SKColors.LightGray, textAlign: 0);
```

### 链式属性

```csharp
TextBlock("Hello")
    .FontSize(120)
    .Foreground(SKColors.White)
    .LetterSpacing(8)
    .LineSpacing(10)
    .Align(0, 0)
    .Margin(20);
```

- `.Font(size, color)`：同时设置字号与颜色；
- `.FontFamily("字体名")`；
- `.AddStroke(color, width)`：可叠加多层描边；
- `.TextAlign(...)` / `.Wrap(...)`；
- 垂直文本：`.Orientation(Orientation.Vertical)`；
- 通过 `font("字体名")` 设置全局默认字体。

### 多行文字

```csharp
TextBlock("第一行\n第二行", 80, SKColors.Black)
    .LineSpacing(16)
    .Align(0, 0);
```

## 2. 富文本：`NBRichText` + `NBTextRun`

```csharp
public static NBTextRun TextRun(
    string text = "", float fontSize = 40, SKColor? color = null,
    string? fontFamily = null);

public static NBRichText RichText(
    NBTextRun?[]? runs = null, bool wrap = true, int textAlign = -1);
```

```csharp
RichText([
    TextRun("普通文字", 60, SKColors.Black),
    TextRun("强调", 80, SKColors.Orange).AddStroke(SKColors.White, 6),
    TextRun("继续普通", 60, SKColors.Black),
], textAlign: 0);
```

- 每个 run 可有独立字号、颜色、字体、描边、字间距；
- 适合同一行内混排不同样式；
- `RichText` 负责整体布局与换行。

## 3. `Styles`：复用样式

```csharp
public static T Styles<T>(this T t, params Action<NBVisual>?[] styles)
    where T : NBVisual;
```

```csharp
var baseStyle = (NBVisual v) =>
{
    v.As<NBText>()?
        .FontSize(120)
        .Foreground(SKColors.Black)
        .Align(0, 0);
};

var strokeStyle = (NBVisual v) =>
{
    v.As<NBText>()?
        .AddStroke(SKColors.Orange, 20)
        .AddStroke(SKColors.White, 14);
};

TextBlock("求关注").Styles(baseStyle, strokeStyle);
```

- 样式按传入顺序依次执行，后面的可覆盖前面的属性；
- `v.As<NBText>()` 用于取具体控件类型；不是 `NBText` 时返回 `null`；
- 样式只应修改属性，不要在其中创建长期资源。

完整示例（逐字跳动）：

```csharp
var style1 = (NBVisual v) =>
{
    v.As<NBText>()?
        .AddStroke(SKColors.Orange, 55)
        .AddStroke(SKColors.White, 50)
        .AddStroke(SKColors.Red, 40)
        .FontSize(120)
        .Foreground(SKColors.Black)
        .Align(0, 0);
};

var style2 = (NBVisual v) =>
{
    v.As<NBText>()?.OnFrame(e =>
    {
        var txt = e.SenderAs<NBText>()?.Text ?? "";
        var idx = "求关注".IndexOf(txt);
        if (idx < 0) idx = 0;

        var frame = e.frame % 10;
        var animateIdx = e.frame % 30 / 10;

        if (animateIdx == idx)
        {
            float p = (float)Easing.BackIn(frame / 10.0f);
            e.Sender.RenderTransform(SKMatrix.CreateScale(1 + 0.2f * p, 1 + 0.2f * p));
        }
        else
        {
            e.Sender.RenderTransform();
        }
    });
};

HStack([
    TextBlock("求").Styles(style1, style2),
    TextBlock("关").Styles(style1, style2),
    TextBlock("注").Styles(style1, style2),
]).Align(0, 0).Spacing(20)
  .AsClip(30, name: "clip");
```

## 4. 逐帧动画

```csharp
public static T OnFrame<T>(this T widget, Action<NBDrawFrameArgs> handler)
    where T : NBVisual;
```

常用字段：

| 字段 | 含义 |
| --- | --- |
| `e.p` | 当前 clip 内进度，`0..1` |
| `e.pf` | 按帧推进的进度 |
| `e.frame` | 当前帧号（clip 内相对帧） |
| `e.Sender` | 触发事件的 `NBVisual` |
| `e.SenderAs<T>()` | 转成具体控件类型 |
| `e.SenderLayoutable` | 布局对象（用于 Margin 等） |

示例：

```csharp
TextBlock("淡入")
    .Opacity(0)
    .OnFrame(e =>
    {
        float p = (float)Easing.SineInOut(e.p);
        e.Sender.Opacity(p);
        e.Sender.RenderTransform(SKMatrix.CreateTranslation(0, (1 - p) * 80));
    });
```

缓动：`Easing.SineInOut`、`Easing.BackIn`、`Easing.CubicOut` 等，返回 `double`，按需转 `float`。

## 5. 文字布局注意事项

- 文字尺寸由字号与字体决定；容器太小时会换行或溢出，必要时设置 `.MaxWidth(...)`；
- `Align` 负责控件在父容器中的位置，`TextAlign` 负责多行文本内部的对齐；
- 描边会增大文字实际测量尺寸，居中排版时要预留空间；
- 中文字体缺失会显示方块，部署环境需确保字体存在，或用 `font(...)` 指定已安装字体；
- 大量文字逐帧改内容时，尽量复用控件、只改属性。
