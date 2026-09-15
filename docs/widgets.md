# 常用组件

## 布局组件

### Grid

`NBGrid` 实现了和 Avalonia 的 Grid 的逻辑相似，可参考 Avalonia 的 [Grid 文档](https://v11.docs.avaloniaui.net/zh-Hans/docs/reference/controls/grid/)

`Grid` 扩展方法可以方便的创建 `NBGrid`:

```csharp
public static NBGrid Grid(NBVisual[]? childs = null, string? rowDef = null, string? colDef = null);
```

### HGrid

`HGrid` 扩展方法，可以方便的创建只有一行，但是有多列的 Grid:

```csharp
public static NBGrid HGrid(string colDef, NBVisual?[]? childs);
```

使用示例:

```csharp
#!/usr/bin/env dotnet

HGrid("*,200,0.5*,*,2*,Auto",
[
    null,
    Rect(0,100,SKColors.Green,20).Align(null,0),
    Rect(0,100,SKColors.Red,20).Align(null,-1),
    Rect(0,100,SKColors.Blue,20).Align(null,0),
    Rect(0,100,SKColors.Green,20).Align(null,1),
    Rect(100,100,SKColors.Blue,20).Align(0,0),
]).AsClip(out var clip, 30, name: "clip");

run(stage(1920,1080,bg: SKColors.Orange), [clip]);
```

其中，`"*,200,0.5*,*,2*,Auto"` 定义了 6 列，一共 6 个定义项，顺序依次：

- `*`：星号尺寸，等价 `1*`，权重为 1
- `200`：固定像素尺寸，固定占用 200 设备独立像素
- `0.5*`：星号尺寸，权重 0.5
- `*`：星号尺寸，等价 `1*`，权重为 1
- `2*`：星号尺寸，权重为 2
- `Auto`：内容自适应尺寸，由该行 / 列内部最大子控件大小决定

后面传入的列内容，与上面列定义按照下标进行匹配。传入的列可以为 null，如果超出了定义的列，则多余的列无效。如果不足定义的列，则不足的列用 null 填充。

### VGrid

`VGrid` 扩展方法，可以方便的创建只有一列，但是有多行的 Grid:

```csharp
public static NBGrid VGrid(string rowDef, NBVisual?[]? childs)
```

### NBStack

### HStack

### VStack

### NBPanel

### NBWrapPanel

## 内容组件

### NBImage

### NBSvg

### NBText

### NBRichText

### NBLayer

### NBLottie

### NBTypst

### NBTypstMath

## 形状组件

### NBRect

### NBEllipse

### NBPath