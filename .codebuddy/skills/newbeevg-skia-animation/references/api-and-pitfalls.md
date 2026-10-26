# SKAnimation API 与常见问题

## 接口定义

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

`SKAnimation` 是默认实现：通过构造函数传入的 `OnFrame` 回调逐帧绘制。

`Draw` 直接把指定帧画到已有画布，不创建中间位图，适合嵌入其他渲染流程；调用方负责画布的变换与裁切状态。

绘制逻辑较多时，可继承 `SKCustomAnimation`（`SKAnimation` 的子类），重写 `DrawFrame(SKCanvas canvas, SKSize size, int frame)` 即可，无需在构造时传入回调。

`SKCustomAnimation` 还提供主题色：构造时可传入 `NBVideoTheme`，颜色不够用时传入继承自它的 `ContentTheme`；为空则使用 `NBVideoThemes.Nature`。子类通过 `Theme` 属性取用 `Background` / `TextPrimary` / `Primary` 等颜色。

## 主题色（NBVideoTheme / NBVideoThemes）

`NBVideoTheme` 描述一套配色，包含以下成员：

| 成员 | 用途 |
| --- | --- |
| `Name` | 中文主题名，如「海洋」 |
| `Primary` | 强调色：标题装饰、关键图形 |
| `Background` | 画面底色 |
| `TextPrimary` | 标题与正文 |
| `TextSecondary` | 副标题、注释 |
| `Divider` | 细线、边框、表格线 |
| `Positive` / `Negative` | 数据可视化的正/负值 |

20 套内置主题位于 `NBVideoThemes` 的静态属性上：

```csharp
var theme = NBVideoThemes.Ocean;
var name = theme.Name;   // "海洋"
```

| 属性 | 名称 | 明暗 | 属性 | 名称 | 明暗 |
| --- | --- | --- | --- | --- | --- |
| `Tech` | 科技 | 暗 | `Minimal` | 极简 | 浅 |
| `Eco` | 环保 | 浅 | `Cyberpunk` | 赛博朋克 | 暗 |
| `Finance` | 金融 | 浅 | `Luxury` | 奢华 | 暗 |
| `Healthcare` | 医疗 | 浅 | `Retro` | 复古 | 浅 |
| `Education` | 教育 | 浅 | `Candy` | 糖果 | 浅 |
| `Media` | 影视 | 暗 | `Arctic` | 极地 | 浅 |
| `Retail` | 零售 | 浅 | `Forest` | 深林 | 暗 |
| `Nature` | 自然 | 浅 | `Corporate` | 商务 | 浅 |
| `Ocean` | 海洋 | 浅 | `AI` | 智能 | 浅 |
| `Sunset` | 日落 | 暗 | `Podcast` | 播客 | 暗 |

遍历与预览：

```csharp
foreach (var theme in NBVideoThemes.AllThemes())
{
    using var preview = theme.Preview(1280, 720); // 生成色板图
    // 保存或继续使用 preview
}
```

注意：

- 静态主题是共享实例，不要修改其颜色属性；需要微调时先 `new NBVideoTheme { ... }` 再覆盖；
- `AllThemes()` 每次返回新列表，可自由增删排序；
- `Preview(width, height)` 返回 `SKBitmap`，由调用方 `Dispose()`；
- 构造 `SKCustomAnimation` 时传入 `ContentTheme`（继承自 `NBVideoTheme`），子类通过 `Theme` / `_theme` 取色，切换主题无需改动绘制代码。

## 完整示例（file-based app）

在 `apps/files/` 下新建 `.cs` 文件：顶层语句在前，`SKCustomAnimation` 子类声明放在文件末尾。

生成 90 帧、1080×1920 的动画：中心圆呼吸缩放、外圈刻度旋转、底部显示帧号。

