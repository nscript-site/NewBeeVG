# Typst 动画

内嵌需要本地安装 [typst](https://typst.app/)，且能正确编译/预览对应的 typ 文件。NewBeeVG 会调用 typst 的编译器，将 typ 文件编译成图像，嵌入到视频中。

> [!NOTE]
> 在 typ 文件中设置 `#set page(fill: none)`，生成的图像的背景为透明色。设置 `#set page(  width: auto, height: auto)` 可以让图像大小适应内容。
> typ 文件中可以通过 `sys.inputs.at` 获取编译参数。传入不同的参数，可生成序列图像。

typ 文件示例如下 ( [page1.typ](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typst/page1.typ) ):

```typst
#import "@preview/cetz:0.3.4": canvas, draw

#set page(fill: none)

#set page(
  width: auto,
  height: auto,
  margin: 10pt,
)

= 标题

#highlight(fill: aqua)[浅蓝高亮]

这是正文。

$ a^2 + b^2 = c^2 $

#let frames = int(sys.inputs.at("frames", default: "0"))

#canvas(length: 20pt, {
  import draw: *

  set-style(stroke: 1pt + blue)
  line((0, 0), (4, 0))
  line((0, 0), (0, 3))
  line((0, 0), (4, 3))
  circle((2, 1.5 ), radius: 0.8 + frames/30.0, fill: aqua.lighten(40%))
})

#image("./assets/snows.jpg", width: 200pt)
```

上面 typst 中，嵌入了 cetz 绘制的图像，绘制时，会根据传入的参数 frames 来改变圆的大小。

调用代码([typst.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typst.cs)):

```csharp
#!/usr/bin/env dotnet

VGrid($"*", [
    TypstFile("./typst/page1.typ").Ref(out var typ)
        .Align(0,0)
        .OnFrame(e=> typ.UpdateInputs("frames", e.frame))
    ]).Background(SKColors.DeepSkyBlue)
    .AsClip(out var clip, 30, name: "typst");

run(stage(bg: SKColors.Orange), [clip]);
```

上面代码中，会将 frames 参数传入 typst 编译器，驱动生成每帧 typst 渲染结果，嵌入到视频中。

`UpdateInputs` 扩展方法也可以一次更新多个参数，方法原型如下:

```csharp
public static T UpdateInputs<T>(this T self, string key, object val) where T : NBTypst
{
    ...
}

public static T UpdateInputs<T>(this T self, params (string, object)[] inputs) where T : NBTypst
{
    ...
}

public static T UpdateInputs<T>(this T self, IList<(string, object)> inputs) where T : NBTypst
{
    ...
}
```