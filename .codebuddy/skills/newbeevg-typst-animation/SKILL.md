---
name: newbeevg-typst-animation
description: 本技能用于在 NewBeeVG 的 dotnet 文件式视频项目中用 TypstAnim 制作逐页 Typst 动画。涵盖 Typst 页面尺寸与透明背景、逐页输出、页数与帧数换算、CeTZ 函数图与示意图、公式推导、字体与包路径、性能优化，以及把 TypstAnim 节点嵌入布局。
---

# NewBeeVG Typst 动画制作

## 用途

Typst 动画只使用一个入口：

```csharp
public static NBTypstAnimation TypstAnim(string path);
```

`TypstAnim` 把 `.typ` 文档的**每一页编译成一帧**，适合：

- 数学公式推导、逐步展开的证明；
- CeTZ 函数图、坐标系、几何示意图；
- 分镜式图表、表格、复杂排版；
- 每页内容不同、帧数固定的动画。

本技能只做**逐页动画**：页数就是帧数，不涉及运行时参数重编译。不要改用 `TypstFile`、`TypstContent` 等静态控件替代；动画统一走 `TypstAnim`。

## 核心接口

```csharp
public static NBTypstAnimation TypstAnim(string path);
```

`NBTypstAnimation` 继承 `NBBaseLoopAnimation`：

| 成员 | 说明 |
| --- | --- |
| `TotalFrames` | 成功编译的页数，也是动画总帧数 |
| `TypstFrames` | `List<SKPicture>`，每页一张 |
| 页面尺寸 | 取自第一页的 `CullRect`；所有页按此尺寸映射 |
| `LoopMode` | 默认 `Stop`（停在最后一页）；循环用 `Loop` |
| `Speed` | 播放倍速 |

行为：

- `path` 为 `.typ` 文件路径，相对路径按运行时目录解析；
- 调用 `TypstAnim` 时立即读取并编译整个文档；
- 每页 `RenderPageToSvg(i)`，再转成 `SKPicture`；
- 编译失败记录在 `DecodeException`，节点不绘制内容；
- 文件不存在时记录 `IOException`，不会中断视频。

`NBTypstAnimation` 是**可视节点**，不是 `ISKAnimation`，没有 `SavePNG` / `SaveMp4` / `SaveGif`；要预览或导出画面，使用 `run(...)` 或把它放进 clip 后走视频导出流程。

## 工作流

### 1. 编写 Typst 文档

保存到视频脚本目录的 `assets/typst/`，目录不存在时创建。

硬性要求：

- 所有页面使用**相同的 `width` / `height`**；
- 页面比例与目标布局区域一致；
- 需要透明背景时写 `fill: none`；
- 每一页放一个动画状态，页间用 `#pagebreak()`；
- 不要在页面里依赖鼠标、点击等交互事件。

```typst
#set page(width: 960pt, height: 540pt, margin: 40pt, fill: none)
#set text(size: 28pt)

= 第 1 步
$ x^2 + 2x + 1 = 0 $

#pagebreak()

= 第 2 步
$ (x + 1)^2 = 0 $

#pagebreak()

= 第 3 步
$ x = -1 $
```

### 2. 用 C# 接入

```csharp
var anim = TypstAnim("./assets/typst/steps.typ");

HStack([
    anim.LoopMode(NBAnimateLoopMode.Stop).Align(0, 0),
]).AsClip(anim.TotalFrames, name: "steps");
```

- `anim.TotalFrames` 在调用 `TypstAnim` 时已经确定，可直接用于 `AsClip`；
- 3 页文档就是 3 帧动画，`AsClip(3)` 最自然；
- 放进更长的 clip 且 `Stop`，前 3 帧播完 3 页，之后停在最后一页；
- 放进更长的 clip 且 `Loop`，会按页数循环，速度快；
- 需要“每页停留”时复制页面，而不是依赖播放器自动停留。

### 3. 页数、帧数与时长

`TypstAnim` 没有独立的帧率参数；页数直接对应帧数：

```text
60 页 → 60 帧 → 25fps 下 2.4 秒
```

因此：

- 目标 2 秒、25fps 的动画需要 50 页；
- 目标 3 秒、30fps 的动画需要 90 页；
- 页数不足时，播放会在最后一页停住（`Stop`）或快速循环（`Loop`）；
- 需要每页停留 N 帧，就在 Typst 中把该页内容重复 N 次，生成 N 倍页数；
- 页面越多，编译时间和内存越高，优先用较小页面尺寸（如 400×240、960×540）。

### 4. 校验

- 确认 `TotalFrames` 与预期页数一致；
- 确认所有页面尺寸一致；
- 确认第一页不是空白；
- 确认最后一页与第一页的衔接符合预期；
- 确认透明区域没有白底/黑底；
- 编译失败时读取 `DecodeException` 的消息与位置；
- 用 `run(...)` 预览或导出画面：

```csharp
HStack([
    anim.LoopMode(NBAnimateLoopMode.Stop).Align(0, 0),
]).AsClip(anim.TotalFrames, name: "steps");

run(stage(1920, 1080, bg: SKColors.White), clips(), args: args);
```

```bash
# 预览
dotnet run --
# 导出第 0 帧
dotnet run -- 0 output/typst/frame_000.png
# 导出 MP4
dotnet run -- output/typst.mp4
```

## CeTZ 函数动画示例

`assets/anim_plot.typ` 是完整的可运行示例：

- 60 页，每页对应 `y = sin(x - t)` 的一个 `t`；
- CeTZ 绘制网格、坐标轴、刻度、参照波形、运动波形和波峰红点；
- 页面尺寸 `400pt × 240pt`，比例 5:3；
- 末尾用 `#pagebreak(weak: true)` 避免多出空白页。

