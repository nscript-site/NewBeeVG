---
name: newbeevg-video
description: This skill should be used when building, editing, or debugging a NewBeeVG video project with stage/clip/layout/text/effects/export, especially in dotnet file-based apps. It covers the full pipeline from a Markdown script to `*_storyboard.md`, per-shot TTS + `assets/audios/audio_timeline.md`, global theme/font header variables, assets discovery (README + audios/images/videos/svgs), choosing and composing SkiaAnim/PngAnim/LottieAnim/TypstAnim (prefer APNG, Typst, or existing Skia modules; create new SkiaAnim only as a fallback), SVG-to-APNG conversion (final filename uses .png), AsClip timing, HGrid/VGrid/HStack/VStack/Panel/WrapPanel layout, Align semantics, NBText/NBRichText/styles, Shaders/BitmapFilters/FrameMask/Filters/ColorFilters/Opacity/RenderTransform, the widget catalog except deprecated NBLayer, embedding SKCustomAnimation via SkiaAnim, audio clips via AudioFile/LinkTo, and run(...) modes (preview, MP4 export, single-frame PNG export).
---

# NewBeeVG 视频制作

## 用途

用 NewBeeVG 的 markup + clip 时间线构建视频：用布局组件描述每一帧画面，用 `AsClip` 把画面变成有起止帧的片段，最后用 `run(...)` 预览或导出。

推荐组合：**布局组件（排版）+ 动画（SkiaAnim / PngAnim / LottieAnim / TypstAnim）+ assets 素材（背景/图标/图片/SVG）**。

不适用：深入 3D 或特定 TTS 引擎参数时，再查对应专项文档；本技能不覆盖已废弃的 `NBLayer`。

## 完整视频制作流程

生成视频时按以下四步执行，不要跳步。稿件、分镜和 C# 脚本放在同一目录；语音及时间表放在该目录的 `assets/audios/`。

### 1. 读取 Markdown 稿件

稿件是唯一输入源，文件名形如 `xxxx.md`。先完整读取并提取：

- **视频内容**：口播/旁白、标题、字幕、要点、数据、引用；
- **视频要求**：画幅、时长、帧率、主题、字体、语气、节奏、目标平台；
- **已有素材**：图片、视频、音频、SVG、`modules/` 中的 Skia 模板及其引用路径。

要求缺失时采用默认值，并在分镜中标注：`1920×1080`、`25fps`、`NBVideoThemes.Nature`、可用中文字体。不要自行改写口播事实或数据。

### 2. 拆分分镜并保存 `xxxx_storyboard.md`

在同目录创建 `xxxx_storyboard.md`（`xxxx` 与稿件同名），先写总表，再写每个镜头的详情：

```markdown
# 《视频标题》分镜

- 源稿件：xxxx.md
- 画幅：1920×1080
- 帧率：25 fps
- 主题：NBVideoThemes.Nature
- 字体：阿里巴巴普惠体 2.0
- 总时长：约 60 秒

| 镜头 | 口播摘要 | 画面 | 素材/动画 | 时长 | 音频 |
| --- | --- | --- | --- | --- | --- |
| shot01 | ... | ... | ... | 3.4s | shot01.mp3 |

## 镜头 01 · shot01

- 口播：...
- 画面：...
- 构图/布局：...
- 素材/动画：...
- 字幕：...
- 时长：3.4 秒（85 帧）
- 转场：...
- 音频：./assets/audios/shot01.mp3
```

分镜要求：

- 每个镜头使用唯一 `shotNN`，与音频文件名、C# clip 名一一对应；
- 口播按语义断句，一个镜头只表达一个完整意思；
- 画面写清主体、构图、文字层级、动效、素材路径与背景；
- 时长先按语速估算，TTS 后按实际音频时长校正；
- 所有镜头时长之和与稿件总时长一致，误差控制在 1 秒内；
- 主题、字体、画幅、帧率在总表固定，后续不要逐镜头变化。

### 3. 合成语音并记录入点/出点

