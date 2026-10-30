---
name: newbeevg-video
description: This skill should be used when building, editing, or debugging a NewBeeVG video project with stage/clip/layout/text/effects/export, especially in dotnet file-based apps. It covers assets discovery (README + audios/images/videos/svgs), choosing and composing SkiaAnim/PngAnim/LottieAnim/TypstAnim, SVG-to-APNG conversion, AsClip timing, HGrid/VGrid/HStack/VStack/Panel/WrapPanel layout, Align semantics, NBText/NBRichText/styles, Shaders/BitmapFilters/FrameMask/Filters/ColorFilters/Opacity/RenderTransform, the widget catalog except deprecated NBLayer, embedding SKCustomAnimation via SkiaAnim, audio clips via AudioFile/LinkTo, and run(...) modes (preview, MP4 export, single-frame PNG export).
---

# NewBeeVG 视频制作

## 用途

用 NewBeeVG 的 markup + clip 时间线构建视频：用布局组件描述每一帧画面，用 `AsClip` 把画面变成有起止帧的片段，最后用 `run(...)` 预览或导出。

推荐组合：**布局组件（排版）+ 动画（SkiaAnim / PngAnim / LottieAnim / TypstAnim）+ assets 素材（背景/图标/图片/SVG）**。

不适用：深入 3D、TTS 等专项能力时，再查对应专项文档；本技能不覆盖已废弃的 `NBLayer`。

## 核心模型

| 概念 | 类型 | 说明 |
| --- | --- | --- |
| workspace | `NBWorkspace` | 工作空间，可包含多个 work；`run` 会设置 `NBWorkspace.Current` |
| work | `NBWork` | 一个视频项目，持有 `Stage` 与多条 `Tracks` |
| track | `NBTrack` | 轨道，按顺序渲染多个 clip |
| clip | `NBClip` / `NBDrawingClip` | 片段，核心参数是起始帧与持续帧数 |
| stage | `NBStage` | 画幅、帧率、背景色、音频采样率 |

`run` 默认把传入的所有 clip 放进**同一条 track**；`clipLists` 里的片段也会合并到这条 track。

clip 的时间规则：

- `frames > 0`：持续指定帧数；
- `frames <= 0`（常写 `-1`）：持续到最后一帧，适合装饰层、角标、背景；
- `start = null`：自动接在上一个 clip 之后；
- `start = 整数`：从指定帧开始；`start = -1` 常用于装饰层（配合 `frames = -1` 全程显示）。

## 工程约定

- 使用 **dotnet file-based app**：文件以 `#!/usr/bin/env dotnet` 开头，`dotnet xxx.cs` 直接运行。
- 需要引用其他源文件时用 `#:include 相对路径.cs`；类声明放在顶层语句之后。
- 仓库 `apps/files/` 下已有 `Directory.Build.props`，全局导入 `NewBeeVG`、`SkiaSharp`、静态 `NewBeeVG.Methods`，不用重复 `using`。
- 仓库外运行需在文件顶部加 `#:package NewBeeVG@1.0.1.25`；仓库内由 `Directory.Build.props` 提供项目引用，出现重复引用时删除该指令。
- **首字母大写的方法**是界面/布局/样式方法（`TextBlock`、`HStack`、`Align`、`Background`…）；
- **首字母小写的方法**是行为方法（`stage`、`run`、`font`、`clips`…）。

## 资源目录

`assets` 是资源根目录，按类型组织：

```text
assets/
├── audios/
├── images/      # 含 PngAnim 使用的 APNG
├── videos/
├── svgs/
├── lottie/      # LottieAnim 的 JSON
├── typst/       # TypstAnim 的 .typ 及依赖
└── README.md
```

自创的 `SkiaAnim` 源码不放在 `assets`，而是与它同级：

```text
modules/
├── MyAnimation.cs
└── README.md
```

查找顺序：

1. 先看视频脚本当前目录下的 `./assets`；
2. 不存在则依次向父目录查找；
3. 一直找到项目根目录下的 `assets`；
4. 使用最近的一个存在的 `assets` 目录。

