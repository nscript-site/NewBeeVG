---
name: newbeevg-skia-animation
description: This skill should be used when creating, editing, or debugging frame-by-frame animations rendered with SkiaSharp in the NewBeeVG codebase, especially in file-based apps under apps/files where a subclass of SKCustomAnimation overrides DrawFrame, or when working with ISKAnimation, SKAnimation, skanim(...), SKCanvas frame callbacks, NBVideoTheme/NBVideoThemes color themes, custom ContentTheme extensions, PNG frame export (SavePNG), MP4/GIF export (SaveMp4/SaveGif), frame index to progress conversion, transparency, frame rate, and bitmap disposal.
---

# NewBeeVG SkiaSharp 逐帧动画

## 用途

在 NewBeeVG 中以 SkiaSharp 画布逐帧绘制动画，并输出：

- 单帧或序列 PNG（`SavePNG`）；
- MP4 视频（`SaveMp4`，H.264）；
- GIF 动图（`SaveGif`）。

推荐做法是**在 file-based app 中新建 `SKCustomAnimation` 的子类并重写 `DrawFrame`**，并在子类中定义两个内部类：

| 内部类 | 作用 |
| --- | --- |
| `ContentSetting` | 承载内容参数：标题、字号、数据、数量等 |
| `ContentTheme` | 当 `NBVideoTheme` 的 7 个基础颜色不够用时，继承它并补充专用颜色（网格线、第二强调色等） |

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

- 需要在 `apps/files/*.cs` 这类 file-based app 中创建逐帧动画；
- 需要为动画选择或切换主题配色（科技、环保、金融等 23 套内置主题）；
- `NBVideoTheme` 现有颜色不够用，需要额外的专用颜色；
- 需要从 `SKCanvas` 渲染结果导出 PNG 或 PNG 序列；
- 需要直接导出 MP4 / GIF，或排查导出失败、帧率不准、尺寸被压缩等问题；
- 需要排查 `BuildFrame` 返回 `null`、透明背景丢失、逐帧导出内存增长等问题。

不适用：需要文本排版、Grid/StackPanel 布局、Typst/Lottie 等 NewBeeVG 组件能力时，应使用 `NBClip` / `NBVisual` 动画方案。

## 先确认的参数

开始编写代码前，明确以下信息，缺失时向用户确认：

1. 输出尺寸（像素宽高）；
2. 总帧数 `frames`，或时长 + 帧率（`frames = 时长 × fps`）；
3. 是否需要透明背景；
4. 配色方案：内置主题是否够用；不够用时需要补充哪些颜色；
5. 输出形态：单张 PNG、PNG 序列、MP4，还是 GIF；
6. 输出目录是否已存在（`SavePNG` 会自动创建，`SaveMp4` / `SaveGif` 不会）。

## 工作流

### 1. 新建 SKCustomAnimation 子类

file-based app 的约定：**顶层语句在前，类型声明在后**。子类放在文件末尾，重写 `DrawFrame` 实现每帧绘制。

