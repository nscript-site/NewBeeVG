# Typst 逐页动画模式

## 1. 页面设置

每个动画文件都从页面设置开始：

```typst
#set page(
  width: 960pt,
  height: 540pt,
  margin: 40pt,
  fill: none,
)
#set text(size: 28pt, font: "New Computer Modern")
#set par(justify: true)
```

规则：

- 16:9 用 `960pt × 540pt`；1:1 用 `900pt × 900pt`；
- `fill: none` 透明；需要底色时用 `fill: rgb("#...")`，或交给 NewBeeVG 背景；
- **所有页面必须同尺寸**，`NBTypstAnimation` 以第一页为基准；
- 页面尺寸决定节点比例，最终由布局缩放到目标区域。

## 2. 基本逐页动画

```typst
#set page(width: 960pt, height: 540pt, margin: 40pt, fill: none)

= 第 1 步
$ x^2 + 2x + 1 = 0 $

#pagebreak()

= 第 2 步
$ (x + 1)^2 = 0 $

#pagebreak()

= 第 3 步
$ x = -1 $
```

```csharp
var anim = TypstAnim("./assets/typst/steps.typ");

HStack([
    anim.LoopMode(NBAnimateLoopMode.Stop).Align(0, 0),
]).AsClip(anim.TotalFrames, name: "steps");
```

- 3 页 = 3 帧；
- 放进 3 帧 clip 正好播完；
- 放进长 clip 且 `Stop`，前 3 帧播完，之后停在最后一页；
- 放进长 clip 且 `Loop`，每 3 帧循环一次。

## 3. 60 帧函数动画

CeTZ 函数图通常用循环生成 60 页，每页对应一个时间 `t`：

```typst
#import "@preview/cetz:0.3.4": canvas, draw

#set page(width: 400pt, height: 240pt, margin: 0pt)

#let N = 60
#let PI = calc.pi

#let frame(t) = canvas(length: 1pt, {
  import draw: *
  // 网格、坐标轴、曲线、标注
})

#for i in range(N) {
  let t = 2 * PI * i / N
  [ #frame(t) #pagebreak(weak: true) ]
}
```

完整示例见 `assets/anim_plot.typ`：`y = sin(x - t)`，60 页，包含网格、坐标轴、刻度、参照波形、运动波形和波峰红点。

```csharp
var plot = TypstAnim("./assets/typst/anim_plot.typ");

HStack([
    plot.LoopMode(NBAnimateLoopMode.Loop).Align(0, 0),
]).AsClip(plot.TotalFrames, name: "sin-wave");
```

## 4. 每页停留

`TypstAnim` 没有逐页持续时间参数；一页就是一帧。需要一页显示 30 帧时，生成 30 个相同页面：

```typst
#for _ in range(30) {
  [ 页面内容 #pagebreak(weak: true) ]
}
```

页数 = 步骤数 × 停留帧数。适合短动画；页数过多时降低页面尺寸或减少步骤。

## 5. 公式逐步推导

- 每页只放一个推导状态；
- 用 `#pagebreak()` 分隔；
- 关键公式用独立页，避免一页内容过密；
- 需要多行推导时用 `$ ... \ ... $` 或分行写多个公式；
- 不要把大段文字和复杂图形塞进同一页。

```typst
#set page(width: 960pt, height: 540pt, margin: 40pt, fill: none)
#set text(size: 28pt)

$ x^2 + 2x + 1 = 0 $

#pagebreak()

$ (x + 1)^2 = 0 $

#pagebreak()

$ x = -1 $
```

## 6. CeTZ 示意图

```typst
#import "@preview/cetz:0.3.4": canvas, draw
#set page(width: 960pt, height: 540pt, margin: 20pt, fill: none)

#canvas({
  import draw: *
  set-style(stroke: 2pt + rgb("#4FA3D1"))

  line((0, 0), (6, 0))
  line((0, 0), (0, 4))
  circle((2, 2), radius: 0.8, fill: rgb("#F0B429"))
})
```

注意：

- 坐标使用 CeTZ 画布单位，图形保持在页面范围内；
- `cetz:0.3.4` 需要 Typst 0.13+；本机 CLI 较旧会报版本错误；
- 复杂三维、阴影、混合模式可能无法完整转成 SVG，先预览验证；
- 每页的图元数量会影响编译时间和内存。

## 7. 表格与排版

- 固定页面宽度与表格列宽，避免不同页宽度不一致；
- 表格跨页会破坏“一页一帧”的对应关系，重要表格拆成独立页；
- 关键文字指定字体，避免回退字体导致换行变化；
- 代码块可使用 `zebraw` 包，但会增加包依赖；
- 页面边距、字号、行距在所有页保持一致。

## 8. 字体与包

- 自定义字体放在脚本目录的 `fonts/` 或 `assets/typography/`；
- NewBeeVG 搜索这两个相对路径和系统字体；
- 字体缺失会回退，公式/中文排版可能变化；
- `@preview/...` 包必须存在于 `LocalApplicationData/typst/packages`；
- 已知示例版本：`cetz:0.3.4`；升级前先验证；
- `#import` 路径和图片相对 `.typ` 文件所在目录解析。

## 9. 编译与验证

单独预览/导出帧序列：

```bash
typst compile anim_plot.typ "frames/f-{0p}.png" --ppi 200
```

NewBeeVG 内：

```csharp
var anim = TypstAnim("./assets/typst/anim_plot.typ");

HStack([
    anim.LoopMode(NBAnimateLoopMode.Loop).Align(0, 0),
]).AsClip(anim.TotalFrames, name: "plot");

run(stage(1920, 1080, bg: SKColors.White), clips(), args: args);
```

`NBTypstAnimation` 是可视节点，不是 `ISKAnimation`，没有 `SavePNG`；导出使用 `run(...)` 的 PNG/MP4 模式。

检查：

- 页数是否符合目标帧数；
- 所有页面尺寸是否一致；
- 第一页是否空白；
- 最后一页是否有多余空白页；
- 透明区域是否正常；
- 字体、公式、CeTZ 图形是否完整。

## 10. 常见问题

| 现象 | 原因与处理 |
| --- | --- |
| 动画一闪而过 | 页数太少；增加页面或使用 `Loop` |
| 最后一页停留 | `LoopMode = Stop`；需要循环用 `Loop` |
| 多出空白页 | 循环尾部 `#pagebreak()`；用 `#pagebreak(weak: true)` |
| 页面被拉伸 | 各页尺寸不一致；统一 `width/height` |
| 节点空白 | 文件路径或编译错误；检查 `DecodeException` |
| cetz 报版本错误 | `cetz:0.3.4` 需要 Typst 0.13+ |
| 包找不到 | 版本未缓存；检查 `LocalApplicationData/typst/packages` |
| 字体不对 | 缺少字体文件或未安装；放入 `fonts/` 或换系统字体 |
| 编译很慢 | 页数多、页面大、图元复杂；降低尺寸与页数 |
