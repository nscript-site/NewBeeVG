# 常用组件

## 布局组件

### Grid

`NBGrid` 和 Avalonia 的 Grid 的逻辑相似，可参考 Avalonia 的 [Grid 文档](https://v11.docs.avaloniaui.net/zh-Hans/docs/reference/controls/grid/)

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

`NBStack` 和 Avalonia 的 StackPanel 的逻辑相似，可参考 Avalonia 的 [StackPanel 文档](https://docs.avaloniaui.net/controls/layout/panels/stackpanel)

### HStack

`HStack` 扩展方法，可以方便的创建水平布局的 `NSStack`:

```csharp
public static NBStack HStack(NBVisual[]? childs)
{
    var stack = new NBStack { Orientation = Orientation.Horizontal};
    if (childs != null)
    {
        stack.Childs(childs);
    }
    return stack;
}
```

### VStack

`VStack` 扩展方法，可以方便的创建垂直布局的 `NSStack`:

```csharp
public static NBStack VStack(NBVisual[]? childs)
{
    var stack = new NBStack { Orientation = Orientation.Vertical };
    if (childs != null)
    {
        stack.Childs(childs);
    }
    return stack;
}
```

### NBPanel

`NBPanel` 和 Avalonia 的 Panel 的逻辑相似，可参考 Avalonia 的 [Panel 文档](https://docs.avaloniaui.net/controls/layout/panels/panel)。

`Panel` 扩展方法，可以方便的创建 `NBPanel`:

```csharp
public static NBPanel Panel(NBVisual[]? childs = null)
{
    var panel = new NBPanel();
    if (childs != null)
    {
        panel.Childs(childs);
    }
    return panel;
}
```

### NBWrapPanel

`NBWrapPanel` 和 Avalonia 的 WrapPanel 的逻辑相似，可参考 Avalonia 的 [WrapPanel 文档](https://docs.avaloniaui.net/controls/layout/panels/wrappanel)。

`WrapPanel` 扩展方法，可以方便的创建 `NBWrapPanel`:

```csharp
public static NBWrapPanel WrapPanel(NBVisual[]? childs = null, bool isHorizontal = true)
{
    var panel = new NBWrapPanel { Orientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical };
    if (childs != null)
    {
        panel.Childs(childs);
    }
    return panel;
}
```

## 内容组件

### NBImage

```csharp
 public static NBImage Image(string source, float? width = null, float? height = null)
 {
     return Image(SKBitmap.Decode(source), width, height);
 }

 public static NBImage Image(byte[] data, float? width = null, float? height = null)
 {
     using var stream = new MemoryStream(data);
     return Image(SKBitmap.Decode(stream), width, height);
 }

 public static NBImage Image(SKBitmap source, float? width = null, float? height = null)
 {
     var img = new NBImage { Source = source };
     if (width.HasValue) img.Width = width.Value;
     if (height.HasValue) img.Height = height.Value;
     return img;
 }

 public static NBImage Image(string basePath, string fileName, float? width = null, float? height = null)
 {
     var img = new NBImage();
     img.TryLoad(
         () => img.Source = SKBitmap.Decode(FileHelpers.LoadStream(basePath, fileName))
     );
     if (width.HasValue) img.Width = width.Value;
     if (height.HasValue) img.Height = height.Value;
     return img;
 }
```

### NBSvg

```csharp
public static NBSvg SVG(Stream stream, float? width = null, float? height = null)
{
    var svg = new NBSvg { SvgStream = stream };
    if (width.HasValue) svg.Width = width.Value;
    if (height.HasValue) svg.Height = height.Value;
    return svg;
}

public static NBSvg SVG(string content, float? width = null, float? height = null)
{
    var svg = new NBSvg();
    svg.SvgContent(content);
    if (width.HasValue) svg.Width = width.Value;
    if (height.HasValue) svg.Height = height.Value;
    return svg;
}

public static NBSvg SVG(string basePath, string fileName, float? width = null, float? height = null)
{
    var svg = new NBSvg();
    
    svg.TryLoad(()=> svg.SvgStream = FileHelpers.LoadStream(basePath, fileName));

    if (width.HasValue) svg.Width = width.Value;
    if (height.HasValue) svg.Height = height.Value;

    return svg;
}
```

### NBText

```csharp
public static NBText Text(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null, bool wrap = true, int textAlign = -1)
{
    return TextBlock(text, fontSize, color, fontFamily, wrap, textAlign);
}

public static NBText TextBlock(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null, bool wrap = true, int textAlign = -1)
{
    var tb = new NBText { Text = text, FontFamily = fontFamily ?? GetDefaultFontFamily(), FontSize = fontSize, Foreground = color ?? SKColors.Black };
    tb.IsWrapText = wrap;
    tb.TextAlign = textAlign.ToNBTextAlign();
    return tb;
}
```

### NBRichText

```csharp
public static NBTextRun TextRun(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null)
{
    var tb = new NBTextRun { Text = text, FontFamily = fontFamily ?? GetDefaultFontFamily(), FontSize = fontSize, Foreground = color ?? SKColors.Black };
    return tb;
}

public static NBRichText RichText(NBTextRun?[]? runs = null, bool wrap = true, int textAlign = -1)
{
    var tb = new NBRichText();
    tb.IsWrapText = wrap;
    tb.TextAlign = textAlign.ToNBTextAlign();
    if(runs != null)
    {
        foreach(var run in runs)
        {
            if (run != null)
            {
                tb.Add(run);
            }
        }
    }
    return tb;
}
```

### NBLayer

```csharp
public static NBLayer Layer(NBVisual?[]? childs = null)
{
    var layer = new NBLayer();
    layer.Align(0, 0);
    if(childs != null)
    {
        if(childs.Length > 0)
        {
            layer.Source = childs[0];
        }

        if(childs.Length > 1)
        {
            layer.Mask = childs[1];
        }
    }
    return layer;
}
```

### NBLottie

```csharp
public static NBLottie LottieFile(string path, float? width = null, float? height = null, double speed = 1.0, NBAnimateLoopMode loop = NBAnimateLoopMode.Loop)
{
    var file = new NBLottie();
    file.LottieFile = path;
    if (width.HasValue) file.Width = width.Value;
    if (height.HasValue) file.Height = height.Value;
    file.Speed = speed;
    file.LoopMode = loop;
    return file;
}

public static NBLottie LottieFile(string basePath, string fileName, float? width = null, float? height = null, double speed = 1.0, NBAnimateLoopMode loop = NBAnimateLoopMode.Loop)
{
    var file = new NBLottie();
    file.LoadFromFile(basePath, fileName);
    if (width.HasValue) file.Width = width.Value;
    if (height.HasValue) file.Height = height.Value;
    file.Speed = speed;
    file.LoopMode = loop;
    return file;
}
```

### NBTypst

```csharp
public static NBTypst TypstFile(string path, float? width = null, float? height = null)
{
    var file = new NBTypst();
    file.TypstFile = path;
    if (width.HasValue) file.Width = width.Value;
    if (height.HasValue) file.Height = height.Value;
    return file;
}
```

### NBTypstMath

```csharp
public static NBTypstMath TypstMath(string content, float? fontSize = null, int? pageMargin = 10)
{
    var file = new NBTypstMath();
    file.TypstContent = content;
    file.FontSize = fontSize;
    if(pageMargin != null)
        file.PageMargin(pageMargin.Value);
    return file;
}
```

### NBTypstCode

```csharp
public static NBTypstCode TypstCode(string content, int pageWidth = 600, string? lang = null, bool showLang = true, float? fontSize = null, int? pageMargin = 10)
{
    var file = new NBTypstCode();
    file.TypstContent = content;
    file.Lang = lang;
    file.ShowLang = showLang;
    file.FontSize = fontSize;
    file.PageWidth = pageWidth;
    if (pageMargin != null)
        file.PageMargin(pageMargin.Value);
    return file;
}
```

## 形状组件

### NBRect

```csharp
public static NBRect Rect(double width = 100, double height = 100, SKColor? fill = null, double cornerRadius = 0)
{
    return new NBRect(width, height) { Fill = fill, CornerRadius = cornerRadius };
}
```

### NBEllipse

```csharp
public static NBEllipse Ellipse(double width = 100, double height = 100, SKColor? fill = null)
{
    return new NBEllipse(width, height) { Fill = fill };
}
```

### NBPath

```csharp
public static NBPath VecPath(Action<SKPath> onCreate, SKColor? fill = null, NBBorder? border = null)
{
    return new NBPath(onCreate, fill, border);
}

public static NBPath VecPath(SKColor? fill = null, NBBorder? border = null)
{
    return new NBPath((SKPath?)null, fill, border);
}

public static NBPath VecPath(SKPath path, SKColor? fill = null, NBBorder? border = null)
{
    return new NBPath(path, fill, border);
}

public static NBPath VecPath(SKPath p0, SKPath p1, float t, SKColor? fill = null, NBBorder? border = null)
{
    return new NBPath(p0, p1, t, fill, border);
}

public static SKPath RectPath(float x, float y, float width, float height)
{
    var p = new SKPath();
    p.AddRect(new SKRect(x, y, x + width, x + height));
    return p;
}

public static SKPath CirclePath(float x, float y, float radius)
{
    var p = new SKPath();
    p.AddCircle(x, y, radius);
    return p;
}

/// <summary>
/// 创建一个插值矢量路径，支持动画
/// </summary>
/// <param name="onCreateStart"></param>
/// <param name="onCreateEnd"></param>
/// <param name="t"></param>
/// <param name="fill"></param>
/// <param name="border"></param>
/// <returns></returns>
public static NBPath VecPath(Action<SKPath> onCreateStart, Action<SKPath> onCreateEnd, float t, SKColor? fill = null, NBBorder? border = null)
{
    return new NBPath(onCreateStart, onCreateEnd, t, fill, border);
}
```