```csharp
#!/usr/bin/env dotnet

const int Width = 1080;
const int Height = 1920;
const int Frames = 90;

// 内容参数集中放在子类的内部类 ContentSetting 中
var content = new BouncingBallAnimation.ContentSetting
{
    Title = "NewBee VG",
    TitleSize = 96f,
    Radius = 80f
};

// 内置主题颜色不够时，用 ContentTheme 补充专用颜色
var contentTheme = new BouncingBallAnimation.ContentTheme
{
    // 继承自 NBVideoTheme 的 7 个基础颜色
    Name = "深海",
    Primary = new SKColor(0x2E, 0x6B, 0xFF),
    Background = new SKColor(0x0A, 0x12, 0x1E),
    TextPrimary = new SKColor(0xF2, 0xF6, 0xFF),
    TextSecondary = new SKColor(0x8B, 0x9A, 0xAE),
    Divider = new SKColor(0x24, 0x31, 0x42),
    Positive = new SKColor(0x3D, 0xD5, 0x8C),
    Negative = new SKColor(0xFF, 0x6B, 0x6B),

    // ContentTheme 补充的专用颜色
    Grid = new SKColor(0x1C, 0x2A, 0x3A),
    Highlight = new SKColor(0x66, 0xE0, 0xFF)
};

var anim = new BouncingBallAnimation(Width, Height, Frames, content, contentTheme);

Console.WriteLine($"动画已创建：{anim.Width}x{anim.Height}，共 {anim.Frames} 帧，主题：{anim.Theme.Name}");

public class BouncingBallAnimation : SKCustomAnimation
{
    private readonly ContentSetting _content;
    private readonly ContentTheme _theme;

    // 推荐签名：尺寸 + 帧数 + 内容配置 + 可选扩展主题
    public BouncingBallAnimation(
        int width,
        int height,
        int frames,
        ContentSetting content,
        ContentTheme? contentTheme = null)
        : base(width, height, frames, contentTheme ?? ContentTheme.CreateDefault())
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));

        // 基类 Theme 一定是 ContentTheme（传入的实例或 CreateDefault 的实例）
        _theme = (ContentTheme)Theme;
    }

    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        canvas.Clear(_theme.Background);

        // 帧号 -> 0..1 进度
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

        float cx = size.Width / 2f;
        float cy = size.Height * (0.2f + 0.6f * MathF.Abs(1f - 2f * t));

        // 基础主题色
        using var paint = new SKPaint { IsAntialias = true, Color = _theme.Primary };
        canvas.DrawCircle(cx, cy, _content.Radius, paint);

        // ContentTheme 补充的专用颜色
        using var grid = new SKPaint
        {
            IsAntialias = true,
            Color = _theme.Grid,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f
        };
        canvas.DrawLine(0, cy, size.Width, cy, grid);

        using var highlight = new SKPaint { IsAntialias = true, Color = _theme.Highlight };
        canvas.DrawCircle(cx, cy, _content.Radius * 0.25f, highlight);

        using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei") ?? SKTypeface.Default;
        using var font = new SKFont(typeface, _content.TitleSize);
        using var titlePaint = new SKPaint { IsAntialias = true, Color = _theme.TextPrimary };
        canvas.DrawText(_content.Title, cx, size.Height - 140f, SKTextAlign.Center, font, titlePaint);
    }

    /// <summary>
    /// 动画内容配置：把标题、字号、半径等可变输入集中在一个内部类中。
    /// </summary>
    public class ContentSetting
    {
        /// <summary>标题文字。</summary>
        public string Title { get; set; } = "";

        /// <summary>标题字号（像素）。</summary>
        public float TitleSize { get; set; } = 96f;

        /// <summary>圆形半径（像素）。</summary>
        public float Radius { get; set; } = 80f;
    }

    /// <summary>
    /// 扩展主题：当 NBVideoTheme 的 7 个基础颜色不够用时，
    /// 继承它并补充网格线、第二强调色等专用颜色。
    /// </summary>
    public class ContentTheme : NBVideoTheme
    {
        /// <summary>网格线 / 辅助线颜色。</summary>
        public SKColor Grid { get; set; } = new SKColor(0x2A, 0x33, 0x40);

        /// <summary>第二种强调色（在 Primary 之外）。</summary>
        public SKColor Highlight { get; set; } = new SKColor(0xFF, 0xC8, 0x57);

        /// <summary>未传入主题时使用的默认配色（复制自自然主题）。</summary>
        public static ContentTheme CreateDefault() => From(NBVideoThemes.Nature);

        /// <summary>基于任一内置主题复制出一套可扩展的 ContentTheme。</summary>
        public static ContentTheme From(NBVideoTheme theme)
        {
            ArgumentNullException.ThrowIfNull(theme);

            return new ContentTheme
            {
                Name = theme.Name,
                Primary = theme.Primary,
                Background = theme.Background,
                TextPrimary = theme.TextPrimary,
                TextSecondary = theme.TextSecondary,
                Divider = theme.Divider,
                Positive = theme.Positive,
                Negative = theme.Negative
            };
        }
    }
}
```

要点：

- **两个内部类各司其职**：`ContentSetting` 存业务内容，`ContentTheme` 存颜色；都不要新增顶层类型，避免破坏 file-based app 的类型顺序；
- **构造签名固定**：`(width, height, frames, ContentSetting content, ContentTheme? contentTheme = null)`，后续新增字段只改内部类；
- **`ContentTheme` 必须继承 `NBVideoTheme`**：这样可以直接传给基类 `SKCustomAnimation`，基类的 `Theme` 实际就是 `ContentTheme` 实例；
- **默认主题**：`contentTheme` 为 `null` 时传 `ContentTheme.CreateDefault()`，它是从 `NBVideoThemes.Nature` 复制的新实例，可安全修改；
- **取回扩展颜色**：构造函数中 `_theme = (ContentTheme)Theme;`，之后直接用 `_theme.Grid`、`_theme.Highlight`；
- **空值校验**：构造函数中校验 `content`，避免 `DrawFrame` 里出现空引用；
- 基类 `SKCustomAnimation` 已把 `OnFrame` 指向 `DrawFrame`，子类只需重写方法，不要再手动设置 `OnFrame`；
- `Draw` 在调用 `DrawFrame` 前已校验帧号与尺寸，因此重写体内不必重复判断 `frame` 越界；
- `Frames` / `Width` / `Height` / `Theme` 是基类公开成员，可直接在 `DrawFrame` 中使用；
- 在 `apps/files/` 下，`NewBeeVG`、`SkiaSharp`、静态 `Methods` 已通过 `Directory.Build.props` 全局导入，无需再写 `using`。

### 2. 选择主题色

`Theme` 是 `NBVideoTheme` 类型，包含 `Background`、`TextPrimary`、`TextSecondary`、`Primary`、`Divider`、`Positive`、`Negative`、`Name`。

内置 23 套主题位于 `NBVideoThemes`：