```csharp
#!/usr/bin/env dotnet

const int Width = 1080;
const int Height = 1920;
const int Frames = 90;

// 内容参数集中放在子类的内部类 ContentSetting 中
var content = new ClockAnimation.ContentSetting
{
    TickCount = 12,
    LabelSize = 48f,
    ShowFrameLabel = true
};

// 主题颜色不够时用 ContentTheme 扩展；也可用 ContentTheme.From(NBVideoThemes.Ocean) 基于内置主题
var contentTheme = new ClockAnimation.ContentTheme
{
    Name = "深海",
    Primary = new SKColor(0x2E, 0x6B, 0xFF),
    Background = new SKColor(0x0A, 0x12, 0x1E),
    TextPrimary = new SKColor(0xF2, 0xF6, 0xFF),
    TextSecondary = new SKColor(0x8B, 0x9A, 0xAE),
    Divider = new SKColor(0x24, 0x31, 0x42),
    Positive = new SKColor(0x3D, 0xD5, 0x8C),
    Negative = new SKColor(0xFF, 0x6B, 0x6B),
    Grid = new SKColor(0x1C, 0x2A, 0x3A),
    Highlight = new SKColor(0x66, 0xE0, 0xFF)
};

var anim = new ClockAnimation(Width, Height, Frames, content, contentTheme);

anim.SavePNG(0, "output/preview.png");

public class ClockAnimation : SKCustomAnimation
{
    private readonly ContentSetting _content;
    private readonly ContentTheme _theme;

    // 推荐签名：尺寸 + 帧数 + 内容配置 + 可选扩展主题
    public ClockAnimation(
        int width,
        int height,
        int frames,
        ContentSetting content,
        ContentTheme? contentTheme = null)
        : base(width, height, frames, contentTheme ?? ContentTheme.CreateDefault())
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));

        // 基类 Theme 一定是 ContentTheme（传入实例或 CreateDefault 实例）
        _theme = (ContentTheme)Theme;
    }

    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        canvas.Clear(_theme.Background);

        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

        float cx = size.Width / 2f;
        float cy = size.Height / 2f;
        float radius = 160f + 60f * MathF.Sin(t * MathF.PI * 2f);

        using (var fill = new SKPaint { IsAntialias = true, Color = _theme.Primary })
        {
            canvas.DrawCircle(cx, cy, radius, fill);
        }

        canvas.Save();
        canvas.Translate(cx, cy);
        canvas.RotateDegrees(t * 360f);

        using (var tick = new SKPaint
        {
            IsAntialias = true,
            Color = _theme.TextSecondary,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 8
        })
        {
            for (int i = 0; i < _content.TickCount; i++)
            {
                float step = 360f / _content.TickCount;
                float angle = i * step * MathF.PI / 180f;
                float x0 = MathF.Cos(angle) * (radius + 40f);
                float y0 = MathF.Sin(angle) * (radius + 40f);
                float x1 = MathF.Cos(angle) * (radius + 90f);
                float y1 = MathF.Sin(angle) * (radius + 90f);
                canvas.DrawLine(x0, y0, x1, y1, tick);
            }
        }

        canvas.Restore();

        if (_content.ShowFrameLabel)
        {
            using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei") ?? SKTypeface.Default;
            using var font = new SKFont(typeface, _content.LabelSize);
            using var textPaint = new SKPaint { IsAntialias = true, Color = _theme.TextPrimary };

            canvas.DrawText(
                $"Frame {frame:000} / {Frames - 1:000}",
                cx,
                size.Height - 140f,
                SKTextAlign.Center,
                font,
                textPaint);
        }
    }

    /// <summary>
    /// 动画内容配置：把可变输入集中在一个内部类中，避免构造函数参数膨胀。
    /// </summary>
    public class ContentSetting
    {
        /// <summary>刻度数量。</summary>
        public int TickCount { get; set; } = 12;

        /// <summary>底部帧号文字大小（像素）。</summary>
        public float LabelSize { get; set; } = 48f;

        /// <summary>是否显示底部帧号。</summary>
        public bool ShowFrameLabel { get; set; } = true;
    }

    /// <summary>
    /// 扩展主题：NBVideoTheme 的 7 个基础颜色不够用时，
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

- **内容参数封装**：所有可变输入放进内部类 `ContentSetting`，构造签名固定为 `(width, height, frames, content, contentTheme = null)`；新增字段时只需改 `ContentSetting`，调用方与构造函数不用变；
- **主题颜色扩展**：`NBVideoTheme` 的 7 个颜色不够用时，不要继续增加构造参数，而是定义内部类 `ContentTheme : NBVideoTheme` 补充属性并整体传入；
- **内部类位置**：`ContentSetting` / `ContentTheme` 定义在动画子类内部（通常放在类末尾），通过 `ClockAnimation.ContentSetting` 实例化；不要新增顶层类型，避免破坏 file-based app 的类型顺序；
- **空值校验**：构造函数中校验 `content`，避免 `DrawFrame` 里出现空引用；
- 类声明必须在顶层语句之后，否则 file-based app 无法编译；
- `SKCustomAnimation` 构造时已把 `OnFrame` 指向 `DrawFrame`，子类不要再赋值 `OnFrame`；
- `Draw` 会先校验帧号与尺寸，`DrawFrame` 内无需重复判断；
- `apps/files/` 已通过 `Directory.Build.props` 全局导入 `NewBeeVG`、`SkiaSharp` 与静态 `Methods`。

### 内容参数封装（ContentSetting）

推荐把子类的可变输入统一封装为内部类，而不是堆在构造函数参数上：

```csharp
public class ReportAnimation : SKCustomAnimation
{
    private readonly ContentSetting _content;