按分镜逐条合成语音，保存到 `./assets/audios/`：

```text
assets/audios/
├── shot01.mp3
├── shot02.mp3
├── ...
└── audio_timeline.md
```

- 音频文件名与分镜 `shotNN` 一致，优先 mp3/wav；
- 音色、语速、语言从稿件要求读取；缺失时全片统一默认，不逐镜头变化；
- 生成后用 `ffprobe` 读取实际时长，按分镜顺序累加时间轴；
- 在 `assets/audios/audio_timeline.md` 记录每条语音的入点/出点：

```markdown
| 分镜 | 音频 | 入点(s) | 出点(s) | 时长(s) | 画面 clip |
| --- | --- | --- | --- | --- | --- |
| shot01 | shot01.mp3 | 0.00 | 3.40 | 3.40 | shot01 |
| shot02 | shot02.mp3 | 3.40 | 7.20 | 3.80 | shot02 |
```

- 入点 = 该镜头在成片时间轴上的开始时间；出点 = 入点 + 实际语音时长；
- 片头、片尾、镜头间停顿都要计入时间轴；
- 需要停顿时直接调整入点，不要用静音文件拼接；
- 记录总时长，供 C# 脚本换算帧数。

读取单个音频时长：

```bash
ffprobe -v error -show_entries format=duration -of default=nw=1:nk=1 ./assets/audios/shot01.mp3
```

### 4. 生成 C# 单文件视频脚本

在稿件同目录生成 `xxxx.cs`（与稿件同名），使用 dotnet file-based app。以分镜和 `audio_timeline.md` 为准：

1. **文件头部**声明全局主题与字体：
   ```csharp
   var theme = NBVideoThemes.Nature;
   var fontName = "阿里巴巴普惠体 2.0";
   font(fontName);
   ```
2. **先画面、后音频**：每个镜头先创建画面 clip 并登记 `name`，再创建对应音频并 `LinkTo`；
3. **帧数换算**：`frames = (int)Math.Ceiling((出点 - 入点) * fps)`，建议额外加 2–4 帧尾部留白，避免语音被硬切；
4. **音频对齐**：顺序镜头直接用 `AudioFile("./assets/audios/shotNN.mp3").AsClip("audio_NN").LinkTo("shotNN")`；非顺序或需要偏移时，按时间表计算 `offset = 入点帧 - clip.StartFrame`；
5. **素材复用**：先查 `assets/` 和 `modules/README.md`，能用则复用；缺失素材按 SVG → APNG / Typst → 新建 SkiaAnim 的优先级补齐，避免每个镜头各用一套风格；
6. **设计一致性**：统一栅格、安全边距、字号层级（标题/副标题/正文/注释）、主色与装饰色；每镜头只保留一个视觉中心，转场克制，字幕不遮挡主体；
7. **收尾**：`run(stage(...), clips(), args: args)`，确保预览、MP4、单帧 PNG 三种模式都能运行。

```csharp
#!/usr/bin/env dotnet

// ===== 全片主题与字体：只在这里修改 =====
var theme = NBVideoThemes.Nature;
var fontName = "阿里巴巴普惠体 2.0";
font(fontName);

const int FPS = 25;

// shot01：先创建画面 clip
{
    var frames = (int)Math.Ceiling(3.40 * FPS) + 2;

    HStack([
        TextBlock("第一句口播").Font(96, theme.TextPrimary).Align(0, 0),
    ]).Align(0, 0)
      .AsClip(frames, start: 0, name: "shot01");
}

// 再创建音频并链接到已登记的 shot01
AudioFile("./assets/audios/shot01.mp3")
    .AsClip("audio_01").LinkTo("shot01");

// shot02 同理，start 可省略以自动接续
// ...

run(stage(1920, 1080, bg: theme.Background), clips(), args: args);
```

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
- **全局主题与字体必须写在文件头部**：用 `var theme = NBVideoThemes.XXX;`、`var fontName = "字体名";` 和 `font(fontName);` 各声明一次，后续 clip 只引用 `theme.*` 与全局字体。