```csharp
var baseTheme = NBVideoThemes.AI;                    // 按静态属性取
var contentTheme = MyAnimation.ContentTheme.From(baseTheme); // 转成可扩展主题

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

### 3. 帧号换算进度

`frame` 范围是 `0 ~ Frames-1`。按用途选择分母：

```csharp
// 一次性动画：最后一帧恰好为 1.0
float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

// 循环动画：避免首尾帧重复
float t = frame / (float)Frames;
```

### 4. 先验证单帧

先保存中间帧检查构图与颜色，再批量导出：

```csharp
anim.SavePNG(0, "output/preview.png");
```

`SavePNG` 会自动创建目标目录。

### 5. 导出 PNG 序列

```csharp
for (int i = 0; i < anim.Frames; i++)
{
    if (!anim.SavePNG(i, $"output/frames/{i:0000}.png"))
        Console.WriteLine($"第 {i} 帧渲染失败");
}
```

使用 4 位补零命名，便于 ffmpeg 或后续工具按序号读取。

### 6. 直接导出 MP4 / GIF

```csharp
Directory.CreateDirectory("output"); // SaveMp4 / SaveGif 不自动建目录

// MP4：H.264，时长 = Frames / fps
anim.SaveMp4(30, "output/animation.mp4");

// GIF：帧率建议使用 10/20/25/50，输出尺寸超过 1200 像素会被等比缩小
anim.SaveGif(25, "output/animation.gif");
```

`SaveMp4` 会先调用 `NBGlobal.CheckOrLoadFFmpeg()` 加载 ffmpeg。

### 7. 手动合成（可自定义编码参数）

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
| `Frames` / `Width` / `Height` | 总帧数、单帧像素尺寸 |
| `BuildFrame(int frame)` | 返回新的 `SKBitmap`；越界、尺寸非法或回调为空时返回 `null`，调用方负责释放 |
| `Draw(SKCanvas canvas, int frame)` | 直接把帧绘制到已有画布，不创建位图；成功返回 `true` |
| `SavePNG(int frame, string filePath)` | 渲染并保存为 PNG；目录自动创建；成功返回 `true`，无法渲染返回 `false` |
| `SaveMp4(double fps, string filePath)` | 全部帧编码为 MP4；依赖 ffmpeg；不返回成功状态，不自动建目录 |
| `SaveGif(double fps, string filePath)` | 全部帧编码为 GIF；尺寸上限 1200，二值透明；不自动建目录 |

## 关键约束

- **内容参数封装**：继承 `SKCustomAnimation` 时，把标题、字号、数据等输入封装为**内部类** `ContentSetting`，构造签名保持 `(width, height, frames, content, contentTheme = null)`；不要把十几个参数平铺在构造函数上。
- **主题颜色扩展**：`NBVideoTheme` 的 7 个颜色不够用时，**不要往构造函数里继续加颜色参数**，而是定义内部类 `ContentTheme : NBVideoTheme`，把额外颜色作为属性，并整体传入构造函数。
- **ContentTheme 必须继承 NBVideoTheme**：否则无法直接传给基类，也无法通过 `(ContentTheme)Theme` 取回扩展颜色。
- **默认主题要复制**：`contentTheme` 为 `null` 时用 `ContentTheme.CreateDefault()`（复制 `NBVideoThemes.Nature`），不要直接返回共享静态实例，避免被修改后污染全局。
- **file-based app 的类型顺序**：顶层语句必须在前，`class` 声明只能放在文件末尾；`ContentSetting` / `ContentTheme` 作为子类内部类，不占用顶层类型位置。
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

1. 子类是否继承 `SKCustomAnimation`，`DrawFrame` 是否使用 `protected override`；
2. 构造函数是否为 `(width, height, frames, content, contentTheme = null)`，内容参数是否封装在内部类 `ContentSetting` 中；
3. 主题颜色不够用时，是否通过内部类 `ContentTheme : NBVideoTheme` 扩展，而不是继续增加构造参数；
4. `ContentTheme` 是否提供 `From(theme)` / `CreateDefault()`，默认值是否使用复制而非共享静态实例；
5. file-based app 中类声明是否位于顶层语句之后；
6. 是否通过 `Theme` / `_theme` 取色，而不是散落的硬编码颜色；
7. `DrawFrame` 是否每帧清空画布；
8. 是否误改 `OnFrame`，导致 `DrawFrame` 不生效；
9. 是否处理了 `BuildFrame` / `SavePNG` 的 `null` / `false` 返回；
10. 透明背景是否按需求用 `SKColors.Transparent`（或 `Theme.Background`）清空画布；
11. `fps` 是否大于 0；GIF 是否使用了合适的帧率；
12. `SaveMp4` / `SaveGif` 的输出目录是否已存在。

## 资源

- `references/api-and-pitfalls.md`：完整示例、主题与 `ContentTheme` 扩展、逐帧技巧、三种导出方式、常见问题。
- `assets/animation-template.cs`：基于 `SKCustomAnimation` + `ContentSetting` + `ContentTheme` 的可复制模板。