**优先读取 `assets/README.md`**：该文件通常记录资源清单、用途、尺寸、时长、来源等。先按 README 索引找素材，不要盲目遍历整个目录。

引用规则：

- 代码中的路径相对于运行时目录（通常是脚本所在目录）；
- 引用上层 assets 时写相对路径，如 `../assets/images/bg.png`；
- 新生成的素材优先放到**视频脚本当前目录**的 `assets` 下，目录不存在时创建；
- 命名建议小写、短横线或下划线，并体现用途，如 `svgs/arrow-flow.svg`、`images/bg-night.png`。

## 素材分析与生成

制作视频前先做素材盘点：

1. **分析稿件/脚本**：拆分镜头，列出每个 clip 需要的背景、图片、图标、图表、SVG、音频、视频素材；
2. **读取 assets 与 README**：标记已有素材，列出缺失素材；同时检查 `modules/README.md` 是否已有合适的 `SkiaAnim` 特效；
3. **选择动画实现**：复杂特效/数据图形用 `SkiaAnim`，简单矢量动效用 SVG → APNG（`PngAnim`），数学/科学排版用 `TypstAnim`，Lottie 仅作兜底；
4. **生成缺失素材**：优先使用大模型的 SVG 生成能力，产出静态 SVG 或带 SMIL/CSS 动画的 SVG；简单动画再转成 APNG；
5. **保存素材**：
   - SVG → `assets/svgs/`
   - 图片 / APNG → `assets/images/`
   - Lottie → `assets/lottie/`
   - Typst → `assets/typst/`
   - 音频 → `assets/audios/`
   - 视频 → `assets/videos/`
   - 目录不存在时先创建；
6. **登记**：在 `assets/README.md` 中补充文件名、用途、尺寸/时长，方便后续复用。

生成 SVG 时：

- 画布尺寸与使用位置匹配，避免运行时大幅缩放；
- 图标/插画使用清晰的分层与语义化命名；
- 需要动效时优先用 SMIL（`animate` / `animateTransform`）或内联 CSS 动画；
- 颜色尽量与视频主题色一致；
- 避免外部字体、外链图片和脚本，保证 SVG 自包含。

## 动画素材选型

`SkiaAnim`、`PngAnim`、`LottieAnim`、`TypstAnim` 都会返回可视节点，可直接放进任意布局容器。按内容特点选择：

| 工厂 | 适用场景 | 素材来源 |
| --- | --- | --- |
| `SkiaAnim(ISKAnimation)` | 复杂动画、数据图形、数字滚动、图表、程序化特效 | `modules/` 预制特效；没有则新建 |
| `PngAnim(path)` | 画面简单、矢量插画、SMIL/CSS 动画 | 大模型生成 SVG，再转成 APNG |
| `LottieAnim(path)` | Lottie JSON 动画 | 大模型可生成，但质量较差，不推荐 |
| `TypstAnim(path)` | 数学、科学、矢量图表、复杂排版 | Typst + CeTZ 多页文档 |

### `SkiaAnim`：复杂动画与程序化特效

适合需要逐帧计算、数据驱动或精细控制的动画，例如数字增长、图表生长、粒子、路径描边、转场特效。

1. **先找预制特效**：项目目录的 `modules/` 通常放有现成的 `SkiaAnim` 特效，先读 `modules/README.md`，按介绍选择；找到后用 `#:include modules/Xxx.cs` 引入并直接 `SkiaAnim(new Xxx(...))`；
2. **没有合适的再新建**：使用 `newbeevg-skia-animation` 技能创建 `SKCustomAnimation` 子类；
3. **保存位置**：自创特效放在**视频脚本文件所在目录**的 `modules/`，目录不存在时创建；同步在 `modules/README.md` 中补充用途、参数和示例；
4. 简单静态图形不必新建特效，优先用 SVG；需要简单动效时用 SVG 转 APNG（`PngAnim`）。

### `PngAnim`：简单画面与矢量动画

适合扁平插画、图标位移、路径生长、描边、简单形变等矢量场景。流程：