## 全局主题与字体（必须）

每个视频脚本都要在文件头部集中设置全片主题和字体，只声明一次，后续所有 clip 复用：

```csharp
#!/usr/bin/env dotnet

// ===== 全片主题与字体：只在这里修改 =====
var theme = NBVideoThemes.Nature;
var fontName = "阿里巴巴普惠体 2.0";
font(fontName);

// 可选：提取语义色变量，避免片段内重复 theme.XXX
var bg = theme.Background;
var primary = theme.Primary;
var textPrimary = theme.TextPrimary;
var textSecondary = theme.TextSecondary;
```

规则：

- `font(fontName)` 必须在创建任何 `TextBlock` / `Text` / `RichText` 之前调用，位置固定在文件头部；
- 主题使用 `NBVideoThemes` 的内置主题（如 `Tech`、`Nature`、`Finance`），不要在片段内另建零散配色；需要微调时先复制成新的 `NBVideoTheme` 再改，不要直接修改共享的静态主题实例；
- 片段内通过 `theme.Primary`、`theme.Background`、`theme.TextPrimary`、`theme.TextSecondary`、`theme.Divider`、`theme.Positive`、`theme.Negative` 取色；
- `stage` 背景直接用 `bg: theme.Background`；
- `SkiaAnim` 模块需要主题时，把全局 `theme` 传给主题参数（如 `ContentTheme.From(theme)`），不要另传硬编码颜色；
- 不要在多个 clip 里重复调用 `font(...)`，也不要在片段内写死本应由主题决定的颜色；确需装饰色时从 `theme` 派生或集中定义。

> 后文与参考文档中的短片段为突出 API 结构可能直接写颜色字面量；生成完整视频脚本时必须改成文件头部的 `theme.*` 变量。

## 资源目录

`assets` 是资源根目录，按类型组织：

```text
assets/
├── audios/       # 分镜语音 + audio_timeline.md
├── apngs/        # PngAnim 使用的 APNG 内容，后缀统一为 .png
├── images/      # 普通静态图片
├── videos/
├── svgs/
├── lottie/      # LottieAnim 的 JSON
├── typsts/      # TypstAnim 的 .typ 及依赖
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
3. **按优先级选择动画实现**：先看现有 `modules/` 模板，能用则复用；否则优先生成 APNG（简单矢量）或 Typst（数学/科学/复杂排版）；以上都实现不了或效果不好，才新建 `SkiaAnim`。Lottie 仅作已有可靠素材的兜底；
4. **生成缺失素材**：优先使用大模型的 SVG 生成能力，产出静态 SVG 或带 SMIL/CSS 动画的 SVG；简单动画再转成 APNG；
5. **保存素材**：
   - SVG → `assets/svgs/`
   - 普通图片 → `assets/images/`
   - APNG → `assets/apngs/`
   - Lottie → `assets/lottie/`
   - Typst → `assets/typsts/`
   - 分镜语音 → `assets/audios/`，同时维护 `audio_timeline.md`
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

`SkiaAnim`、`PngAnim`、`LottieAnim`、`TypstAnim` 都会返回可视节点，可直接放进任意布局容器。

**生成动画素材时，优先使用 APNG、Typst，或直接复用现有 `modules/` 中的 Skia 模板；这三者都无法实现或效果不好时，才编写新的 Skia 动画模板。** 不要一上来就新建 `SkiaAnim`。

优先方案（按素材类型选择，不是硬顺序）：

1. **现有 Skia 模板**：先读 `modules/README.md`，有合适特效直接 `#:include` 复用；
2. **APNG**：简单矢量、图标、路径、进度、描边、形变，用 SVG → `svg-animation-convert`；
3. **Typst**：数学公式、科学示意图、函数图、复杂表格与排版，用 Typst 多页文档；
4. **新建 Skia**：只有上述方案无法实现或效果明显不好时，才用 `newbeevg-skia-animation` 新建；
5. **Lottie**：仅在已有可靠素材时使用，不让 AI 从零生成。