    public ReportAnimation(
        int width,
        int height,
        int frames,
        ContentSetting content,
        ContentTheme? contentTheme = null)          // ContentTheme 定义见下
        : base(width, height, frames, contentTheme ?? ContentTheme.CreateDefault())
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        // 直接读取 _content 中的字段
    }

    /// <summary>动画内容配置（纯数据）。</summary>
    public class ContentSetting
    {
        /// <summary>标题。</summary>
        public string Title { get; set; } = "";

        /// <summary>副标题。</summary>
        public string Subtitle { get; set; } = "";

        /// <summary>数据项。</summary>
        public IReadOnlyList<float> Values { get; set; } = Array.Empty<float>();
    }
}
```

约定：

- 构造函数签名保持 `(int width, int height, int frames, ContentSetting content, ContentTheme? contentTheme = null)`；
- `ContentSetting` 声明为**子类的嵌套类**，访问方式为 `ReportAnimation.ContentSetting`；
- 内容类只存放数据（POCO），不要持有 `SKBitmap`、`SKPaint`、`SKCanvas` 等原生对象；确需持有时应在文档中写明释放责任；
- 给字段提供合理默认值，调用方只覆盖需要变化的项；
- 在构造函数中校验 `content != null`，让错误尽早暴露。

### 主题扩展（ContentTheme）

当 `NBVideoTheme` 的 7 个颜色不够用时，定义内部类 `ContentTheme` 继承它并补充专用颜色：

```csharp
public class ContentTheme : NBVideoTheme
{
    /// <summary>网格线 / 辅助线。</summary>
    public SKColor Grid { get; set; } = new SKColor(0x2A, 0x33, 0x40);

    /// <summary>第二种强调色。</summary>
    public SKColor Highlight { get; set; } = new SKColor(0xFF, 0xC8, 0x57);

    /// <summary>警告色。</summary>
    public SKColor Warning { get; set; } = new SKColor(0xFF, 0xB0, 0x2E);

    /// <summary>默认配色：复制自然主题，避免暴露共享静态实例。</summary>
    public static ContentTheme CreateDefault() => From(NBVideoThemes.Nature);

