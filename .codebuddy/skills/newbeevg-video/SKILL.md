---
name: newbeevg-video
description: This skill should be used when building, editing, or debugging a NewBeeVG video project with stage/clip/layout/text/effects/export, especially in dotnet file-based apps. It covers assets discovery (README + audios/images/videos/svgs), SVG asset generation for missing material, AsClip timing, HGrid/VGrid/HStack/VStack/Panel/WrapPanel layout, Align semantics, NBText/NBRichText/styles, Shaders/BitmapFilters/FrameMask/Filters/ColorFilters/Opacity/RenderTransform, the widget catalog except deprecated NBLayer, embedding SKCustomAnimation via SkiaAnim, audio clips via AudioFile/LinkTo, and run(...) modes (preview, MP4 export, single-frame PNG export).
---

# NewBeeVG 视频制作

## 用途

用 NewBeeVG 的 markup + clip 时间线构建视频：用布局组件描述每一帧画面，用 `AsClip` 把画面变成有起止帧的片段，最后用 `run(...)` 预览或导出。

推荐组合：**布局组件（排版）+ SkiaAnim（动态图形）+ assets 素材（背景/图标/图片/SVG）**。

不适用：深入 3D、Typst、TTS 等专项能力时，再查对应专项文档；本技能不覆盖已废弃的 `NBLayer`。

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
- 仓库外运行需在文件顶部加 `#:package NewBeeVG@1.0.1.24`；仓库内由 `Directory.Build.props` 提供项目引用，出现重复引用时删除该指令。
- **首字母大写的方法**是界面/布局/样式方法（`TextBlock`、`HStack`、`Align`、`Background`…）；
- **首字母小写的方法**是行为方法（`stage`、`run`、`font`、`clips`、`skanim`…）。

## 资源目录

`assets` 是资源根目录，按类型组织：

```text
assets/
├── audios/
├── images/
├── videos/
├── svgs/
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
2. **读取 assets 与 README**：标记已有素材，列出缺失素材；
3. **生成缺失素材**：优先使用大模型的 SVG 生成能力，产出静态 SVG 或带 SMIL/CSS 动画的 SVG；
4. **保存素材**：
   - SVG → `assets/svgs/`
   - 图片 → `assets/images/`
   - 音频 → `assets/audios/`
   - 视频 → `assets/videos/`
   - 目录不存在时先创建；
5. **登记**：在 `assets/README.md` 中补充文件名、用途、尺寸/时长，方便后续复用。

生成 SVG 时：

- 画布尺寸与使用位置匹配，避免运行时大幅缩放；
- 图标/插画使用清晰的分层与语义化命名；
- 需要动效时优先用 SMIL（`animate` / `animateTransform`）或内联 CSS 动画；
- 颜色尽量与视频主题色一致；
- 避免外部字体、外链图片和脚本，保证 SVG 自包含。

## 组合原则

制作每个 clip 时，尽量**组合布局组件、SkiaAnim 和素材**，不要用单一的全屏 SkiaAnim 构成整个 clip：

```csharp
// 推荐：背景素材 + 布局分栏 + SkiaAnim + 文字
Panel([
    Image("./assets/images/bg.png").Size(1920, 1080).Stretch(Stretch.UniformToFill),

    HGrid("480,*", [
        SVG("./assets/svgs/icon-chart.svg").Size(320, 320).Align(0, 0),

        HStack([
            SkiaAnim(new ChartAnimation(600, 800, 90, content)),
            VStack([
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
- **SkiaAnim** 负责数据驱动的图形、数字滚动、图表、程序化动效；
- **assets** 提供背景、照片、图标、插画、音视频；
- 一个 clip 内通常至少包含两类元素（例如 SkiaAnim + 文字，或素材 + 布局 + SkiaAnim）；
- 片头/片尾可用纯文字布局；正文与数据镜头应避免“整个画面只有一个 SkiaAnim”。

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

## 嵌入 SkiaAnim

`SkiaAnim` 把 `ISKAnimation` 包装成 `NBSkiaAnimation`（可视节点），可直接放进任何布局容器：

```csharp
var anim = new MyAnimation(600, 400, 90, content);

HGrid("*,*", [
    SkiaAnim(anim),
    SkiaAnim(anim2),
]).AsClip(90, name: "clip1");
```

- 节点尺寸来自动画的 `Width` / `Height`，布局会把它映射到分配区域；
- 默认循环播放；需要往复时设置 `LoopMode`；
- 动画帧号来自当前 clip 的 `NBDrawContext.Current.frame`；
- 同一个动画实例可被多个节点共享，但建议每个节点用独立实例，避免状态互相影响；
- 需要透明背景时，动画内用 `canvas.Clear(SKColors.Transparent)`，不要填不透明底色。

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
6. SkiaAnim 实例尺寸与布局区域是否匹配，是否需要透明背景；
7. 是否避免使用已废弃的 `NBLayer`；
8. `run(...)` 的 `args` 模式是否与当前需求一致（预览 / MP4 / PNG）；
9. 文件顶部是否有 `#!`，`#:include` 路径是否正确，类声明是否在顶层语句之后；
10. 音频是否用 `AudioFile(...).AsClip("audio").LinkTo("目标clip", offset)`，目标 clip 是否已先创建并登记名称；
11. 是否按 `./assets` → 父目录 → 项目根目录的顺序找到 assets，并优先阅读 `README.md`；
12. 缺失素材是否优先用 SVG 生成并保存到脚本目录的 `assets/svgs`（图片/音频/视频放对应子目录），目录是否已创建；
13. 每个 clip 是否组合了布局组件、SkiaAnim 与素材，而不是只用一个全屏 SkiaAnim。

## 资源

- `references/assets.md`：assets 查找顺序、README 约定、素材盘点与 SVG 生成流程、资源引用代码。
- `references/concepts-and-run.md`：概念、clip 时间规则、`AsClip`、音频 `LinkTo`、`clips()`、`run` 参数与返回值细节。
- `references/layout.md`：六种布局容器、Grid 尺寸语法、`Align` 全取值与排版示例。
- `references/texts-and-style.md`：`NBText` / `NBRichText` / `TextRun`、`Styles`、逐帧动画与缓动。
- `references/effects.md`：渲染流程、Shaders、BitmapFilters、FrameMask、Filters、ColorFilters、Opacity/RenderTransform。
- `references/widgets.md`：布局、内容、形状组件目录（不含 NBLayer）。
- `assets/video-template.cs`：可直接运行的 SkiaAnim + 布局 + run 模板。