1. 用大模型生成带 SMIL / CSS 动画的自包含 SVG，保存到 `assets/svgs/`；
2. 用 `svg-animation-convert` 转成 APNG：

```bash
npx --yes svg-animation-convert --input .\your.svg --output your.apng
```

3. 把输出的 APNG 放到 `assets/images/`，脚本中用 `PngAnim("./assets/images/your.apng")` 引入；转换器的输出扩展名必须是 `.apng`，不能写 `.png`。

无限循环的 SVG 动画必须显式指定转换时长，否则转换可能无法结束或帧数不确定：

```bash
npx --yes svg-animation-convert --input .\your.svg --output your.apng --duration 3
```

- `--duration` 按秒计，取值覆盖一个完整循环或镜头所需时长；
- 控制 APNG 的分辨率与帧数，避免文件过大、解码过慢；
- 转换后先在预览中确认循环衔接与透明区域是否正常；
- 需要完整的 SVG 动画编写、转换参数、APNG 结构校验与优化流程时，使用 `newbeevg-apng-animation` 技能。

### `LottieAnim`：不推荐作为首选

`LottieAnim` 可以加载 Lottie JSON 并包装成可视节点，但**目前大模型生成 Lottie 的能力较差**，容易出现结构错误、图层错乱或动画不符合预期，因此只作为已有可靠素材时的兜底，不推荐让 AI 从零生成。

```csharp
LottieAnim("./assets/lottie/loading.json");
```

### `TypstAnim`：数学、科学与复杂排版

适合公式推导、科学示意图、复杂表格、矢量图表等 Typst 擅长的内容。用 Typst 编写多页文档，配合 CeTZ 等包绘制矢量图，每个页面会成为一帧：

```csharp
TypstAnim("./assets/typst/derivation.typ");
```

- 页面尺寸、边距应与目标布局区域匹配；
- 多页文档就是时间轴：页数决定动画帧数，重要步骤拆成独立页；
- 图片、数据和其他依赖放在 `.typ` 同目录或子目录，用相对路径引用；
- 编译工作区是 `.typ` 文件所在目录，`#include` / `#import` 的相对路径以它为基准；
- 需要完整的 Typst 页面尺寸、CeTZ 示意图、逐页动画与页数/帧数换算时，使用 `newbeevg-typst-animation` 技能。

### 存放约定

```text
<视频脚本目录>/
├── assets/
│   ├── images/      # PngAnim 的 APNG / PNG
│   ├── lottie/      # LottieAnim 的 JSON
│   ├── typst/       # TypstAnim 的 .typ 及其依赖
│   ├── svgs/        # SVG 源文件
│   └── README.md
└── modules/         # 自创 SkiaAnim 的 .cs 与 README.md
```

- `PngAnim`、`LottieAnim`、`TypstAnim` 的动画素材放在**视频脚本文件所在目录**的 `assets/` 下，目录不存在时创建；
- 自创的 `SkiaAnim` 放在**视频脚本文件所在目录**的 `modules/` 下，目录不存在时创建；
- 新增素材或模块后，同步更新对应的 `README.md`，方便后续复用。

## 组合原则

制作每个 clip 时，尽量**组合布局组件、动画组件和素材**，不要用单一的全屏动画构成整个 clip。四种动画可以组合使用，也可以在同一画面中通过布局嵌入多个动画节点：

```csharp
// 推荐：背景素材 + 布局分栏 + 多个动画 + 文字
Panel([
    Image("./assets/images/bg.png").Size(1920, 1080).Stretch(Stretch.UniformToFill),

    HGrid("480,*", [
        PngAnim("./assets/images/icon-chart.png").Size(320, 320).Align(0, 0),

        HStack([
            SkiaAnim(new ChartAnimation(600, 800, 90, content)),
            VStack([
                TypstAnim("./assets/typst/formula.typ"),
                TextBlock("季度增长").Font(72, SKColors.White),
                TextBlock("+128%").Font(160, SKColors.Orange),
            ]).Spacing(20),
        ]).Spacing(40),
    ]).Margin(120),

    TextBlock("数据来源：内部统计").Font(32, SKColors.LightGray)
        .Align(-1, 1).Margin(80),
])
.AsClip(90, name: "chart");
```