| 工厂 | 适用场景 | 素材来源 |
| --- | --- | --- |
| `SkiaAnim(ISKAnimation)` | 复杂动画、数据图形、数字滚动、图表、程序化特效 | 优先复用 `modules/` 现有模板；没有合适模板且 APNG/Typst 做不了时再新建 |
| `PngAnim(path)` | 画面简单、矢量插画、SMIL/CSS 动画 | 大模型生成 SVG，再转成 APNG；最终文件后缀用 `.png` |
| `LottieAnim(path)` | Lottie JSON 动画 | 大模型可生成，但质量较差，不推荐 |
| `TypstAnim(path)` | 数学、科学、矢量图表、复杂排版 | Typst + CeTZ 多页文档 |

### `SkiaAnim`：复杂动画与程序化特效

适合需要逐帧计算、数据驱动或精细控制的动画，例如数字增长、图表生长、粒子、路径描边、转场特效。

**默认先复用现有模板；只有在 APNG、Typst 和现有模板都无法满足时，才新建 `SkiaAnim`。**

1. **先找预制特效**：项目目录的 `modules/` 通常放有现成的 `SkiaAnim` 特效，先读 `modules/README.md`，按介绍选择；找到后用 `#:include modules/Xxx.cs` 引入并直接 `SkiaAnim(new Xxx(...))`；
2. **确认是否真的需要新建**：简单矢量动效优先用 APNG（`PngAnim`），数学/科学排版优先用 Typst（`TypstAnim`）；这两者和现有模板都做不了，或效果明显不好时，才使用 `newbeevg-skia-animation` 技能创建 `SKCustomAnimation` 子类；
3. **保存位置**：自创特效放在**视频脚本文件所在目录**的 `modules/`，目录不存在时创建；同步在 `modules/README.md` 中补充用途、参数和示例；
4. 简单静态图形不必新建特效，优先用 SVG；需要简单动效时用 SVG 转 APNG（`PngAnim`）。

### `PngAnim`：简单画面与矢量动画

适合扁平插画、图标位移、路径生长、描边、简单形变等矢量场景。流程：

1. 用大模型生成带 SMIL / CSS 动画的自包含 SVG，保存到 `assets/svgs/`；
2. 用 `svg-animation-convert` 转成 APNG（转换器只接受 `.apng` 输出）：

```bash
npx --yes svg-animation-convert --input .\your.svg --output your.apng
```

3. **立即把输出重命名为 `.png`**，再放到 `assets/apngs/`：

```powershell
Move-Item .\your.apng .\assets\apngs\your.png -Force
```

```bash
mv ./your.apng ./assets/apngs/your.png
```

4. 脚本中用 `PngAnim("./assets/apngs/your.png")` 引入。

**最终文件后缀必须是 `.png`，不能是 `.apng`**：APNG 内容放在 `.png` 文件中不影响 NewBeeVG 解码，但 `.apng` 在很多图片查看器/预览器中无法打开。交付前检查 `assets/apngs/` 下没有残留 `.apng`。

无限循环的 SVG 动画必须显式指定转换时长，否则转换可能无法结束或帧数不确定：

```bash
npx --yes svg-animation-convert --input .\your.svg --output your.apng --duration 3
mv ./your.apng ./assets/apngs/your.png
```

- `--duration` 按秒计，取值覆盖一个完整循环或镜头所需时长；
- 控制 APNG 的分辨率与帧数，避免文件过大、解码过慢；
- 转换后先在预览中确认循环衔接与透明区域是否正常；
- 需要完整的 SVG 动画编写、转换参数、APNG 结构校验与优化流程时，使用 `newbeevg-apng-animation` 技能；它提供 `inspect_apng.py --require-png` 检查最终后缀。

### `LottieAnim`：不推荐作为首选

`LottieAnim` 可以加载 Lottie JSON 并包装成可视节点，但**目前大模型生成 Lottie 的能力较差**，容易出现结构错误、图层错乱或动画不符合预期，因此只作为已有可靠素材时的兜底，不推荐让 AI 从零生成。

