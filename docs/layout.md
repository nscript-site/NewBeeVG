# 排版布局

## 排版示例

排版布局示例 [layout.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/layout.cs): 

```csharp
#!/usr/bin/env dotnet

HGrid("*,*,*,*,Auto", [
    Rect(200,200,fill:SKColors.Green).Align(0,0),
    Rect(200,200,fill:SKColors.Green).Align(-1,-1),
    Rect(200,200,fill:SKColors.Green).Align(null,0),
    Rect(0,0,fill:SKColors.Green).Align(null,null),
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
])
.Margin(10)
.Background(SKColors.LightCoral)
.AsClip(out var hgrid, 30, name:"hgrid");

VGrid("*,*,*,*,Auto", [
    Rect(200,200,fill:SKColors.Green).Align(0,0),
    Rect(200,200,fill:SKColors.Green).Align(-1,-1),
    Rect(200,200,fill:SKColors.Green).Align(0,null),
    Rect(0,0,fill:SKColors.Green).Align(null,null),
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
])
.Margin(10)
.Background(SKColors.LightCoral)
.AsClip(out var vgrid, 30, name:"vgrid");

HStack([
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
    Rect(200,200,fill:SKColors.Green).Align(0,null),
])
.Spacing(20)
.Background(SKColors.LightCoral)
.AsClip(out var hstack, 30, name:"hstack");

VStack([
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
    Rect(200,200,fill:SKColors.Green).Align(null,1),
])
.Spacing(20)
.Background(SKColors.LightCoral)
.AsClip(out var vstack, 30, name:"vstack");

Panel([
    Rect(400,400,fill:SKColors.Red).Align(-1,-1),
    Rect(200,200,fill:SKColors.Green).Align(-1,-1),    
    Rect(200,200,fill:SKColors.Blue).Align(1,-1),    
    Rect(200,200,fill:SKColors.Bisque).Align(null,1),    
])
.AsClip(out var panel, 30, name:"panel");

WrapPanel([
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
]).Align(0,0)
.AsClip(out var wpanel, 30, name:"wpanel");

run(stage(1920, 1080, bg: SKColors.Orange), [hgrid, vgrid, hstack, vstack, panel, wpanel]);
```

## Align

Align 扩展方法，可以设置组件的水平及垂直对齐行为。其原型如下:

```csharp
public static TCtrl Align<TCtrl>(this TCtrl ctrl, int? hAlign = null, int? vAlign = null) where TCtrl : NBLayoutable;
```

各种取值的对齐行为：

- null: 拉伸填充到给定空间;
- 小于 0: 左对齐或上对齐
- 0: 居中对齐
- 大于 0: 右对齐或下对齐

## Layer

`NBLayer` 是带蒙版的层。通过 `Layer` 扩展方法，可以创建 ``NBLayer`，原型如下:

```csharp
public static NBLayer Layer(NBVisual?[]? childs = null);
```

其中，传入的 childs 如果为 2 个元素，则第一个元素为内容，第二个元素为蒙版。

参考示例 [layer.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/layer.cs):

```csharp
#!/usr/bin/env dotnet

Layer([
    TextBlock("输入你的文字").Font(120, SKColors.Black).Align(0,0).Id("Text"),
    Rect().Bind("Text")
    .OnFrame(e=>e.Sender.Shaders(Shaders.AlphaLinearGradient(e.p)))
])
.Align(0, 0)
.AsClip(out var clip1, frames: 40, name: "layer");

run(stage(1920, 1080, bg: SKColors.White), [clip1]);
```

`NBLayer` 支持 Bind 定位。上面例子中，TextBlock 由于尺寸是运行时计算的，Rect 通过设置 Bind 到 TextBlock 的 Id，则会在运行时，将 Rect 的 Bounds 绑定到 TextBlock 的 Bounds，和 TextBlock 对齐位置。