- **布局组件**负责分栏、留白、对齐与信息层级；
- **动画组件**负责动态内容：复杂特效与数据图形用 `SkiaAnim`，简单矢量动画用 `PngAnim`，数学/科学排版用 `TypstAnim`，Lottie 仅作兜底；
- **assets** 提供背景、照片、图标、插画、音视频；
- 一个 clip 内通常至少包含两类元素（例如动画 + 文字，或素材 + 布局 + 动画）；
- **同一画面可以嵌入多个动画节点**：把 `SkiaAnim`、`PngAnim`、`TypstAnim` 分别放进 `HGrid` / `HStack` / `Panel` 的不同区域，形成主图 + 辅助图 + 数据卡等组合；
- 同一 clip 内的动画共享当前帧号；需要各自独立时间轴时，把动画拆成不同 clip，用 `start` 叠加或错开；
- 片头/片尾可用纯文字布局；正文与数据镜头应避免“整个画面只有一个全屏动画”。

## 最小视频骨架

```csharp
#!/usr/bin/env dotnet

#:include modules/MyAnimation.cs

// 1) 片头
{
    VStack([
        TextBlock("视频演示").FontSize(200).Foreground(SKColors.White)
    ]).Align(0, 0)
      .AsClip(30, name: "start");
}

// 2) 内容：在布局中嵌入 SkiaAnim
{
    var content = new MyAnimation.ContentSetting { From = 0, To = 12580, Format = "N0" };
    int frames = 90;

    HGrid("*,*", [
        SkiaAnim(new MyAnimation(600, 400, frames, content)),
        SkiaAnim(new MyAnimation(600, 400, frames, content)),
    ]).AsClip(frames, name: "clip1");
}

// 3) 片尾
{
    VStack([
        TextBlock("视频结束").FontSize(200).Foreground(SKColors.White)
    ]).Align(0, 0)
      .AsClip(30, name: "end");
}

// 4) 装饰层：frames = -1 全程显示，start = -1
{
    VGrid("100,*,100", [
        TextBlock("Logo").Align(1, -1).Margin(20, 20, 50, 0),
        null,
        TextBlock("Footer").Align(1, 1).Margin(20, 0, 50, 20)
    ]).AsClip(-1, -1, name: "decoration");
}

// 预览；传 args 时支持导出
run(stage(1920, 1080, bg: SKColors.Orange), clips(), args: args);
```

注意：片段用 `{ }` 隔离局部变量，避免 `content` / `frames` 等重名。

## 创建 clip

```csharp
// 直接创建并加入 clips()
HStack([...]).AsClip(60, name: "intro");

// 需要后续引用
HStack([...]).AsClip(out var intro, 60, start: 0, name: "intro");

// 从第 30 帧开始，持续 90 帧
HStack([...]).AsClip(90, 30, name: "body");
```

- `AsClip` 会自动把 clip 登记到 `clips()`；
- 不传 `start` 时自动紧接前一个 clip；
- `AsClip(out var clip, ...)` 的 `out` 可用于后续 `Bind`、手动改 `IsVisible` 等；
- `name` 建议唯一，便于调试与按名称查找。

## 音频片段

音频是独立的 `NBAudioClip`，不绘制画面；用 `LinkTo` 把它的起始时间对齐到某个画面 clip：

```csharp
// 音频链接到 name 为 clip1 的 第 0 帧（初始帧）
AudioFile("./Assets/audio01.mp3")
    .AsClip("audio").LinkTo("clip1");

// 音频链接到 clip1 的第 10 帧（offset 为相对 clip1 起点的帧数）
AudioFile("./Assets/audio01.mp3")
    .AsClip("audio").LinkTo("clip1", 10);
```

