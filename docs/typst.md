# Typst 动画

## 什么是 Typst

**[Typst](https://typst.app/)**（读音 /taɪpst/）是一套**Rust 编写、开源的标记式排版系统**，定位是**现代版 LaTeX**：拥有 LaTeX 级别的专业出版排版能力，但是语法接近 Markdown、上手简单、编译极快，源码编译输出 PDF，主打学术论文、技术报告、书籍、简历等正式文档写作。

NewBeeVG 支持使用 Typst 嵌入复杂文档及动画。

内嵌 Typst 内容，需要本地安装 [typst](https://typst.app/)，且能正确编译/预览对应的 typ 文件。NewBeeVG 会调用 typst 的编译器，将 typ 文件编译成图像，嵌入到视频中。

## 嵌入 Typst 文件

使用 `TypstFile` 扩展方法，可以很方便的嵌入 Typst 文件。

[math.typ](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typst/math.typ) 是一个简单的 typ 文件:

```typst
#set page(fill: none)
#set page(width: auto, height: auto, margin: 10pt )
$ #text(fill: rgb("#FFFF00FF"))[A]  = pi r^2 $
$ "area" = pi dot "radius"^2 $
$ cal(A) :=
    { x in RR | x "is natural" } $
#let x = 5
$ #x < 17 $
```

嵌入 Typst 文件示例参考 [typstsimple.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typstsimple.cs) :

```csharp
#!/usr/bin/env dotnet

VStack([
    TypstFile("./typst/math.typ").Ref(out var typ)
        .Align(0,0)
]).Background(SKColors.DeepSkyBlue)
.AsClip(out var clip, 30, name: "typst");

run(stage(bg: SKColors.Orange), [clip]);
```
> [!NOTE]
> 在 typ 文件中设置 `#set page(fill: none)`，生成的图像的背景为透明色。设置 `#set page(  width: auto, height: auto)` 可以让图像大小适应内容。

## 嵌入 Typst 动画

> [!NOTE]
> typ 文件中可以通过 `sys.inputs.at` 获取编译参数。在每一帧，传入不同的参数，生成不同的图像，即可使用 typst 来生成动画。

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

## 直接嵌入 Typst 内容

通过 `TypstContent` 扩展方法，可以很方便的直接嵌入 Typst 内容。该方法原型为:

```csharp
public static NBTypstContent TypstContent(string content, int? pageMargin = 10)
{
    var file = new NBTypstContent();
    file.TypstContent = content;
    if (pageMargin != null)
        file.PageMargin(pageMargin.Value);
    return file;
}
```

> [!NOTE]
> 通过 `TypstContent` 扩展方法嵌入的 Typst 内容不需要设置 page 参数，相关参数可通过创建的 `NBTypstContent` 的 `PageMargin`、`PageSize`、`FontSize`、`ParagraphJustify`、`ParJustify(ParagraphJustify的简写)`、`PageBackgroud`、`PageBg(PageBackgroud的简写)` 等扩展方法来设置。

示例([typstcontent.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typstcontent.cs))如下：

```csharp
#!/usr/bin/env dotnet

 var content = """
     2025年，中国对外直接投资流量2135.8亿美元，比上年增长11.1%，对外投资存量3.4万亿美元，连续9年保持全球前三，占全球投资的比重增加到7.4%。对外投资行业覆盖国民经济的18个行业门类，主要集中在租赁和商务服务、批发零售、制造和金融四个领域，近年来逐步向绿色低碳、数字经济和绿色矿产等领域稳步拓展。

     截至2025年末，我国在境外设立企业5.8万家，遍布189个国家和地区，境外企业从业员工总数476.1万人，其中雇用外方员工296.5万人。
     """;

 VStack([
    TextBlock("TypstContent 直接嵌入内容").Margin(10),
    TypstContent(content).PageMargin(10).PageSize(500,null)
        .PageBg(SKColors.White).ParagraphJustify()
        .Align(0,-1).Margin(20),
    TypstContent(content).PageMargin(10).PageSize(500,null)
        .Align(0,-1).Margin(20)
 ]).Margin(100)
 .AsClip(out var clip, 30, name: "typstcontent");

run(stage(1920, 1080, bg: SKColors.Orange), [clip]);
```

## 使用 Seg 动画

对于 Typst 内容，可以标记一段文字作为一个 Seg，然后，通过 `Seg` 扩展方法动态改变文字属性，实现文字动画。

Seg 标注语法如下为 `#seg-id[seg-content]`，示例如下:

```typst
#t1["2025年，中国对外直接投资流量2135.8亿美元，比上年增长11.1%"]
```

> [!WARNING]
> seg-id 不能和 typst 的已有函数名 ( text 等) 冲突。

示例([typstseg1.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typstseg1.cs))如下：

```csharp
#!/usr/bin/env dotnet

 var content = """
    #t1[2025年，中国对外直接投资流量2135.8亿美元，比上年增长11.1%，对外投资存量3.4万亿美元，连续9年保持全球前三，占全球投资的比重增加到7.4%。对外投资行业覆盖国民经济的18个行业门类，主要集中在租赁和商务服务、批发零售、制造和金融四个领域，近年来逐步向绿色低碳、数字经济和绿色矿产等领域稳步拓展。]

    #t2[截至2025年末，我国在境外设立企业5.8万家，遍布189个国家和地区，境外企业从业员工总数476.1万人，其中雇用外方员工296.5万人。]

    #t3[2025年，中国企业对共建“一带一路”国家直接投资460.5亿美元，占当年对外投资流量的21.6%，在共建“一带一路”国家设立境外企业数量2.2万家，投资存量4072.5亿美元。]
    """;

 VStack([
    TextBlock("Seg 动画").Margin(10),
    TypstContent(content).Ref(out var typ)
        .PageMargin(10).PageSize(500,null)
        .PageBg(SKColors.White).ParagraphJustify()
        .OnFrame(e=>{
            typ.Seg("t1", SKColors.Red, 30 + e.frame * 4);
            typ.Seg("t2", SKColors.Green, 30 + e.frame * 4);
            typ.Seg("t3", SKColors.Blue, 30 + e.frame * 4);
         })
        .Align(0,-1).Margin(20),
    TypstContent(content).PageMargin(10).PageSize(500,null)
        .Align(0,-1).Margin(20)
 ]).Margin(100)
 .AsClip(out var clip, 30, name: "typst seg");

run(stage(1920, 1080, bg: SKColors.Orange), [clip]);
```

上面示例中，t1,t2,t3 三个 Seg，背景色分别为红、绿、蓝，透明通道 alpha 值从 30 逐渐增加到 150。

> [!WARNING]
> 直接嵌入 Typst 内容，使用 Seg 时，需要对每一个 seg-id，调用至少一次 `Seg` 扩展方法，否则会报编译错误。

为了避免报错，可以在 Typst 内容的开头加上相关函数定义，示例如下:
```typst
> #let t1(b)=text(b)
> #let t2(b)=text(b)
> #let t3(b)=text(b)
```

当然，可以以对 TypstFile 应用 Seg 语法。

示例 typst 文件  ( [seg.typ](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typst/seg.typ) ):

```typst
#set page(fill: white)

#set page(
  width: 600pt,
  height: auto,
  margin: 10pt,
)

#let t1(b)=text(b)
#let t2(b)=text(b)
#let t3(b)=text(b)

#set par(justify: true)

#t1[2025年，中国对外直接投资流量2135.8亿美元，比上年增长11.1%，对外投资存量3.4万亿美元，连续9年保持全球前三，占全球投资的比重增加到7.4%。对外投资行业覆盖国民经济的18个行业门类，主要集中在租赁和商务服务、批发零售、制造和金融四个领域，近年来逐步向绿色低碳、数字经济和绿色矿产等领域稳步拓展。]

#t2[截至2025年末，我国在境外设立企业5.8万家，遍布189个国家和地区，境外企业从业员工总数476.1万人，其中雇用外方员工296.5万人。]

#t3[2025年，中国企业对共建“一带一路”国家直接投资460.5亿美元，占当年对外投资流量的21.6%，在共建“一带一路”国家设立境外企业数量2.2万家，投资存量4072.5亿美元。]
```

文件头部通过 let 定义 t1,t2,t3 三个函数，避免 typst 预览报错。 

动画示例([typstseg2.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/typstseg2.cs))如下：

```csharp
#!/usr/bin/env dotnet

 VStack([
    TextBlock("Seg 动画").Margin(10),
    TypstFile("typst/seg.typ").Ref(out var typ)
        .OnFrame(e=>{
            typ.Seg("t1", SKColors.Red, 30 + e.frame * 4);
            typ.Seg("t2", SKColors.Green, 30 + e.frame * 4);
            typ.Seg("t3", SKColors.Blue, 30 + e.frame * 4);
         })
        .Align(0,-1).Margin(20)
 ]).Margin(100)
 .AsClip(out var clip, 30, name: "typst seg");

run(stage(1920, 1080, bg: SKColors.Orange), [clip]);
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
> 2, 数学公式支持 Seg 动画。可通过 `Seg` 扩展方法来设置 Seg 的颜色。上例中，通过设置 t1,t2,t3 三个 Seg，动态设置其 alpha 值，来让公式逐步显示。

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

> [!NOTE]
> 默认使用 typst 的 `zebraw` 包显示代码，`zebraw` 的语法详细参考 [zenbraw 的文档](https://typst.app/universe/package/zebraw/)。

> [!WARNING]
> 嵌入代码不支持 Seg 动画。