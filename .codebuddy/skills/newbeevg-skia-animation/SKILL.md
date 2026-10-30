---
name: newbeevg-skia-animation
description: This skill should be used when creating, editing, or debugging frame-by-frame animations rendered with SkiaSharp in the NewBeeVG codebase, especially when creating an SKCustomAnimation subclass in .NET file-based apps using the two-file pattern (class file plus a _Test.cs runner with #:include), or when working with ISKAnimation, SKAnimation, SKCanvas frame callbacks, NBVideoTheme/NBVideoThemes color themes, ContentSetting/ContentTheme inner classes, SKAnimSetting-based content margins and AnimRatio timing, exact core class/method signatures, PNG frame export (SavePNG), MP4/GIF export (SaveMp4/SaveGif), frame index to progress conversion, transparency, frame rate, bitmap disposal, and the debug-flag guard placement for loop-generated test cases.
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
#:package NewBeeVG@1.0.1.25
// 包含 MyAnimation 所在的源文件路径
#:include MyAnimation.cs

// 在仓库 apps/files 内运行时，Directory.Build.props 已提供 NewBeeVG 项目引用；
// 若出现重复引用或版本冲突，删除上面的 #:package 指令即可。

using NewBeeVG;
using SkiaSharp;
using static NewBeeVG.Methods;

// debug = true  ：只跑 AI 测试语句（PNG / MP4 / GIF 导出）
// debug = false ：跳过测试，执行结尾 run(...)，用下面 SkiaAnim 注册的真实片段出片
bool debug = true;
if (args.Length > 0) debug = args[0] == "debug";   // 不带参数 = 出测试图；传 "debug" 以外的值 = 正式出片
```

`debug` 用于切换「AI 测试」与「真实出片」：

- `debug == true`：只执行 AI 测试语句（`SavePNG` / `SaveMp4` / `SaveGif` 等），不调用 `run`，不合成视频；
- `debug == false`：跳过这些测试语句，执行结尾 `run(stage(Width, Height), clips())`，按真实场景出片。

命令行行为：

- 不带参数：`debug` 保持 `true`，只出测试图；
- 第一个参数是 `"debug"`：`debug` 保持 `true`，同样只出测试图；
- 第一个参数是其他值（如 `out.mp4`、`42 out.png`）：`debug = false`，跳过测试并执行结尾 `run(...)`；
- 若希望这些参数继续传给 `run` 直接导出，把结尾改成 `run(stage(Width, Height), clips(), args: args);`。

**AI 在编写、运行测试和验证编译期间，`bool debug` 默认必须是 `true`，并且要保留 `if (args.Length > 0) debug = args[0] == "debug";` 这一行；不要把 `debug` 硬编码成 `false`。** AI 跑测试时不要传入会让 `debug` 变为 `false` 的参数；`false` 只由使用者在正式出片时通过命令行或手动切换触发。

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
| `ContentSetting : SKAnimSetting` | 承载内容参数：标题、字号、数据、数量等；**必须继承 `SKAnimSetting`** |
| `ContentTheme` | 当 `NBVideoTheme` 的 7 个基础颜色不够用时，继承它并补充专用颜色 |

`ContentSetting` 必须继承 `SKAnimSetting`，这样才能通过 `Margin` 控制特效四周留白：

```csharp
public class ContentSetting : SKAnimSetting
{
    public string Title { get; set; } = "";
    public float TitleSize { get; set; } = 96f;
    public float Radius { get; set; } = 80f;
}
```

`Margin` 支持以下写法：

| 写法 | 含义 |
| --- | --- |
| `[]` 或 `null` | 四边为 0 |
| `[10]` | 四边都是 10 |
| `[10, 20]` | 水平 10、垂直 20 |
| `[10, 20, 30, 40]` | 左 10、上 20、右 30、下 40 |

`GetMargin()` 会统一返回 `[left, top, right, bottom]`。实现 `DrawFrame` 时，特效必须默认填满带 Margin 的内容矩形：

```text
x = left
y = top
w = Width  - left - right
h = Height - top  - bottom
```

而不是直接使用整张 `(Width, Height)`。

### AnimRatio

`SKAnimSetting.AnimRatio` 控制动效在整段 clip 内完成的比例，默认 `0.80f`：

- 前 `AnimRatio` 的时间完成动画；
- 剩余时间保持最终状态；
- 取值 `0..1`；`<= 0` 表示不动画（直接最终状态），`>= 1` 表示全程动画。

`DrawFrame` 中必须把原始进度映射为动画进度，不要直接用 `frame / (Frames - 1)`：

```csharp
// 原始进度 0..1
float raw = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

// 按 AnimRatio 压缩：前段完成动效，后段静止在最终状态
float ratio = Math.Clamp(_content.AnimRatio, 0f, 1f);
float t = ratio <= 0f ? 1f : Math.Clamp(raw / ratio, 0f, 1f);
```

- `t` 用于所有动效计算；
- `raw > AnimRatio` 后 `t` 保持 `1`，画面不再变化；
- 不要在 `AnimRatio` 之后继续推进、回退或循环动画。

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

`DrawFrame` 中处理 Margin 的标准写法：

```csharp
protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
{
    // 透明背景不要清空画布：每帧位图初始就是透明的
    if (_theme.Background.Alpha > 0)
        canvas.Clear(_theme.Background);

    var m = _content.GetMargin(); // [left, top, right, bottom]
    float left = m[0], top = m[1], right = m[2], bottom = m[3];
    float w = size.Width - left - right;
    float h = size.Height - top - bottom;

    if (w <= 0 || h <= 0) return;

    canvas.Save();
    canvas.ClipRect(new SKRect(left, top, left + w, top + h));
    canvas.Translate(left, top);

    // 原始进度 → 按 AnimRatio 压缩后的动画进度
    float raw = Frames <= 1 ? 1f : frame / (float)(Frames - 1);
    float ratio = Math.Clamp(_content.AnimRatio, 0f, 1f);
    float t = ratio <= 0f ? 1f : Math.Clamp(raw / ratio, 0f, 1f);

    // 后续按内容区域 (0, 0, w, h) 绘制，用 t 驱动动效
    float cx = w / 2f;
    float cy = h / 2f;

    // ... 用 t 绘制具体内容 ...

    canvas.Restore();
}
```

要点：

- **透明背景不要清空画布**：`_theme.Background.Alpha == 0` 时跳过 `canvas.Clear(...)`；每帧位图初始就是透明的，Clear 是多余操作；
- 只有 `_theme.Background.Alpha > 0` 时才 `canvas.Clear(_theme.Background)`；
- 背景通常铺满整张画布，内容只在 Margin 内绘制；
- 先 `ClipRect` 再 `Translate`，可以保证内容不会画出边距；
- 所有尺寸计算使用 `w` / `h`，不要再用 `size.Width` / `size.Height`；
- 动效必须使用按 `AnimRatio` 压缩后的 `t`，不要直接用原始帧进度；
- Margin 过大导致内容区域小于等于 0 时直接返回。

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
- 内部类 `ContentSetting` 必须继承 `SKAnimSetting`；
- 构造函数调用 `base(width, height, frames, contentTheme ?? ContentTheme.CreateDefault())`。

C# 12 / .NET 10 支持主构造函数，但这里仍建议显式写构造函数，便于校验 `content` 与做字段赋值。

### 2. 创建测试文件 `MyAnimation_Test.cs`

规则：

- 必须以 `#!/usr/bin/env dotnet` 开头；
- `#:package NewBeeVG@1.0.1.25` 用于脱离仓库时引入 NewBeeVG；在仓库的 `apps/files/` 内，`Directory.Build.props` 已提供项目引用，出现重复引用时删除该指令；
- `#:include` 后写**所实现动画类的源文件路径**（模板为 `MyAnimation.cs`），路径相对于测试文件；
- `using` 至少包含 `NewBeeVG`、`SkiaSharp`、`using static NewBeeVG.Methods;`（`SkiaAnim` 是 `Methods` 的静态方法）；
- 在 `using` 之后声明 `bool debug = true;`，并保留 `if (args.Length > 0) debug = args[0] == "debug";`；**AI 不得把默认值改成 `false`**，测试时也不要传会让它变为 `false` 的参数；
- **每个测试场景用一对大括号 `{ }` 包起来**，隔离 `content` / `anim` / `theme` 等局部变量，避免不同场景重名；
- **只在真正写本地文件的调用前加 `if (debug == true)`**，例如 `if (debug == true) anim.SavePNG(...)`、`if (debug == true) anim.SaveMp4(...)`；不要把循环、实例构造、多个导出整体包进一个 `if (debug == true) { ... }` 大块；
- **测试目录创建同样加守卫**：例如 `if (debug == true) Directory.CreateDirectory("output/numberroll");`；测试用的目录（尤其 `SaveMp4` / `SaveGif` 的输出目录）不要在守卫外创建；
- 每个场景末尾写**真实场景**的 `SkiaAnim(new Xxx(...)).AsClip(frames, name: "...")`，且不要加 `debug` 条件：`debug = true` 时它只完成编译与片段注册（不 `run`），`debug = false` 时由结尾 `run(...)` 出片；
- 顶层只保留共享常量（`Width` / `Height` / `Frames`）、`debug` 与各场景块，不要在块外声明场景专用变量；
- **文件结尾固定写**：

```csharp
if (debug == false)
    run(stage(Width, Height), clips());
```

#### 循环生成测试用例（必须按此结构）

当测试需要遍历参数生成多组样例时，**不要把整个 `foreach` 包进 `if (debug == true)`**：那样 `debug = false` 时 `SkiaAnim` 也不会注册片段，正式出片会缺内容。正确写法是「每个迭代内，测试代码进守卫块，`SkiaAnim` 留在块外」：

```csharp
if (debug == true) Directory.CreateDirectory("output/numberroll");

foreach (var (nm, mb) in new (string, float)[] { ("关", 0f), ("中", 0.5f), ("满", 1f) })
{
    var s = Base();
    s.Glint = 0f; s.Turns = 2f; s.MotionBlur = mb;

    using var probe = new NumberRollA(Width, Height, Frames, s, theme);
    if (debug == true) probe.SavePNG(20, $"output/numberroll/模糊_{nm}.png");

    SkiaAnim(new NumberRollA(Width, Height, Frames, s, theme))
        .AsClip(Frames, name: $"模糊_{nm}");
}
```

要点：

- 循环参数（`s`、`theme`、`nm`）在守卫外准备，测试与 `SkiaAnim` 共用；
- **只在输出调用前加守卫**：`if (debug == true) probe.SavePNG(...)`；不要把整个循环体或「构造 + 保存」包进 `if (debug == true) { ... }`；
- `probe` 用 `using var`，在迭代结束时释放；`SkiaAnim` 里必须**重新 `new` 一个实例**，不能复用已释放的 `probe`；
- **循环内创建的每个特效都必须有 `SkiaAnim`**：除了 `probe.SavePNG(...)`，同一迭代还要写 `SkiaAnim(new Xxx(...)).AsClip(Frames, name: ...)`；只写 probe 会让正式出片缺少这些档位；
- `AsClip` 的 `name` 带上循环键（如 `$"模糊_{nm}"`），避免片段重名；
- 禁止写成 `if (debug == true) SkiaAnim(...)`，否则交付后改 `debug = false` 会没有片段可出。

#### 反例：一个 if 包住多条写文件语句（禁止）

不要这样写：

```csharp
if (debug == true)
{
    anim.SaveMp4(Fps, "output/pageflip/flip_book_5s.mp4");

    foreach (int f in Stills)
        anim.SavePNG(f, $"output/pageflip/书页_{f:000}.png");

    foreach (var (nm, v) in new (string, float)[] { ("关", 0f), ("中", 0.5f), ("满", 1f) })
    {
        var s = Base();
        s.Shadow = v;
        using var probe = new PageFlipImageA(Width, Height, Frames, s, theme);
        probe.SavePNG(42, $"output/pageflip/投影_{nm}.png");
    }
}
```

正确写法：每条写文件语句各自带守卫，循环与对象构造留在守卫外：

```csharp
if (debug == true) anim.SaveMp4(Fps, "output/pageflip/flip_book_5s.mp4");

foreach (int f in Stills)
{
    if (debug == true) anim.SavePNG(f, $"output/pageflip/书页_{f:000}.png");
}

foreach (var (nm, v) in new (string, float)[] { ("关", 0f), ("中", 0.5f), ("满", 1f) })
{
    var s = Base();
    s.Shadow = v;
    using var probe = new PageFlipImageA(Width, Height, Frames, s, theme);
    if (debug == true) probe.SavePNG(42, $"output/pageflip/投影_{nm}.png");

    SkiaAnim(new PageFlipImageA(Width, Height, Frames, s, theme))
        .AsClip(Frames, name: $"投影_{nm}");
}
```

判断标准：`if (debug == true)` 的花括号里如果出现两条及以上写文件语句（`SavePNG` / `SaveMp4` / `SaveGif` / `Directory.CreateDirectory`），就是错误写法，必须逐条拆开。

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

#### 字体

如果动画需要绘制文字，在 `ContentSetting` 中添加字体字段，默认留空：

```csharp
/// <summary>字体名；为空时使用 SKCustomAnimation.GetDefaultFontName()。</summary>
public string FontFamily { get; set; } = "";
```

在 `DrawFrame` 中解析：

```csharp
// 调用方指定了字体就用指定值，否则回退到官方默认字体
var fontName = string.IsNullOrWhiteSpace(_content.FontFamily)
    ? GetDefaultFontName()
    : _content.FontFamily;

using var typeface = SKTypeface.FromFamilyName(fontName) ?? SKTypeface.Default;
using var font = new SKFont(typeface, _content.FontSize);
```

- `GetDefaultFontName()` 优先使用 `font("...")` 的全局设置，否则按平台返回 Microsoft YaHei / Helvetica / DejaVu Sans / Arial；
- 不要给 `FontFamily` 设置 `"Microsoft YaHei"` 之类的平台字体默认值；调用方需要时只覆盖该字段；
- 返回值只是字体名，不保证系统已安装；`SKTypeface.FromFamilyName` 可能返回替代字体，建议用 `?? SKTypeface.Default` 兜底。

### 4. 帧号换算进度

`frame` 范围是 `0 ~ Frames-1`。先算原始进度，再按 `AnimRatio` 压缩：

```csharp
// 一次性动画：最后一帧恰好为 1.0
float raw = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

// 循环动画：避免首尾帧重复
// float raw = frame / (float)Frames;

// 动效进度：前 AnimRatio 段完成动画，后段保持最终状态
float ratio = Math.Clamp(_content.AnimRatio, 0f, 1f);
float t = ratio <= 0f ? 1f : Math.Clamp(raw / ratio, 0f, 1f);
```

不要直接用 `raw` 驱动动效；`raw > AnimRatio` 后 `t` 必须保持 `1`。

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
// 在测试文件中必须加守卫：if (debug == true) Directory.CreateDirectory("output");

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

## 关键类与签名（与源码保持一致）

以下签名可直接用于编写代码，不需要反编译程序集。

### `ISKAnimation`

```csharp
public interface ISKAnimation
{
    int Frames { get; }
    int Width { get; }
    int Height { get; }

    SKBitmap? BuildFrame(int frame);
    bool Draw(SKCanvas canvas, int frame);
    bool SavePNG(int frame, string filePath);
    void SaveMp4(double fps, string filePath);
    void SaveGif(double fps, string filePath);
}
```

### `SKAnimation`

```csharp
public class SKAnimation : ISKAnimation
{
    public int Frames { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    // 逐帧回调：画布、画面尺寸、帧号；为 null 时无法生成任何帧
    public Action<SKCanvas, SKSize, int>? OnFrame { get; init; }

    public SKAnimation(int width, int height, int frames,
        Action<SKCanvas, SKSize, int>? onFrame = null);

    public SKBitmap? BuildFrame(int frame);
    public bool Draw(SKCanvas canvas, int frame);
    public bool SavePNG(int frame, string filePath);
    public void SaveMp4(double fps, string filePath);
    public void SaveGif(double fps, string filePath);
}
```

### `SKCustomAnimation`

```csharp
public class SKCustomAnimation : SKAnimation
{
    // 当前主题；构造函数注入，构造后不可更改
    public NBVideoTheme Theme { get; private set; }

    // 优先 font(...) 的全局字体，否则按平台返回默认字体
    public static string GetDefaultFontName();

    // theme 为 null 时使用 NBVideoThemes.Nature
    // 构造函数内部会把 OnFrame 指向 DrawFrame
    public SKCustomAnimation(int width, int height, int frames,
        NBVideoTheme? theme = null);

    // 子类重写此方法；默认实现为空
    protected virtual void DrawFrame(SKCanvas canvas, SKSize size, int frame);
}
```

### `SKAnimSetting`

```csharp
public class SKAnimSetting
{
    // 动效完成比例，默认 0.80；前 80% 完成动画，后 20% 保持最终状态
    public float AnimRatio = 0.80f;

    // 支持 0 / 1 / 2 / 4 个元素：四边 0、四边相同、水平+垂直、左+上+右+下
    public int[] Margin { get; set; } = new int[] { };

    // 归一化为 [left, top, right, bottom]
    public int[] GetMargin();
}
```

### `NBVideoTheme` / `NBVideoThemes`

```csharp
public partial class NBVideoTheme
{
    public SKColor Primary { get; set; }
    public SKColor Background { get; set; }
    public SKColor TextPrimary { get; set; }
    public SKColor TextSecondary { get; set; }
    public SKColor Divider { get; set; }
    public SKColor Positive { get; set; }
    public SKColor Negative { get; set; }
    public string Name { get; set; }

    public SKBitmap Preview(int width = 1920, int height = 1080);
}

public class NBVideoThemes
{
    // 均为 static get 属性
    // Tech, Eco, Finance, Healthcare, Education, Media, Retail, Nature,
    // Ocean, Sunset, Minimal, Cyberpunk, Luxury, Retro, Candy, Arctic,
    // Forest, Corporate, AI, Podcast, NightCopper, ParchmentDay, ColdArchive
}
```

### `NBSkiaAnimation`

```csharp
public class NBSkiaAnimation : NBBaseImage
{
    public ISKAnimation? Animation { get; set; }
    public int TotalFrames { get; }
    public NBAnimateLoopMode LoopMode { get; set; } // 默认 Loop
}
```

### `NBStage`

```csharp
public class NBStage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; } = 25;
    public SKColor? Background { get; set; }
    public int AudioFrameRate { get; set; } = 44100;
}
```

### `NBClip`

```csharp
public class NBClip : IPlayable
{
    // null 表示自动接在前一个 clip 之后
    public int? StartFrame { get; internal set; }

    // <= 0 表示持续到最后一帧
    public int DurationFrames { get; set; }

    public string Name { get; internal set; }
    public bool IsVisible { get; set; }
}
```

### `Methods` 常用静态方法

```csharp
public static NBStage stage(
    int width = 1080, int height = 1920,
    SKColor? bg = null, double fps = 25.0);

public static int run(
    NBStage stage, IList<NBClip> clips,
    IList<NBClip>[]? clipLists = null,
    string[]? args = null,
    [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "");
    // 最后一个参数由编译器自动注入，调用方不要手动传

public static List<NBClip> clips(bool reset = false);
public static NBClip? clip(string key);

public static NBSkiaAnimation SkiaAnim(ISKAnimation animation);
public static NBSkiaAnimation SkiaAnim(SKSize size, int frames,
    Action<SKCanvas, SKSize, int> onFrame);

public static NBAudioClip AudioFile(string path);
public static void font(string name);
public static string GetDefaultFontFamily();
```

### 常用扩展方法

```csharp
public static NBDrawingClip AsClip<T>(
    this T widget, int frames, int? start = null, string name = "clip")
    where T : NBVisual;

public static NBDrawingClip AsClip<T>(
    this T widget, out NBDrawingClip value,
    int frames, int? start = null, string name = "clip")
    where T : NBVisual;

public static NBClip AsClip<T>(
    this T widget, string name = "clip")
    where T : NBClip; // 音频等非可视 clip

public static T LinkTo<T>(
    this T widget, string clipName, int startFrame = 0)
    where T : NBClip;

public static T LinkTo<T>(
    this T widget, T referenceClip, int startFrame = 0)
    where T : NBClip;

public static T Styles<T>(
    this T t, params Action<NBVisual>?[] styles)
    where T : NBVisual;
```

## API 摘要

| 成员 | 说明 |
| --- | --- |
| `SKCustomAnimation(int width, int height, int frames, NBVideoTheme? theme = null)` | 推荐基类；子类重写 `DrawFrame`；`theme` 为空时使用 `NBVideoThemes.Nature` |
| `YourCustomAnimation(int width, int height, int frames, ContentSetting content, ContentTheme? contentTheme = null)` | 推荐子类签名；`ContentSetting` 承载内容，`ContentTheme` 扩展颜色 |
| `ContentTheme : NBVideoTheme` | 主题颜色不够用时继承并补充属性；`From(theme)` 复制内置主题，`CreateDefault()` 复制自然主题 |
| `Theme` | 当前主题（`NBVideoTheme`），传入 `ContentTheme` 时实际类型即为 `ContentTheme` |
| `GetDefaultFontName()` | 默认字体名：优先 `font(...)` 的全局设置，否则按平台返回 Microsoft YaHei / Helvetica / DejaVu Sans / Arial |
| `SKAnimSetting.AnimRatio` | 动效完成比例，默认 0.80；前 80% 完成动画，后 20% 保持最终状态 |
| `SKAnimSetting.Margin` / `GetMargin()` | 动画通用设置；`Margin` 控制留白，`GetMargin()` 归一化为 [左, 上, 右, 下] |
| `SKAnimation(int width, int height, int frames, Action<...>? onFrame)` | 回调式实现；`onFrame` 可为 `null` |
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
- **测试文件必须有入口指令**：`#!/usr/bin/env dotnet` + `#:include <实现类源文件>`；`#:package NewBeeVG@1.0.1.25` 按运行环境决定是否保留。
- **测试文件必须引入 Methods**：写 `using static NewBeeVG.Methods;`，否则 `SkiaAnim(...)` / `run(...)` / `clips()` 无法解析。
- **必须有 `debug` 开关**：`using` 之后声明 `bool debug = true;`；所有 AI 测试语句加 `if (debug == true)` 前缀，`debug = false` 时只执行正式出片。
- **AI 不要把 `debug` 硬编码为 `false`**：默认必须是 `true`，并保留 `args` 覆盖行；编写、测试、编译验证期间通过不传参数或传 `"debug"` 保持 `true`，不要为了让出片跑起来改默认值。
- **测试目录创建也要守卫**：所有测试用目录创建都写成 `if (debug == true) Directory.CreateDirectory("output/xxx");`，不要无条件创建；`SavePNG` 虽会自动建目录，显式创建同样放在守卫内。
- **一条守卫只对应一条写文件语句**：`if (debug == true)` 后面只能跟一条 `SavePNG` / `SaveMp4` / `SaveGif` / `Directory.CreateDirectory`；禁止把多条导出或整个 `foreach` 包进同一个守卫块。
- **结尾必须有 run**：文件末尾固定写 `if (debug == false) run(stage(Width, Height), clips());`。
- **测试场景用大括号隔离**：每个场景单独 `{ }`，`content` / `theme` / `anim` 都放在块内，避免变量重名与相互污染。
- **每个场景必须写真实 SkiaAnim 代码**：场景末尾的 `SkiaAnim(new YourAnimation(...)).AsClip(frames, name: "...")` 就是交付后的生产代码，**不要加 `if (debug == true)`**，也不要用假的片段糊弄编译；`debug` 改为 `false` 后由 `run(...)` 直接出片。
- **循环测试用例的守卫位置**：`foreach` / `for` 外层不要包 `if (debug == true)`；每个迭代内只给实际写文件的调用加守卫（如 `if (debug == true) probe.SavePNG(...)`），随后无条件写 `SkiaAnim(new Xxx(...)).AsClip(Frames, name: $"key_{key}")`；不要用 `if (debug == true) { ... }` 包住整个循环体或一大段逻辑。
- **循环内创建的特效必须有 SkiaAnim**：每个迭代在 `if (debug == true) probe.SavePNG(...)` 之后，必须再写一条无条件的 `SkiaAnim(new Xxx(...)).AsClip(Frames, name: $"key_{key}")`；只创建 probe 保存 PNG、不写 `SkiaAnim`，会导致正式出片缺少这些档位。
- **内容参数封装**：继承 `SKCustomAnimation` 时，把标题、字号、数据等输入封装为**内部类** `ContentSetting`，构造签名保持 `(width, height, frames, content, contentTheme = null)`；不要把大量参数平铺在构造函数上。`ContentSetting` **必须继承 `SKAnimSetting`**；需要绘制文字时，在 `ContentSetting` 增加 `FontFamily`（默认空字符串），绘制时为空则使用 `GetDefaultFontName()`。
- **Margin 生效**：`DrawFrame` 必须用 `_content.GetMargin()` 计算内容矩形 `(left, top, width-left-right, height-top-bottom)`，并先 `ClipRect` / `Translate` 再绘制；特效默认填满该矩形，不能忽略 Margin 直接用整张画布。
- **AnimRatio 生效**：所有动效必须用 `ratio <= 0 ? 1 : Math.Clamp(raw / ratio, 0, 1)` 压缩进度（`ratio` 为 `AnimRatio`）；`AnimRatio` 之后保持最终状态，不能直接用原始帧进度驱动动画。
- **主题颜色扩展**：`NBVideoTheme` 的 7 个颜色不够用时，不要继续加构造参数，而是定义内部类 `ContentTheme : NBVideoTheme`，把额外颜色作为属性，并整体传入构造函数。
- **ContentTheme 必须继承 NBVideoTheme**：否则无法直接传给基类，也无法通过 `(ContentTheme)Theme` 取回扩展颜色。
- **默认主题要复制**：`contentTheme` 为 `null` 时用 `ContentTheme.CreateDefault()`（复制 `NBVideoThemes.Nature`），不要直接返回共享静态实例，避免被修改后污染全局。
- **内置主题是共享对象**：`NBVideoThemes.Tech` 等静态实例不要在运行时修改；需要微调时用 `ContentTheme.From(...)` 复制后再改。
- **透明位图（预乘 Alpha）**：`BuildFrame` 使用 `SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul)` 创建位图，支持透明通道，`SavePNG` 导出的 PNG 会保留 Alpha。

```csharp
var info = new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
var bmp = new SKBitmap(info);
```

- **透明背景不要 Clear**：`Theme.Background.Alpha == 0` 时不要调用 `canvas.Clear(...)`；每帧位图初始就是透明的，Clear 是多余操作。只有 `Alpha > 0` 时才清空画布，用 `Theme.Background` 铺底。
- **预乘 Alpha**：位图为 `Premul`，绘制半透明颜色/图片时按预乘规则正确合成；外部直通（Unpremul）像素应通过 `SKCanvas` 绘制或先做预乘转换，不要直接内存拷贝。
- **不要覆盖 OnFrame**：`SKCustomAnimation` 构造时已设置 `OnFrame = DrawFrame`；再赋值会让重写的 `DrawFrame` 失效。
- **释放**：`BuildFrame` 的返回值必须 `Dispose()`；逐帧导出采用「渲染 → 写出 → 释放」，不要缓存全部帧。
- **目录行为不同**：`SavePNG` 自动创建目录；`SaveMp4` / `SaveGif` 不创建，路径不存在会失败。
- **PNG 专属**：`SavePNG` 固定 `SKEncodedImageFormat.Png`，与扩展名无关；需要 WebP/JPEG 时用 `BuildFrame` + `SKImage.Encode`。
- **帧率精度**：MP4 支持任意正的 `fps`；GIF 延迟单位为 1/100 秒，非整除帧率（如 30）会被取整，实际速度有偏差。
- **GIF 限制**：`MaxWidth` / `MaxHeight` 为 1200，超出按比例缩小；仅支持二值透明（Alpha 阈值 128）；颜色量化为 256 色。
- **绘制对象**：`SKPaint`、`SKFont`、`SKPath`、`SKShader` 等使用 `using`，避免每帧泄漏原生对象。
- **字体**：跨平台默认字体用 `SKCustomAnimation.GetDefaultFontName()`，不要硬编码 `"Microsoft YaHei"`；`font("...")` 设置的全局字体优先。

## 完成前检查

1. 是否创建了两个文件：动画类源文件（模板为 `MyAnimation.cs`）与测试文件 `MyAnimation_Test.cs`；
2. 动画类文件是否只有类型声明（无 `#!`、无顶层语句）；
3. 测试文件是否有 `#!/usr/bin/env dotnet`、`#:include <实现类源文件>`（模板为 `MyAnimation.cs`）与 `using static NewBeeVG.Methods;`；
4. 是否在 `using` 后声明 `bool debug = true;`，并保留 `if (args.Length > 0) debug = args[0] == "debug";`，且默认值未被改成 `false`；
5. 所有写本地文件的调用（`SavePNG` / `SaveMp4` / `SaveGif` / `Directory.CreateDirectory`）是否各自带 `if (debug == true)`，且没有把大段逻辑整体包进守卫块；
6. 文件结尾是否有 `if (debug == false) run(stage(Width, Height), clips());`；
7. 每个测试场景是否用 `{ }` 隔离变量，末尾的 `SkiaAnim(...).AsClip(...)` 是否为真实场景代码且未被 `debug` 条件包裹；
8. 循环场景是否只给 `SavePNG` 等输出调用加守卫，没有包住整个循环体；每个迭代创建的特效是否都有对应的 `SkiaAnim(...).AsClip(...)`，且在守卫外、`name` 带循环键；
9. 子类是否继承 `SKCustomAnimation`，`DrawFrame` 是否使用 `protected override`；
10. 构造函数是否为 `(width, height, frames, content, contentTheme = null)`，内容参数是否封装在内部类 `ContentSetting` 中；
11. 主题颜色不够用时，是否通过内部类 `ContentTheme : NBVideoTheme` 扩展，而不是继续增加构造参数；
12. `ContentTheme` 是否提供 `From(theme)` / `CreateDefault()`，默认值是否使用复制而非共享静态实例；
13. 是否通过 `Theme` / `_theme` 取色，而不是散落的硬编码颜色；
14. `DrawFrame` 是否按 `Theme.Background.Alpha` 决定是否清空画布；
15. 是否误改 `OnFrame`，导致 `DrawFrame` 不生效；
16. 是否处理了 `BuildFrame` / `SavePNG` 的 `null` / `false` 返回；
17. `Theme.Background` 为透明色时是否**没有**调用 `canvas.Clear(...)`；只有非透明时才清空画布；
18. `fps` 是否大于 0；GIF 是否使用了合适的帧率；
19. `SaveMp4` / `SaveGif` 的输出目录是否已存在（且创建语句是否在 `debug` 守卫内）；
20. 字体是否使用 `SKCustomAnimation.GetDefaultFontName()` 或 `font(...)`，没有跨平台硬编码字体名；
21. `ContentSetting` 是否继承 `SKAnimSetting`，`DrawFrame` 是否按 `GetMargin()` 计算并裁剪内容矩形；
22. 动效是否按 `AnimRatio` 压缩进度，`AnimRatio` 之后是否保持最终状态。

## 资源

- `references/api-and-pitfalls.md`：两个文件的完整示例、主题与 `ContentTheme` 扩展、逐帧技巧、三种导出方式、常见问题。
- `assets/MyAnimation.cs`：动画类文件模板（类名 `MyAnimation`，只含类定义，可直接复制改名）。
- `assets/MyAnimation_Test.cs`：测试入口模板（`#!`、`#:package NewBeeVG@1.0.1.25`、`#:include MyAnimation.cs`、`using static NewBeeVG.Methods;`、`bool debug = true;` + `args` 覆盖、分场景大括号、`debug` 条件测试、结尾 `run(...)`）。