```csharp
LottieAnim("./assets/lottie/loading.json");
```

### `TypstAnim`：数学、科学与复杂排版

适合公式推导、科学示意图、复杂表格、矢量图表等 Typst 擅长的内容。用 Typst 编写多页文档，配合 CeTZ 等包绘制矢量图，每个页面会成为一帧：

```csharp
TypstAnim("./assets/typsts/derivation.typ");
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
│   ├── apngs/        # PngAnim 的 APNG 内容，后缀统一为 .png
│   ├── audios/      # 分镜语音 + audio_timeline.md
│   ├── images/      # 普通静态图片
│   ├── lottie/      # LottieAnim 的 JSON
│   ├── typsts/      # TypstAnim 的 .typ 及其依赖
│   ├── svgs/        # SVG 源文件
│   └── README.md
└── modules/         # 自创 SkiaAnim 的 .cs 与 README.md
```

- `PngAnim` 的 APNG 放 `assets/apngs/`，`LottieAnim` 放 `assets/lottie/`，`TypstAnim` 放 `assets/typsts/`；目录不存在时创建；
- 自创的 `SkiaAnim` 放在**视频脚本文件所在目录**的 `modules/` 下，目录不存在时创建；
- 新增素材或模块后，同步更新对应的 `README.md`，方便后续复用。

## 组合原则

制作每个 clip 时，尽量**组合布局组件、动画组件和素材**，不要用单一的全屏动画构成整个 clip。四种动画可以组合使用，也可以在同一画面中通过布局嵌入多个动画节点：

```csharp
// 推荐：背景素材 + 布局分栏 + 多个动画 + 文字
// 假设文件头部已声明 theme 与 font
Panel([
    Image("./assets/images/bg.png").Size(1920, 1080).Stretch(Stretch.UniformToFill),

    HGrid("480,*", [
        PngAnim("./assets/apngs/icon-chart.png").Size(320, 320).Align(0, 0),

        HStack([
            SkiaAnim(new ChartAnimation(600, 800, 90, content)),
            VStack([
                TypstAnim("./assets/typsts/formula.typ"),
                TextBlock("季度增长").Font(72, theme.TextPrimary),
                TextBlock("+128%").Font(160, theme.Primary),
            ]).Spacing(20),
        ]).Spacing(40),
    ]).Margin(120),

    TextBlock("数据来源：内部统计").Font(32, theme.TextSecondary)
        .Align(-1, 1).Margin(80),
])
.AsClip(90, name: "chart");
```

- **布局组件**负责分栏、留白、对齐与信息层级；
- **动画组件**：优先 APNG、Typst 或现有 Skia 模板；复杂数据图形才用 `SkiaAnim`，新建 `SkiaAnim` 仅在前三者无法满足时采用；Lottie 仅作兜底；
- **assets** 提供背景、照片、图标、插画、音视频；
- 一个 clip 内通常至少包含两类元素（例如动画 + 文字，或素材 + 布局 + 动画）；
- **同一画面可以嵌入多个动画节点**：把 `SkiaAnim`、`PngAnim`、`TypstAnim` 分别放进 `HGrid` / `HStack` / `Panel` 的不同区域，形成主图 + 辅助图 + 数据卡等组合；
- 同一 clip 内的动画共享当前帧号；需要各自独立时间轴时，把动画拆成不同 clip，用 `start` 叠加或错开；
- 片头/片尾可用纯文字布局；正文与数据镜头应避免“整个画面只有一个全屏动画”。

## 最小视频骨架