    /// <summary>从任一内置主题复制。</summary>
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
```

在动画子类中接住并保持强类型：

```csharp
private readonly ContentTheme _theme;

public MyAnimation(
    int width,
    int height,
    int frames,
    ContentSetting content,
    ContentTheme? contentTheme = null)
    : base(width, height, frames, contentTheme ?? ContentTheme.CreateDefault())
{
    _content = content ?? throw new ArgumentNullException(nameof(content));
    _theme = (ContentTheme)Theme;   // 基类 Theme 一定由 ContentTheme 赋值
}
```

要点：

- `ContentTheme` 必须继承 `NBVideoTheme`，才能直接传给基类，并通过 `(ContentTheme)Theme` 取回扩展颜色；
- 未传 `contentTheme` 时使用 `CreateDefault()`（复制 `NBVideoThemes.Nature`），不要直接返回静态共享实例；
- 需要基于内置主题扩展时用 `ContentTheme.From(NBVideoThemes.Ocean)`，之后再覆盖额外颜色；
- 新增颜色只改 `ContentTheme`，构造函数签名保持稳定；
- 不需要扩展颜色时，仍可直接使用内置 `NBVideoTheme`。

## 帧号与插值

```csharp
// 一次性动画
float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

// 循环动画
float t = frame / (float)Frames;

// 线性插值
float value = start + (end - start) * t;

// ease-out
float eased = 1f - MathF.Pow(1f - t, 3f);
```

`size` 参数与 `Width` / `Height` 相同，使用它可以避免在回调里捕获外部尺寸变量。

## 导出方式

### 单帧 PNG

```csharp
anim.SavePNG(0, "output/preview.png");
```

`SavePNG` 会自动创建目标目录。

### PNG 序列

```csharp
for (int i = 0; i < anim.Frames; i++)
{
    if (!anim.SavePNG(i, $"output/frames/{i:0000}.png"))
        Console.WriteLine($"第 {i} 帧渲染失败");
}
```

### 直接导出 MP4

```csharp
Directory.CreateDirectory("output"); // SaveMp4 不自动建目录
anim.SaveMp4(30, "output/animation.mp4");
```

行为说明：

- 调用 `NBGlobal.CheckOrLoadFFmpeg()` 加载 ffmpeg，未安装/加载失败会在该步骤报错；
- 内部创建 `MediaWriter(filePath, Width, Height, fps, containAudio: true)`，视频编码为 H.264；
- 逐帧 `BuildFrame` → `WriteFrame` → 释放，某一帧返回 `null` 时提前结束；
- 每 10 帧打印一次进度；
- `writer.Close()` 写入文件尾，异常或提前结束前都必须执行；
- 方法无返回值，不会抛出「帧渲染失败」的异常。

时长 = `Frames / fps`。例如 90 帧 @ 30fps = 3 秒。

### 直接导出 GIF

```csharp
Directory.CreateDirectory("output"); // SaveGif 不自动建目录
anim.SaveGif(25, "output/animation.gif");
```

行为说明：

- 帧延迟 `delayHundredths = round(100 / fps)`，单位为 1/100 秒；
- `GifEncoderOptions.MaxWidth` / `MaxHeight` 均为 1200，超出时等比缩小；
- GIF 只支持二值透明，Alpha 阈值 128，颜色量化为 256 色；
- `using var gifEncoder` 在 Dispose 时写入文件尾，完成封装；
- 方法无返回值，某帧渲染失败时提前结束。

### 手动合成（自定义编码参数）

```bash
ffmpeg -framerate 30 -i output/frames/%04d.png \
       -c:v libx264 -pix_fmt yuv420p -crf 18 \
       output/animation.mp4
```

## 方法行为对照

| 方法 | 输出 | 是否自动建目录 | 失败行为 | 外部依赖 |
| --- | --- | --- | --- | --- |
| `SavePNG(int, string)` | PNG 单帧 | 是 | 返回 `false`；路径为空或编码失败抛异常 | SkiaSharp |
| `SaveMp4(double, string)` | MP4 / H.264 | 否 | 渲染 `null` 时提前结束；ffmpeg 问题抛异常 | ffmpeg |
| `SaveGif(double, string)` | GIF | 否 | 渲染 `null` 时提前结束；编码问题抛异常 | 内置 `GifEncoder` |

## 不落盘使用

```csharp
using var bmp = anim.BuildFrame(0);

using var image = SKImage.FromBitmap(bmp);

// Avalonia 预览
var writeable = DrawingHelper.ToWriteableBitmap(bmp);

// 其他格式
using var data = image.Encode(SKEncodedImageFormat.Webp, 90);
data.SaveTo("output/preview.webp");
```

## 常见问题

### 透明背景与预乘 Alpha

`SKAnimation.BuildFrame` 使用带 Alpha 的位图：

```csharp
var info = new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
var bmp = new SKBitmap(info);
```

导出透明 PNG 的要点：

1. 每帧用透明色清空画布：

```csharp
canvas.Clear(SKColors.Transparent);
```

2. 只绘制需要保留的内容，未绘制区域保持透明；
3. 使用 `SavePNG` 导出 PNG（PNG 支持 Alpha，不要改用 JPEG）。

注意：位图为 `Premul`（预乘 Alpha），半透明颜色按预乘规则合成。若外部数据是直通 Alpha（Unpremul）像素，不要直接内存拷贝进该位图，应通过 `SKCanvas.DrawBitmap` 绘制或先做预乘转换。

### 导出的 PNG 背景是黑色

常见原因：

- 误用了 `new SKBitmap(w, h, true)`（第三个参数 `isOpaque` 为 `true` 表示不透明）；
- 忘记 `canvas.Clear(...)`，位图初始为透明黑或残留上一帧像素；
- 用不透明颜色清空画布（如 `SKColors.Black`、`new SKColor(0xFF101820)`）。

### SavePNG 返回 false

原因：`frame` 越界、宽高小于等于 0、或 `OnFrame` 为 `null`。

### 子类编译失败或 DrawFrame 不生效

- file-based app 中 `class` 必须写在顶层语句之后，否则编译失败；
- 必须使用 `protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)`，签名不一致时不会构成重写；
- 构造函数必须调用 `base(width, height, frames)`（基类没有无参构造函数）；
- 不要在子类中再给 `OnFrame` 赋值，`SKCustomAnimation` 构造时已将其指向 `DrawFrame`，覆盖后重写的方法不会执行。

### SavePNG 抛出异常

- `ArgumentException`：`filePath` 为空或仅空白字符；
- `InvalidOperationException`：PNG 编码失败（`SKData` 为空或长度为 0）。

### SaveMp4 / SaveGif 报错或没有生成文件

- 输出目录不存在：两者都不会自动创建目录，先调用 `Directory.CreateDirectory`；
- 帧数或尺寸为 0：`BuildFrame` 直接返回 `null`，循环立即结束，可能生成空文件；
- MP4：ffmpeg 未正确加载，检查 `NBGlobal.CheckOrLoadFFmpeg()` 是否能通过；
- 输出路径被占用或无写权限。

### GIF 播放速度与设定帧率不一致

GIF 帧延迟精度为 1/100 秒。例如：

| 目标 fps | delay = round(100/fps) | 实际 fps |
| --- | --- | --- |
| 25 | 4 | 25 |
| 20 | 5 | 20 |
| 30 | 3 | ≈33.3 |
| 24 | 4 | 25 |

需要精确帧率时使用 MP4；GIF 建议取 10 / 20 / 25 / 50 等能整除 100 的帧率。

### GIF 变小、变糊或透明边缘丢失

- 尺寸超过 1200×1200 会被等比缩小；
- 颜色被量化为 256 色，渐变可能出现色带；
- GIF 只有二值透明，半透明边缘会被阈值切分为实色或全透明。

需要高质量与平滑透明度时使用 MP4 或 PNG 序列。

### 帧率不生效

`SKAnimation` 本身没有 fps 字段，只描述第几帧：

- `SaveMp4` / `SaveGif` 的播放速度由传入的 `fps` 参数决定；
- PNG 序列需要由 ffmpeg 的 `-framerate`（或后续工具）指定；
- 接入 NewBeeVG 视频管线时由 `NBStage.FrameRate` 决定。

### 逐帧导出内存持续增长

`BuildFrame` 每次新建 `SKBitmap`，必须由调用方释放。不要在集合中累积位图，也不要只依赖 GC 回收原生内存。

### 图片格式与扩展名不一致

`SavePNG` 固定输出 PNG。写成 `.jpg` 得到的仍是 PNG 内容，需要其他格式时自行调用 `SKImage.Encode`。

### 并发渲染

`SKAnimation` 不保存绘制状态，可并发调用 `BuildFrame`，前提是回调内部不访问共享可变状态。`SaveMp4` / `SaveGif` 内部持有编码器状态，不要并发调用。