- `AudioFile(path)` 创建音频 clip；
- `.AsClip("audio")` 登记音频并命名（音频 clip 不需要 `frames`）；
- `.LinkTo("clip1", offset)`：音频起点 = 目标 clip 的 `StartFrame + offset`，`offset` 默认 `0`；
- 目标 clip 必须**先创建并登记名称**，否则 `LinkTo` 找不到目标，链接会静默失败；
- 音频时长在 `Prepare` 阶段按文件自动计算，`DurationFrames` 会被更新；
- 音频与画面进入同一 track 混音，预览和 MP4 导出都会带音频；
- 路径相对于运行目录。

完整示例：

```csharp
VStack([
    TextBlock("Clip1").Font(80, SKColors.Orange).Align(0, 0)
]).Align(0, 0).AsClip(frames: 40, name: "clip1");

VStack([
    TextBlock("Clip2").Font(80, SKColors.Orange).Align(0, 0)
]).Align(0, 0).AsClip(frames: 40, name: "clip2");

// 音频链接到 clip1 的第 0 帧
AudioFile("./Assets/audio01.mp3")
    .AsClip("audio").LinkTo("clip1");

// 音频链接到 clip1 的第 10 帧
// AudioFile("./Assets/audio01.mp3")
//     .AsClip("audio").LinkTo("clip1", 10);

run(stage(1920, 1080, bg: SKColors.White), clips());
```

## 布局

| 容器 | 用途 |
| --- | --- |
| `HGrid(colDef, childs)` | 单行多列 Grid |
| `VGrid(rowDef, childs)` | 单列多行 Grid |
| `HStack(childs)` / `VStack(childs)` | 水平 / 垂直 Stack |
| `Panel(childs)` | 层叠（后绘制的在上层） |
| `WrapPanel(childs, isHorizontal)` | 自动换行 |

Grid 尺寸字符串：`"*,200,0.5*,2*,Auto"`

- `*` = `1*`，按权重分配剩余空间；
- `200` = 固定像素；
- `Auto` = 由内容决定；
- 子元素数量不足补 `null`，超出定义的忽略。

`Align(hAlign, vAlign)`：

| 取值 | 行为 |
| --- | --- |
| `null` | 拉伸填满 |
| `< 0` | 左 / 上对齐 |
| `0` | 居中 |
| `> 0` | 右 / 下对齐 |

常用：`.Align(0, 0)` 居中、`.Align(-1, -1)` 左上、`.Align(null, 0)` 横向拉伸+垂直居中。

## 文本与样式

```csharp
TextBlock("标题", 120, SKColors.White)     // text, fontSize, color
    .Font(120, SKColors.White)             // 链式写法
    .LetterSpacing(8)
    .Align(0, 0);

Text("等价于 TextBlock");

RichText([
    TextRun("普通", 80, SKColors.Black),
    TextRun("强调", 100, SKColors.Orange),
]);
```

- `textAlign`：`-1` 左、`0` 中、`1` 右；
- 全局字体可用 `font("字体名")`；
- `Styles(params Action<NBVisual>[])` 复用样式；
- 在 style 里用 `v.As<NBText>()` 取具体类型；
- 文字描边：`.AddStroke(color, width)` 可叠加多层。

```csharp
var titleStyle = (NBVisual v) =>
{
    v.As<NBText>()?.FontSize(120).Foreground(SKColors.White).Align(0, 0);
};

TextBlock("标题").Styles(titleStyle);
```

## 动画与特效

### 逐帧回调

```csharp
TextBlock("淡入")
    .Opacity(0)
    .OnFrame(e =>
    {
        e.Sender.Opacity(e.p);                              // 0..1 进度
        e.Sender.RenderTransform(SKMatrix.CreateScale(1 + e.p, 1 + e.p));
    });
```

- `e.p`：当前 clip 内进度 `0..1`；
- `e.pf`：按帧推进的进度；
- `e.frame`：当前帧；
- `e.Sender`：触发组件；`e.SenderAs<NBText>()` 取具体类型；
- `Easing.SineInOut / BackIn / ...` 做缓动。

### 视觉特效