```csharp
#!/usr/bin/env dotnet

#:include modules/MyAnimation.cs

// ===== 全片主题与字体：只在这里修改 =====
var theme = NBVideoThemes.Nature;
var fontName = "阿里巴巴普惠体 2.0";
font(fontName);

// 1) 片头
{
    VStack([
        TextBlock("视频演示").FontSize(200).Foreground(theme.TextPrimary)
    ]).Align(0, 0)
      .AsClip(30, name: "start");
}

// 2) 内容：在布局中嵌入 SkiaAnim
{
    var content = new MyAnimation.ContentSetting { From = 0, To = 12580, Format = "N0" };
    int frames = 90;
    var animTheme = MyAnimation.ContentTheme.From(theme);

    HGrid("*,*", [
        SkiaAnim(new MyAnimation(600, 400, frames, content, animTheme)),
        SkiaAnim(new MyAnimation(600, 400, frames, content, animTheme)),
    ]).AsClip(frames, name: "clip1");
}

// 3) 片尾
{
    VStack([
        TextBlock("视频结束").FontSize(200).Foreground(theme.TextPrimary)
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
run(stage(1920, 1080, bg: theme.Background), clips(), args: args);
```

注意：片段用 `{ }` 隔离局部变量，避免 `content` / `frames` 等重名；主题和字体只在文件头部声明一次。

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

每个分镜的语音是独立的 `NBAudioClip`，不绘制画面；文件名、clip 名与分镜 `shotNN` 对应：

```csharp
// 语音从 shot01 的起始帧开始
AudioFile("./assets/audios/shot01.mp3")
    .AsClip("audio_01").LinkTo("shot01");

// 需要延后 10 帧时，offset 为相对 shot01 起点的帧数
AudioFile("./assets/audios/shot02.mp3")
    .AsClip("audio_02").LinkTo("shot02", 10);
```

- `AudioFile(path)` 创建音频 clip，分镜语音统一放在 `./assets/audios/`；
- `.AsClip("audio_NN")` 登记音频并命名（音频 clip 不需要 `frames`）；
- `.LinkTo("shotNN", offset)`：音频起点 = 目标 clip 的 `StartFrame + offset`，`offset` 默认 `0`；
- 目标画面 clip 必须**先创建并登记名称**，否则 `LinkTo` 找不到目标，链接会静默失败；
- 每个镜头的入点/出点记录在 `assets/audios/audio_timeline.md`，`LinkTo` 的偏移要与其一致；
- 画面 clip 的 `frames` 应覆盖实际语音时长；通常按 `ceil(音频秒数 * fps) + 2~4 帧` 设置；
- 音频时长在 `Prepare` 阶段按文件自动计算，`DurationFrames` 会被更新；
- 音频与画面进入同一 track 混音，预览和 MP4 导出都会带音频；
- 路径相对于运行目录。

完整示例：

```csharp
VStack([
    TextBlock("第一句口播").Font(80, theme.Primary).Align(0, 0)
]).Align(0, 0).AsClip(frames: 87, name: "shot01");

VStack([
    TextBlock("第二句口播").Font(80, theme.Primary).Align(0, 0)
]).Align(0, 0).AsClip(frames: 97, name: "shot02");

AudioFile("./assets/audios/shot01.mp3")
    .AsClip("audio_01").LinkTo("shot01");

AudioFile("./assets/audios/shot02.mp3")
    .AsClip("audio_02").LinkTo("shot02");

run(stage(1920, 1080, bg: theme.Background), clips());
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
TextBlock("标题", 120, theme.TextPrimary)     // text, fontSize, color
    .Font(120, theme.TextPrimary)             // 链式写法
    .LetterSpacing(8)
    .Align(0, 0);

Text("等价于 TextBlock");

RichText([
    TextRun("普通", 80, theme.TextPrimary),
    TextRun("强调", 100, theme.Primary),
]);
```

- `textAlign`：`-1` 左、`0` 中、`1` 右；
- 全局字体在文件头部用 `font(fontName)` 设置一次，不要在每段文字里重复指定；
- `Styles(params Action<NBVisual>[])` 复用样式；
- 在 style 里用 `v.As<NBText>()` 取具体类型；
- 文字描边：`.AddStroke(color, width)` 可叠加多层。