接入 NewBeeVG：

```csharp
var plot = TypstAnim("./assets/typst/anim_plot.typ");

HStack([
    plot.LoopMode(NBAnimateLoopMode.Loop).Align(0, 0),
]).AsClip(plot.TotalFrames, name: "sin-wave");
```

单独预览/导出帧序列：

```bash
typst compile anim_plot.typ "frames/f-{0p}.png" --ppi 200
```

注意：`@preview/cetz:0.3.4` 要求 **Typst 0.13.0 或更高版本**。本机 CLI 较旧时会报：

```text
package requires typst 0.13.0 or newer
```

此时升级 Typst CLI，或直接使用 NewBeeVG 内置的 Typst 编译器；不要通过降低 cetz 版本破坏示例。

## 逐页动画的常见结构

### 公式逐步推导

每页一个公式状态，页间 `#pagebreak()`。适合证明、配方、算法步骤。

### CeTZ 函数图

把时间 `t` 作为页参数，每页重新绘制整张图：

```typst
#let N = 60
#let frame(t) = canvas(length: 1pt, {
  import draw: *
  // 画网格、坐标轴、曲线
})

#for i in range(N) {
  let t = 2 * calc.pi * i / N
  [ #frame(t) #pagebreak(weak: true) ]
}
```

### 每页停留

需要一页显示 30 帧时，在 Typst 中生成 30 个相同页面：

```typst
#for _ in range(30) {
  [ 页面内容 #pagebreak(weak: true) ]
}
```

页数会变成“步骤数 × 停留帧数”，适合短动画；页数过多时降低页面尺寸或减少步骤。

### 表格与排版

- 每页固定宽度，避免自动换行导致页面尺寸变化；
- 表格列宽固定，避免不同页宽度不一致；
- 关键文字指定字体，避免回退字体造成布局差异；
- 代码块可配合 `zebraw` 包，但会增加编译依赖。

## 页面、透明与字体

- 页面设置写在 `.typ` 文件最前面：

```typst
#set page(width: 960pt, height: 540pt, margin: 40pt, fill: none)
```

- `fill: none` 透明；需要底色时由 NewBeeVG 布局提供，或显式设置页面背景；
- 16:9 常用 `960pt × 540pt`；1:1 常用 `900pt × 900pt`；
- 自定义字体放在脚本目录的 `fonts/` 或 `assets/typography/`；NewBeeVG 会搜索这两个相对路径和系统字体；
- 字体缺失会回退，公式和中文排版可能变形；关键镜头固定字体并验证；
- `@preview/...` 包必须存在于 `LocalApplicationData/typst/packages` 缓存中；
- 已知示例版本：`cetz:0.3.4`；升级版本前先编译验证。

## 组合与布局

`TypstAnim` 返回普通可视节点，可与 `SkiaAnim`、`PngAnim`、文字组合：

```csharp
HGrid("*,*", [
    TypstAnim("./assets/typst/anim_plot.typ")
        .LoopMode(NBAnimateLoopMode.Loop),

    VStack([
        TextBlock("函数图像").Font(72, SKColors.White),
        TypstAnim("./assets/typst/steps.typ")
            .LoopMode(NBAnimateLoopMode.Stop),
    ]).Spacing(20),
]).AsClip(60, name: "math");
```

- 每个 `TypstAnim` 独立编译，首帧会有编译停顿；
- 多个 Typst 节点会叠加编译开销，控制页面数和尺寸；
- 透明页面可以直接叠在背景素材上；
- 需要各自独立时间轴时，拆成多个 clip 并用 `start` 叠加。

## 性能与内存

- `TypstAnim` 只编译一次，播放时直接绘制 `SKPicture`；
- 每页都会生成一张 `SKPicture`，页数多、分辨率高时内存明显增加；
- 优先使用 400×240、960×540 这类页面尺寸，不要盲目用 1920×1080；
- 60–90 页的 CeTZ 图通常可接受；数百页时先降低尺寸或减少帧数；
- 不需要逐帧变化的内容做成静态布局，不要为了“动画”强行拆页。

## 常见问题

| 现象 | 原因与处理 |
| --- | --- |
| 节点空白 | 文件不存在或编译失败；检查路径与 `DecodeException` |
| 动画一闪而过 | 页数太少；`TypstAnim` 的页数就是帧数，增加页面或使用 `Loop` |
| 最后一页停留很久 | `LoopMode = Stop`；需要循环改 `.LoopMode(NBAnimateLoopMode.Loop)` |
| 页面被拉伸/裁切 | 各页尺寸不一致；统一所有页面的 `width/height` |
| 多出空白页 | 循环末尾的 `#pagebreak()`；用 `#pagebreak(weak: true)` 避免尾部空页 |
| 字体不对 | 字体未安装或不在 `fonts/`、`assets/typography/`；补字体或换系统字体 |
| 包找不到 | `cetz`/`zebraw` 版本未缓存；确认 `LocalApplicationData/typst/packages` |
| cetz 报版本错误 | `cetz:0.3.4` 需要 Typst 0.13+；升级 CLI 或使用 NewBeeVG 内置编译器 |
| 透明背景变白/黑 | 页面 `fill` 不是 `none`；检查 `#set page(fill: ...)` |
| 编译很慢 | 页数太多或页面尺寸太大；减少页面、降低尺寸、简化 CeTZ 图元 |

## 资源

- `references/newbeevg-typst-api.md`：`TypstAnim` 入口、`NBTypstAnimation` 成员、生命周期与性能。
- `references/typst-animation-patterns.md`：逐页动画、CeTZ、公式推导、停留页、字体与包。
- `assets/anim_plot.typ`：60 页 `y = sin(x - t)` CeTZ 函数动画，可直接用 `TypstAnim` 播放。