NBVisual 渲染顺序：`Content → BitmapFilters → FrameMask → Filters/ColorFilters/Opacity/RenderTransform`。

- `Shaders(...)`：用 Shader 直接替换 Content；
- `BitmapFilters(...)`：对渲染后的位图做处理（可改变尺寸）；
- `FrameMask(...)`：按蒙版裁剪；
- `Filters(...)`：Skia 图像滤镜（模糊、阴影、锐化…）；
- `ColorFilters(...)`：调色（灰度、亮度、对比度、色相…）；
- `Opacity(...)` / `RenderTransform(...)`：透明度与矩阵变换。

```csharp
Rect(400, 200)
    .Shaders(Shaders.LinearGradientOnRect(
        [SKColors.Red, SKColors.Green, SKColors.Blue], [0, 0.5f, 1]))
    .ColorFilters(ColorFilters.Saturation(1.5f))
    .Filters(Filters.Blur(4, 4))
    .Opacity(0.9)
    .Align(0, 0)
    .AsClip(30, name: "effect");
```

## 嵌入动画组件

`SkiaAnim`、`PngAnim`、`LottieAnim`、`TypstAnim` 都返回可视节点，可直接放进任何布局容器：

| 工厂 | 示例 |
| --- | --- |
| `SkiaAnim` | `SkiaAnim(new MyAnimation(600, 400, 90, content))` |
| `PngAnim` | `PngAnim("./assets/images/icon-flow.png")` |
| `LottieAnim` | `LottieAnim("./assets/lottie/loading.json")` |
| `TypstAnim` | `TypstAnim("./assets/typst/formula.typ")` |

```csharp
// 组合多个动画：主图 + 公式 + 图标
HGrid("*,*", [
    SkiaAnim(new MyAnimation(600, 400, 90, content)),
    VStack([
        TypstAnim("./assets/typst/formula.typ"),
        PngAnim("./assets/images/icon-flow.png").Size(240, 240),
    ]).Spacing(20),
]).AsClip(90, name: "clip1");
```

- 节点尺寸：`SkiaAnim` 取自动画 `Width` / `Height`，`PngAnim` 取自 APNG 第一帧，`LottieAnim` 取自 Lottie 画布，`TypstAnim` 取自第一页；布局会把节点映射到分配区域；
- 四种动画都默认 `LoopMode = Stop`（播放一次并停在最后一帧）；需要循环用 `.LoopMode(NBAnimateLoopMode.Loop)`，往复用 `.AutoReverse`，调速用 `.Speed(...)`；
- 动画帧号来自当前 clip 的 `NBDrawContext.Current.frame`；同一 clip 内的多个动画共享这个帧号；
- 需要各自独立时间轴时，把动画拆成不同 clip，用 `start` 叠加或错开；
- `SkiaAnim` 可共享同一个 `ISKAnimation` 实例，但建议每个节点用独立实例，避免动画内部状态互相影响；
- `SkiaAnim` 需要透明背景时，动画内用 `canvas.Clear(SKColors.Transparent)`，不要填不透明底色。

## run：预览与导出

最新签名（以源码注释为准）：

```csharp
public static int run(
    NBStage stage,
    IList<NBClip> clips,
    IList<NBClip>[]? clipLists = null,
    string[]? args = null)
```

`args` 不同长度用途不同：

| `args` | 行为 | 返回值 |
| --- | --- | --- |
| `null` / 空 | 打开 Avalonia 预览窗口，阻塞到窗口关闭 | `-1` |
| 1 个元素：`[mp4Path]` | 导出 MP4（文件已存在会拒绝覆盖，依赖 ffmpeg） | 总帧数 |
| ≥2 个元素：`[frame, pngPath]` | 只渲染指定帧并保存 PNG | 总帧数 |
| 帧号不是整数 | 打印错误，不渲染 | `-1` |

```csharp
// file-based app 顶层：透传命令行参数
run(stage(1920, 1080, bg: SKColors.Black), clips(), args: args);

// dotnet run                      → 预览
// dotnet run -- out.mp4           → 导出 MP4
// dotnet run -- 42 out.png        → 导出第 42 帧 PNG
```