```csharp
var titleStyle = (NBVisual v) =>
{
    v.As<NBText>()?.FontSize(120).Foreground(theme.TextPrimary).Align(0, 0);
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
| `PngAnim` | `PngAnim("./assets/apngs/icon-flow.png")` |
| `LottieAnim` | `LottieAnim("./assets/lottie/loading.json")` |
| `TypstAnim` | `TypstAnim("./assets/typsts/formula.typ")` |

```csharp
// 组合多个动画：主图 + 公式 + 图标
HGrid("*,*", [
    SkiaAnim(new MyAnimation(600, 400, 90, content)),
    VStack([
        TypstAnim("./assets/typsts/formula.typ"),
        PngAnim("./assets/apngs/icon-flow.png").Size(240, 240),
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
run(stage(1920, 1080, bg: theme.Background), clips(), args: args);

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
4. 文件头部是否用全局变量设置主题与字体（`var theme = NBVideoThemes.XXX;`、`var fontName = "..."; font(fontName);`），片段内是否复用 `theme.*` 而未硬编码主题色；文字字号、颜色、对齐、字体是否适配画幅；
5. 动效是否使用 `e.p` / `e.pf` / `Easing`，是否误用绝对帧号；
6. 生成动画素材时，是否优先使用 APNG、Typst 或现有 Skia 模板；只有它们都无法实现或效果不好时，才新建 `SkiaAnim`；
7. 动画实例尺寸与布局区域是否匹配，是否需要透明背景（仅 `SkiaAnim`）；
8. 自创 `SkiaAnim` 是否保存到脚本目录的 `modules/` 并更新 `modules/README.md`；APNG / Lottie / Typst 素材是否保存到脚本目录的 `assets/` 下；APNG 是否放在 `assets/apngs/` 且最终后缀为 `.png`，目录下是否残留 `.apng`；
9. 是否避免使用已废弃的 `NBLayer`；
10. `run(...)` 的 `args` 模式是否与当前需求一致（预览 / MP4 / PNG）；
11. 文件顶部是否有 `#!`，`#:include` 路径是否正确，类声明是否在顶层语句之后；
12. 音频是否用 `AudioFile(...).AsClip("audio").LinkTo("目标clip", offset)`，目标 clip 是否已先创建并登记名称；
13. 是否按 `./assets` → 父目录 → 项目根目录的顺序找到 assets，并优先阅读 `README.md`；
14. 缺失素材是否优先用 SVG / APNG / Typst 生成；复杂 Skia 特效是否先查 `modules/`，最后才新建；素材是否保存到脚本目录的 `assets/` 对应子目录，目录是否已创建；
15. 每个 clip 是否组合了布局组件、多个动画与素材，而不是只用一个全屏动画；
16. 是否先产出 `xxxx_storyboard.md`，每个镜头都记录了口播、画面、素材、时长和对应 `shotNN`；
17. 是否逐镜头生成语音到 `assets/audios/`，并在 `audio_timeline.md` 记录入点、出点与总时长；
18. C# 脚本是否按分镜和音频时间表生成，clip 名与 `shotNN` 对齐，先创建画面 clip 再 `LinkTo` 音频。

## 资源

- `references/assets.md`：assets 查找顺序、README 约定、素材盘点与 SVG 生成流程、资源引用代码。
- `references/concepts-and-run.md`：概念、clip 时间规则、`AsClip`、音频 `LinkTo`、`clips()`、`run` 参数与返回值细节。
- `references/layout.md`：六种布局容器、Grid 尺寸语法、`Align` 全取值与排版示例。
- `references/texts-and-style.md`：`NBText` / `NBRichText` / `TextRun`、`Styles`、逐帧动画与缓动。
- `references/effects.md`：渲染流程、Shaders、BitmapFilters、FrameMask、Filters、ColorFilters、Opacity/RenderTransform。
- `references/widgets.md`：布局、内容、形状组件目录（不含 NBLayer）。
- `assets/video-template.cs`：可直接运行的 SkiaAnim + 布局 + run 模板；可把其中节点替换为 `PngAnim` / `TypstAnim`。
