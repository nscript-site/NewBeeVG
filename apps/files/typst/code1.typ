#import "@preview/zebraw:0.6.3": *
#show: zebraw

#set page(fill: none)

#set page(
  width: 600pt,
  height: auto,
  margin: 10pt,
)

#zebraw(
  lang: false,

```cs
font("阿里巴巴普惠体 2.0");

// clip1
VStack([
    Layer([
        TextBlock("输入你的文字").Font(120, SKColors.Black).Align(0,0).Id("Text"),
        Rect().Bind("Text").Shader(AlphaLinearGradientShader())
    ]),
    TextBlock("输入你的文字").Font(120, SKColors.Black)
        .OnFrame(e=> { e.Sender.Opacity(e.p);  e.SenderLayoutable?.Margin(0,e.p * 200,0,0); })
])
.Align(0, 0)
.AsClip(out var clip1, frames: 40, name: "animate");

VStack([
    TextBlock("Code").Font(80, SKColors.Orange).Align(0,0),
    TypstFile("./typst/code1.typ").MaxHeight(800).Align(0,0),
    TextBlock("生成视频的全部代码").Font(40, SKColors.Black).Align(0,-1),
]).Align(0,0).AsClip(out var clip2, frames: 120, name: "code");

run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2]);
```

)