补充：

- 没有可导出的内容时返回 `0`；
- PNG 输出目录会自动创建；
- 帧号越界只打印提示，仍会尝试渲染；
- `clipLists` 用于把多批 clip 合并进同一条轨道：`run(stage, main, [overlays, captions], args)`；
- 预览模式会启动桌面 UI，必须在主线程调用。

## 常见坑

- **不要使用 `NBLayer` / `Layer(...)`**：已废弃。用 `Panel` + `FrameMask` / `Shaders` / `BitmapFilters` 实现类似效果。
- `Align(null, ...)` 是拉伸，不是居中；居中用 `0`。
- `frames <= 0` 是“持续到结尾”，不是“0 帧”。
- 不传 `start` 时自动接在前一个 clip 后；要叠加请显式传 `start`。
- 每个 clip 建议用 `{ }` 隔离变量。
- `OnFrame` 中修改属性即可，不要在里面创建未释放的 `SKPaint` / `SKBitmap`。
- 特效顺序会影响结果：需要先裁剪再模糊，还是先模糊再裁剪，要按渲染顺序设计。
- 导出 MP4 前确认目录存在（`save` 不会创建目录）。

## 完成前检查

1. `stage` 的宽高、帧率、背景色是否符合目标；
2. 每个 clip 的 `frames` / `start` 是否正确，装饰层是否用 `-1`；
3. 布局容器与 `Align` 是否达到预期（居中 / 拉伸 / 靠边）；
4. 文字字号、颜色、对齐、字体是否适配画幅；
5. 动效是否使用 `e.p` / `e.pf` / `Easing`，是否误用绝对帧号；
6. 动画选型是否匹配内容（复杂特效/数据图形 → `SkiaAnim`，简单矢量 → `PngAnim`，数学/科学排版 → `TypstAnim`；`LottieAnim` 仅在已有可靠素材时使用）；
7. 动画实例尺寸与布局区域是否匹配，是否需要透明背景（仅 `SkiaAnim`）；
8. 自创 `SkiaAnim` 是否保存到脚本目录的 `modules/` 并更新 `modules/README.md`；APNG / Lottie / Typst 素材是否保存到脚本目录的 `assets/` 下；
9. 是否避免使用已废弃的 `NBLayer`；
10. `run(...)` 的 `args` 模式是否与当前需求一致（预览 / MP4 / PNG）；
11. 文件顶部是否有 `#!`，`#:include` 路径是否正确，类声明是否在顶层语句之后；
12. 音频是否用 `AudioFile(...).AsClip("audio").LinkTo("目标clip", offset)`，目标 clip 是否已先创建并登记名称；
13. 是否按 `./assets` → 父目录 → 项目根目录的顺序找到 assets，并优先阅读 `README.md`；
14. 缺失素材是否优先用 SVG 生成并保存到脚本目录的 `assets/svgs`（图片/音频/视频/动画放对应子目录），目录是否已创建；
15. 每个 clip 是否组合了布局组件、多个动画与素材，而不是只用一个全屏动画。

## 资源

- `references/assets.md`：assets 查找顺序、README 约定、素材盘点与 SVG 生成流程、资源引用代码。
- `references/concepts-and-run.md`：概念、clip 时间规则、`AsClip`、音频 `LinkTo`、`clips()`、`run` 参数与返回值细节。
- `references/layout.md`：六种布局容器、Grid 尺寸语法、`Align` 全取值与排版示例。
- `references/texts-and-style.md`：`NBText` / `NBRichText` / `TextRun`、`Styles`、逐帧动画与缓动。
- `references/effects.md`：渲染流程、Shaders、BitmapFilters、FrameMask、Filters、ColorFilters、Opacity/RenderTransform。
- `references/widgets.md`：布局、内容、形状组件目录（不含 NBLayer）。
- `assets/video-template.cs`：可直接运行的 SkiaAnim + 布局 + run 模板；可把其中节点替换为 `PngAnim` / `TypstAnim`。
