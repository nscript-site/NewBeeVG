---
name: newbeevg-skia-animation
description: This skill should be used when creating, editing, or debugging frame-by-frame animations rendered with SkiaSharp in the NewBeeVG codebase, especially when creating an SKCustomAnimation subclass in .NET file-based apps using the two-file pattern (class file plus a _Test.cs runner with #:include), or when working with ISKAnimation, SKAnimation, skanim(...), SKCanvas frame callbacks, NBVideoTheme/NBVideoThemes color themes, ContentSetting/ContentTheme inner classes, PNG frame export (SavePNG), MP4/GIF export (SaveMp4/SaveGif), frame index to progress conversion, transparency, frame rate, and bitmap disposal.
---

# NewBeeVG SkiaSharp 逐帧动画

## 用途

在 NewBeeVG 中以 SkiaSharp 画布逐帧绘制动画，并输出：

- 单帧或序列 PNG（`SavePNG`）；
- MP4 视频（`SaveMp4`，H.264）；
- GIF 动图（`SaveGif`）。

推荐做法是**使用 .NET file-based app，把动画拆成两个文件**：

| 文件 | 内容 | 要求 |
| --- | --- | --- |
| `MyAnimation.cs` | 继承 `SKCustomAnimation` 的类定义 + 注释（模板类名 `MyAnimation`） | **只有类型声明**，不含 `#!`、不含顶层语句 |
| `MyAnimation_Test.cs` | 运行入口：创建实例、导出预览帧/视频 | 含 `#!`、`#:package`、`#:include MyAnimation.cs` 与顶层语句 |

测试文件的标准开头（`#:include` 后写动画类实际所在的源文件路径）：

```csharp
#!/usr/bin/env dotnet
#:package NewBeeVG@1.0.1.15
// 包含 MyAnimation 所在的源文件路径
#:include MyAnimation.cs

// 在仓库 apps/files 内运行时，Directory.Build.props 已提供 NewBeeVG 项目引用；
// 若出现重复引用或版本冲突，删除上面的 #:package 指令即可。

using NewBeeVG;
using SkiaSharp;
using static NewBeeVG.Methods;

bool debug = true;
```

`debug` 用于切换「AI 测试」与「真实出片」：

- `debug == true`：只执行 AI 测试语句（`SavePNG` / `SaveMp4` / `SaveGif` 等），不调用 `run`，不合成视频；
- `debug == false`：跳过这些测试语句，执行结尾 `run(stage(Width, Height), clips())`，按真实场景出片。

**AI 在编写、运行测试和验证编译期间，必须始终保留 `bool debug = true;`，绝对不要把 `debug` 改成 `false`。** `false` 只由使用者在交付后手动切换；AI 若改成 `false`，会跳过全部测试并触发真实渲染/出片，属于禁止操作。

因此测试文件里的 `SkiaAnim(...).AsClip(...)` **必须是真实场景代码**：不要为了测试写假片段，也不要给它加 `if (debug == true)`。这样 AI 生成的文件交付后，只需把 `debug` 改成 `false`，`run(...)` 就会直接用这些片段出片。

测试内容按场景分组，每个场景用一对大括号隔离局部变量；**AI 测试语句统一加 `if (debug == true)`，`SkiaAnim` 部分不加条件**：

```csharp
// 场景 1：默认主题
{
    var content = new MyAnimation.ContentSetting { From = 0, To = 12580 };
    var anim = new MyAnimation(Width, Height, Frames, content);

    if (debug == true) anim.SavePNG(0, "output/myanimation/frame_000.png");
    if (debug == true) anim.SavePNG(Frames - 1, "output/myanimation/frame_last.png");

    // 实际场景用法：只编译、不执行渲染
    SkiaAnim(new MyAnimation(Width, Height, Frames, content))
        .AsClip(Frames * 5, name: "数字增长");
}
```

测试文件结尾固定为：

```csharp
if (debug == false)
    run(stage(Width, Height), clips());
```

动画类内部再定义两个内部类：

| 内部类 | 作用 |
| --- | --- |
| `ContentSetting` | 承载内容参数：标题、字号、数据、数量等 |
| `ContentTheme` | 当 `NBVideoTheme` 的 7 个基础颜色不够用时，继承它并补充专用颜色 |

推荐构造签名：

```csharp
public YourCustomAnimation(
    int width,
    int height,
    int frames,
    ContentSetting content,
    ContentTheme? contentTheme = null)
```

- `contentTheme` 为 `null` 时，使用 `ContentTheme.CreateDefault()`（从 `NBVideoThemes.Nature` 复制的一套完整配色）；
- 需要基于内置主题扩展时，使用 `ContentTheme.From(NBVideoThemes.Ocean)`。

该动画体系与基于 Avalonia 布局的 `NBVisual` / `NBClip` 动画相互独立：它只操作原始 `SKCanvas`，不参与布局计算。

## 何时使用

在以下场景加载本技能：

- 需要在 file-based app 中创建逐帧动画，且把动画类与测试入口分成两个文件；
- 需要为动画选择或切换主题配色（科技、环保、金融等 23 套内置主题）；
- `NBVideoTheme` 现有颜色不够用，需要额外的专用颜色；
- 需要从 `SKCanvas` 渲染结果导出 PNG 或 PNG 序列；
- 需要直接导出 MP4 / GIF，或排查导出失败、帧率不准、尺寸被压缩等问题；
- 需要排查 `BuildFrame` 返回 `null`、透明背景丢失、逐帧导出内存增长等问题。

不适用：需要文本排版、Grid/StackPanel 布局、Typst/Lottie 等 NewBeeVG 组件能力时，应使用 `NBClip` / `NBVisual` 动画方案。

## 先确认的参数

开始编写代码前，明确以下信息，缺失时向用户确认：

1. 动画类名与源文件名（模板为 `MyAnimation.cs` / 类 `MyAnimation`，测试文件 `MyAnimation_Test.cs`）；
2. 输出尺寸（像素宽高）；
3. 总帧数 `frames`，或时长 + 帧率（`frames = 时长 × fps`）；
4. 是否需要透明背景；
5. 配色方案：内置主题是否够用；不够用时需要补充哪些颜色；
6. 输出形态：单张 PNG、PNG 序列、MP4，还是 GIF；
7. 输出目录是否已存在（`SavePNG` 会自动创建，`SaveMp4` / `SaveGif` 不会）。

## 工作流

### 1. 创建动画类文件（如 `MyAnimation.cs`）

规则：

- 文件名即测试文件 `#:include` 的路径；模板中类名为 `MyAnimation`、文件名为 `MyAnimation.cs`；
- 类名与文件名尽量一致；不完全一致时，务必保证 `#:include` 指向该类实际所在的源文件；
- **文件中不要写 `#!` 与顶层语句**，只放 `using`、类型声明和注释；
- 类声明继承 `SKCustomAnimation`，重写 `protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)`；
- 构造函数调用 `base(width, height, frames, contentTheme ?? ContentTheme.CreateDefault())`。

C# 12 / .NET 10 支持主构造函数，但这里仍建议显式写构造函数，便于校验 `content` 与做字段赋值。

### 2. 创建测试文件 `MyAnimation_Test.cs`

规则：

- 必须以 `#!/usr/bin/env dotnet` 开头；
- `#:package NewBeeVG@1.0.1.15` 用于脱离仓库时引入 NewBeeVG；在仓库的 `apps/files/` 内，`Directory.Build.props` 已提供项目引用，出现重复引用时删除该指令；
- `#:include` 后写**所实现动画类的源文件路径**（模板为 `MyAnimation.cs`），路径相对于测试文件；
- `using` 至少包含 `NewBeeVG`、`SkiaSharp`、`using static NewBeeVG.Methods;`（`SkiaAnim` 是 `Methods` 的静态方法）；
- 在 `using` 之后声明 `bool debug = true;`，并且**AI 不得把它改为 `false`**（`false` 由使用者交付后手动切换）；
- **每个测试场景用一对大括号 `{ }` 包起来**，隔离 `content` / `anim` / `theme` 等局部变量，避免不同场景重名；
- **AI 测试语句统一加 `if (debug == true)` 前缀**，例如 `if (debug == true) anim.SavePNG(...)`；这样 `debug = false` 时只走正式出片；
- 每个场景末尾写**真实场景**的 `SkiaAnim(new Xxx(...)).AsClip(frames, name: "...")`，且不要加 `debug` 条件：`debug = true` 时它只完成编译与片段注册（不 `run`），`debug = false` 时由结尾 `run(...)` 出片；
- 顶层只保留共享常量（`Width` / `Height` / `Frames`）、`debug` 与各场景块，不要在块外声明场景专用变量；
- **文件结尾固定写**：

```csharp
if (debug == false)
    run(stage(Width, Height), clips());
```

### 3. 选择主题色

`Theme` 是 `NBVideoTheme` 类型，包含 `Background`、`TextPrimary`、`TextSecondary`、`Primary`、`Divider`、`Positive`、`Negative`、`Name`。

内置 23 套主题位于 `NBVideoThemes`：

```csharp
var baseTheme = NBVideoThemes.AI;                             // 按静态属性取
var contentTheme = MyAnimation.ContentTheme.From(baseTheme);   // 转成可扩展主题

var anim = new MyAnimation(1920, 1080, 120, content, contentTheme);
```

属性名包括 `Tech`、`Eco`、`Finance`、`Healthcare`、`Education`、`Media`、`Retail`、`Nature`、`Ocean`、`Sunset`、`Minimal`、`Cyberpunk`、`Luxury`、`Retro`、`Candy`、`Arctic`、`Forest`、`Corporate`、`AI`、`Podcast`，以及后加的 `NightCopper`（夜墨铜金）、`ParchmentDay`（羊皮纸日间）、`ColdArchive`（冷青夜航）。

当这些颜色不够用时，在 `ContentTheme` 中补充属性，例如：

```csharp
public class ContentTheme : NBVideoTheme
{
    public SKColor Grid { get; set; }        // 网格线
    public SKColor Highlight { get; set; }   // 第二强调色
    public SKColor Warning { get; set; }     // 警告色
    public SKColor Shadow { get; set; }      // 阴影色
}
```

### 4. 帧号换算进度

`frame` 范围是 `0 ~ Frames-1`。按用途选择分母：

```csharp
// 一次性动画：最后一帧恰好为 1.0
float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

// 循环动画：避免首尾帧重复
float t = frame / (float)Frames;
```

### 5. 先验证单帧

先保存中间帧检查构图与颜色，再批量导出：

```csharp
anim.SavePNG(0, "output/preview.png");
```

`SavePNG` 会自动创建目标目录。

### 6. 导出 PNG 序列

```csharp
for (int i = 0; i < anim.Frames; i++)
{
    if (!anim.SavePNG(i, $"output/frames/{i:0000}.png"))
        Console.WriteLine($"第 {i} 帧渲染失败");
}
```

使用 4 位补零命名，便于 ffmpeg 或后续工具按序号读取。

### 7. 直接导出 MP4 / GIF

```csharp
Directory.CreateDirectory("output"); // SaveMp4 / SaveGif 不自动建目录

// MP4：H.264，时长 = Frames / fps
anim.SaveMp4(30, "output/animation.mp4");

// GIF：帧率建议使用 10/20/25/50，输出尺寸超过 1200 像素会被等比缩小
anim.SaveGif(25, "output/animation.gif");
```

`SaveMp4` 会先调用 `NBGlobal.CheckOrLoadFFmpeg()` 加载 ffmpeg。

### 8. 手动合成（可自定义编码参数）

需要控制编码器参数时，先导出 PNG 序列再调用 ffmpeg：

```bash
ffmpeg -framerate 30 -i output/frames/%04d.png \
       -c:v libx264 -pix_fmt yuv420p -crf 18 \
       output/animation.mp4
```

## API 摘要

| 成员 | 说明 |
| --- | --- |
| `SKCustomAnimation(int width, int height, int frames, NBVideoTheme? theme = null)` | 推荐基类；子类重写 `DrawFrame`；`theme` 为空时使用 `NBVideoThemes.Nature` |
| `YourCustomAnimation(int width, int height, int frames, ContentSetting content, ContentTheme? contentTheme = null)` | 推荐子类签名；`ContentSetting` 承载内容，`ContentTheme` 扩展颜色 |
| `ContentTheme : NBVideoTheme` | 主题颜色不够用时继承并补充属性；`From(theme)` 复制内置主题，`CreateDefault()` 复制自然主题 |
| `Theme` | 当前主题（`NBVideoTheme`），传入 `ContentTheme` 时实际类型即为 `ContentTheme` |
| `SKAnimation(int width, int height, int frames, Action<...>? onFrame)` | 回调式实现；`onFrame` 可为 `null` |
| `skanim(SKSize size, int frames, Action<...> onFrame)` | 工厂方法；`size` 的浮点宽高被截断为整数 |
| `SkiaAnim(ISKAnimation)` / `SkiaAnim(SKSize, int, Action<...>)` | 包装为 `NBSkiaAnimation` 可视节点；实际视频管线用法，测试中用于编译冒烟 |
| `AsClip(int frames, int? start = null, string name = "clip")` | 把可视节点登记为片段并注册到 `clips()`；`debug = false` 时由 `run(stage(...), clips())` 正式出片 |
| `Frames` / `Width` / `Height` | 总帧数、单帧像素尺寸 |
| `BuildFrame(int frame)` | 返回新的 `SKBitmap`；越界、尺寸非法或回调为空时返回 `null`，调用方负责释放 |
| `Draw(SKCanvas canvas, int frame)` | 直接把帧绘制到已有画布，不创建位图；成功返回 `true` |
| `SavePNG(int frame, string filePath)` | 渲染并保存为 PNG；目录自动创建；成功返回 `true`，无法渲染返回 `false` |
| `SaveMp4(double fps, string filePath)` | 全部帧编码为 MP4；依赖 ffmpeg；不返回成功状态，不自动建目录 |
| `SaveGif(double fps, string filePath)` | 全部帧编码为 GIF；尺寸上限 1200，二值透明；不自动建目录 |

## 关键约束

- **两文件结构**：动画类单独放在 `MyAnimation.cs`（或 `<类名>.cs`），测试入口放在 `MyAnimation_Test.cs`；不要把所有代码塞进一个文件，也不要让测试文件里出现第二个动画类。
- **类文件必须是纯类型文件**：不含 `#!/usr/bin/env dotnet`、不含 `#:include`、不含顶层语句；否则与测试文件 `#:include` 后会编译失败（顶层语句只能出现在入口文件）。
- **测试文件必须有入口指令**：`#!/usr/bin/env dotnet` + `#:include <实现类源文件>`；`#:package NewBeeVG@1.0.1.15` 按运行环境决定是否保留。
- **测试文件必须引入 Methods**：写 `using static NewBeeVG.Methods;`，否则 `SkiaAnim(...)` / `run(...)` / `clips()` 无法解析。
- **必须有 `debug` 开关**：`using` 之后声明 `bool debug = true;`；所有 AI 测试语句加 `if (debug == true)` 前缀，`debug = false` 时只执行正式出片。
- **AI 永远不要把 `debug` 设为 `false`**：编写、测试、编译验证期间一律保持 `true`，不要为了让出片跑起来而改它；`false` 只由使用者在交付后手动切换。
- **结尾必须有 run**：文件末尾固定写 `if (debug == false) run(stage(Width, Height), clips());`。
- **测试场景用大括号隔离**：每个场景单独 `{ }`，`content` / `theme` / `anim` 都放在块内，避免变量重名与相互污染。
- **每个场景必须写真实 SkiaAnim 代码**：场景末尾的 `SkiaAnim(new YourAnimation(...)).AsClip(frames, name: "...")` 就是交付后的生产代码，**不要加 `if (debug == true)`**，也不要用假的片段糊弄编译；`debug` 改为 `false` 后由 `run(...)` 直接出片。
- **内容参数封装**：继承 `SKCustomAnimation` 时，把标题、字号、数据等输入封装为**内部类** `ContentSetting`，构造签名保持 `(width, height, frames, content, contentTheme = null)`；不要把大量参数平铺在构造函数上。
- **主题颜色扩展**：`NBVideoTheme` 的 7 个颜色不够用时，不要继续加构造参数，而是定义内部类 `ContentTheme : NBVideoTheme`，把额外颜色作为属性，并整体传入构造函数。
- **ContentTheme 必须继承 NBVideoTheme**：否则无法直接传给基类，也无法通过 `(ContentTheme)Theme` 取回扩展颜色。
- **默认主题要复制**：`contentTheme` 为 `null` 时用 `ContentTheme.CreateDefault()`（复制 `NBVideoThemes.Nature`），不要直接返回共享静态实例，避免被修改后污染全局。
- **内置主题是共享对象**：`NBVideoThemes.Tech` 等静态实例不要在运行时修改；需要微调时用 `ContentTheme.From(...)` 复制后再改。
- **透明位图（预乘 Alpha）**：`BuildFrame` 使用 `SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul)` 创建位图，支持透明通道，`SavePNG` 导出的 PNG 会保留 Alpha。

```csharp
var info = new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
var bmp = new SKBitmap(info);
```

- **清空画布决定透明度**：每帧都用 `canvas.Clear(...)` 重新开始。需要透明背景时传 `SKColors.Transparent`；使用主题时传 `Theme.Background`。
- **预乘 Alpha**：位图为 `Premul`，绘制半透明颜色/图片时按预乘规则正确合成；外部直通（Unpremul）像素应通过 `SKCanvas` 绘制或先做预乘转换，不要直接内存拷贝。
- **不要覆盖 OnFrame**：`SKCustomAnimation` 构造时已设置 `OnFrame = DrawFrame`；再赋值会让重写的 `DrawFrame` 失效。
- **释放**：`BuildFrame` 的返回值必须 `Dispose()`；逐帧导出采用「渲染 → 写出 → 释放」，不要缓存全部帧。
- **目录行为不同**：`SavePNG` 自动创建目录；`SaveMp4` / `SaveGif` 不创建，路径不存在会失败。
- **PNG 专属**：`SavePNG` 固定 `SKEncodedImageFormat.Png`，与扩展名无关；需要 WebP/JPEG 时用 `BuildFrame` + `SKImage.Encode`。
- **帧率精度**：MP4 支持任意正的 `fps`；GIF 延迟单位为 1/100 秒，非整除帧率（如 30）会被取整，实际速度有偏差。
- **GIF 限制**：`MaxWidth` / `MaxHeight` 为 1200，超出按比例缩小；仅支持二值透明（Alpha 阈值 128）；颜色量化为 256 色。
- **绘制对象**：`SKPaint`、`SKFont`、`SKPath`、`SKShader` 等使用 `using`，避免每帧泄漏原生对象。

## 完成前检查

1. 是否创建了两个文件：动画类源文件（模板为 `MyAnimation.cs`）与测试文件 `MyAnimation_Test.cs`；
2. 动画类文件是否只有类型声明（无 `#!`、无顶层语句）；
3. 测试文件是否有 `#!/usr/bin/env dotnet`、`#:include <实现类源文件>`（模板为 `MyAnimation.cs`）与 `using static NewBeeVG.Methods;`；
4. 是否在 `using` 后声明 `bool debug = true;`，且 AI 未把它改成 `false`；
5. 所有 AI 测试语句是否都带 `if (debug == true)` 前缀；
6. 文件结尾是否有 `if (debug == false) run(stage(Width, Height), clips());`；
7. 每个测试场景是否用 `{ }` 隔离变量，末尾的 `SkiaAnim(...).AsClip(...)` 是否为真实场景代码且未被 `debug` 条件包裹；
8. 子类是否继承 `SKCustomAnimation`，`DrawFrame` 是否使用 `protected override`；
9. 构造函数是否为 `(width, height, frames, content, contentTheme = null)`，内容参数是否封装在内部类 `ContentSetting` 中；
10. 主题颜色不够用时，是否通过内部类 `ContentTheme : NBVideoTheme` 扩展，而不是继续增加构造参数；
11. `ContentTheme` 是否提供 `From(theme)` / `CreateDefault()`，默认值是否使用复制而非共享静态实例；
12. 是否通过 `Theme` / `_theme` 取色，而不是散落的硬编码颜色；
13. `DrawFrame` 是否每帧清空画布；
14. 是否误改 `OnFrame`，导致 `DrawFrame` 不生效；
15. 是否处理了 `BuildFrame` / `SavePNG` 的 `null` / `false` 返回；
16. 透明背景是否按需求用 `SKColors.Transparent`（或 `Theme.Background`）清空画布；
17. `fps` 是否大于 0；GIF 是否使用了合适的帧率；
18. `SaveMp4` / `SaveGif` 的输出目录是否已存在。

## 资源

- `references/api-and-pitfalls.md`：两个文件的完整示例、主题与 `ContentTheme` 扩展、逐帧技巧、三种导出方式、常见问题。
- `assets/MyAnimation.cs`：动画类文件模板（类名 `MyAnimation`，只含类定义，可直接复制改名）。
- `assets/MyAnimation_Test.cs`：测试入口模板（`#!`、`#:package NewBeeVG@1.0.1.15`、`#:include MyAnimation.cs`、`using static NewBeeVG.Methods;`、`bool debug = true;`、分场景大括号、`debug` 条件测试、结尾 `run(...)`）。
