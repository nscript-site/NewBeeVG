# Typst 动画

## 嵌入 Typst 动画

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

VStack([
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

## 直接嵌入数学公式

通过 `TypstMath` 扩展方法，不用写 typ 文件，也可嵌入数学公式。示例([typstmath.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typstmath.cs))如下：

```csharp
#!/usr/bin/env dotnet

var math = """
            $ #t1[A] #t2[=] #t3[pi r^2] $
            $ #text(fill: red)[f(x)] = #text(fill: blue)[x^2] + #text(fill: orange)[2x] + 1 $
            $ "area" = pi dot "radius"^2 $
            $ cal(A) :=
                { x in RR | x "is natural" } $
            #let x = 5
            $ #x < 17 $
            """;
VStack([
    TextBlock("TypstMath 直接嵌入数学公式").Margin(10),
    TypstMath(math).Width(800).Ref(out var tm)
        .OnFrame(e=>{
            SKColor color = SKColors.Red;
            byte alpha1 = 255;
            byte alpha2 = (byte)(e.frame < 10 ? 0 : 255);
            byte alpha3 = (byte)(e.frame < 20 ? 0 : 255);
            tm.Seg("t1",color,alpha1);
            tm.Seg("t2",color,alpha2);
            tm.Seg("t3",color,alpha3);
        })
        .Align(0,-1).Margin(100)
]).Margin(100)
.AsClip(out var clip, 30, name: "typstmath");

run(stage(bg: SKColors.White), [clip]);
```

> [!NOTE]
> 1, 可通过 #text[] 函数来给公式里的内容设置颜色
> 2, 可以自定义 Seg，动态设置其颜色。Seg 的定义方法类似 #text[]，比如，#t1[f(x)]，表示 id 为 t1 的 Seg，其内容为 f(x)。可通过 `Seg` 扩展方法来设置 Seg 的颜色。上例中，通过设置 t1,t2,t3 三个 Seg，动态设置其 alpha 值，来让公式逐步显示。

## 直接嵌入代码

通过 `TypstCode` 扩展方法，不用写 typ 文件，即可嵌入代码。示例([typstcode.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typstcode.cs))如下：

```csharp
#!/usr/bin/env dotnet

var code = """
            #!/usr/bin/env dotnet

            VGrid($"*", [
                TypstFile("./typst/page1.typ").Ref(out var typ)
                    .Align(0,0)
                    .OnFrame(e=> typ.UpdateInputs("frames", e.frame))
                ]).Background(SKColors.DeepSkyBlue)
                .AsClip(out var clip, 30, name: "typst");

            run(stage(bg: SKColors.Orange), [clip]);
            """;
VStack([
    TextBlock("TypstCode 直接嵌入代码").Margin(10),
    TypstCode(code,600, "csharp").PageMargin(0)
        .Align(0,-1).Margin(0)
]).Margin(10)
.AsClip(out var clip, 30, name: "typstcode");

run(stage(bg: SKColors.Orange), [clip]);
```