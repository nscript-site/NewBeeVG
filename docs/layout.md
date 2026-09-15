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


