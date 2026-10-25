---
name: newbeevg-skia-animation
description: This skill should be used when creating, editing, or debugging frame-by-frame animations rendered with SkiaSharp in the NewBeeVG codebase, especially in file-based apps under apps/files where a subclass of SKCustomAnimation overrides DrawFrame, or when working with ISKAnimation, SKAnimation, SKCanvas frame callbacks, PNG frame export (SavePNG), MP4/GIF export (SaveMp4/SaveGif), frame index to progress conversion, transparency, frame rate, and bitmap disposal.
---

# NewBeeVG SkiaSharp 逐帧动画

## 用途

在 NewBeeVG 中以 SkiaSharp 画布逐帧绘制动画，并输出：

- 单帧或序列 PNG（`SavePNG`）；
- MP4 视频（`SaveMp4`，H.264）；
- GIF 动图（`SaveGif`）。

推荐做法是**在 file-based app 中新建 `SKCustomAnimation` 的子类并重写 `DrawFrame`**。

该动画体系与基于 Avalonia 布局的 `NBVisual` / `NBClip` 动画相互独立：它只操作原始 `SKCanvas`，不参与布局计算。

## 何时使用

在以下场景加载本技能：

- 需要在 `apps/files/*.cs` 这类 file-based app 中创建逐帧动画；
- 需要从 `SKCanvas` 渲染结果导出 PNG 或 PNG 序列；
- 需要直接导出 MP4 / GIF，或排查导出失败、帧率不准、尺寸被压缩等问题；
- 需要排查 `BuildFrame` 返回 `null`、透明背景丢失、逐帧导出内存增长等问题。

不适用：需要文本排版、Grid/StackPanel 布局、Typst/Lottie 等 NewBeeVG 组件能力时，应使用 `NBClip` / `NBVisual` 动画方案。

## 先确认的参数

开始编写代码前，明确以下信息，缺失时向用户确认：

1. 输出尺寸（像素宽高）；
2. 总帧数 `frames`，或时长 + 帧率（`frames = 时长 × fps`）；
3. 是否需要透明背景；
4. 输出形态：单张 PNG、PNG 序列、MP4，还是 GIF；
5. 输出目录是否已存在（`SavePNG` 会自动创建，`SaveMp4` / `SaveGif` 不会）。

## 工作流

### 1. 新建 SKCustomAnimation 子类

file-based app 的约定：**顶层语句在前，类型声明在后**。子类放在文件末尾，重写 `DrawFrame` 实现每帧绘制。

```csharp
#!/usr/bin/env dotnet

const int Width = 1080;
const int Height = 1920;
const int Frames = 90;

var anim = new BouncingBallAnimation(Width, Height, Frames);

// 后续导出步骤见下
Console.WriteLine($"动画已创建：{anim.Width}x{anim.Height}，共 {anim.Frames} 帧");

public class BouncingBallAnimation : SKCustomAnimation
{
    public BouncingBallAnimation(int width, int height, int frames)
        : base(width, height, frames)
    {
    }

    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        // 每帧必须重新铺背景，避免残影
        // 需要透明背景时改为 canvas.Clear(SKColors.Transparent)
        canvas.Clear(SKColors.White);

        // 帧号 -> 0..1 进度
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

        float cx = size.Width / 2f;
        float cy = size.Height * (0.2f + 0.6f * MathF.Abs(1f - 2f * t));

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0xFFFF6B35)
        };
        canvas.DrawCircle(cx, cy, 80f, paint);
    }
}
```

要点：

- 基类 `SKCustomAnimation` 已把 `OnFrame` 指向 `DrawFrame`，子类只需重写方法，不要再手动设置 `OnFrame`；
- `Draw` 在调用 `DrawFrame` 前已校验帧号与尺寸，因此重写体内不必重复判断 `frame` 越界；
- `Frames` / `Width` / `Height` 是基类公开属性，可直接在 `DrawFrame` 中使用；
- 在 `apps/files/` 下，`NewBeeVG`、`SkiaSharp`、静态 `Methods` 已通过 `Directory.Build.props` 全局导入，无需再写 `using`。

### 2. 帧号换算进度

`frame` 范围是 `0 ~ Frames-1`。按用途选择分母：

```csharp
// 一次性动画：最后一帧恰好为 1.0
float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

// 循环动画：避免首尾帧重复
float t = frame / (float)Frames;
```

### 3. 先验证单帧

先保存中间帧检查构图与颜色，再批量导出：

```csharp
anim.SavePNG(0, "output/preview.png");
```

`SavePNG` 会自动创建目标目录。

### 4. 导出 PNG 序列

```csharp
for (int i = 0; i < anim.Frames; i++)
{
    if (!anim.SavePNG(i, $"output/frames/{i:0000}.png"))
        Console.WriteLine($"第 {i} 帧渲染失败");
}
```

使用 4 位补零命名，便于 ffmpeg 或后续工具按序号读取。

### 5. 直接导出 MP4 / GIF

```csharp
Directory.CreateDirectory("output"); // SaveMp4 / SaveGif 不自动建目录

// MP4：H.264，时长 = Frames / fps
anim.SaveMp4(30, "output/animation.mp4");

// GIF：帧率建议使用 10/20/25/50，输出尺寸超过 1200 像素会被等比缩小
anim.SaveGif(25, "output/animation.gif");
```

`SaveMp4` 会先调用 `NBGlobal.CheckOrLoadFFmpeg()` 加载 ffmpeg。

### 6. 手动合成（可自定义编码参数）

需要控制编码器参数时，先导出 PNG 序列再调用 ffmpeg：

```bash
ffmpeg -framerate 30 -i output/frames/%04d.png \
       -c:v libx264 -pix_fmt yuv420p -crf 18 \
       output/animation.mp4
```

## API 摘要

| 成员 | 说明 |
| --- | --- |
| `SKCustomAnimation(int width, int height, int frames)` | 推荐基类；子类重写 `DrawFrame` 提供每帧绘制 |
| `SKAnimation(int width, int height, int frames, Action<...>? onFrame)` | 回调式实现；`onFrame` 可为 `null` |
| `Frames` / `Width` / `Height` | 总帧数、单帧像素尺寸 |
| `BuildFrame(int frame)` | 返回新的 `SKBitmap`；越界、尺寸非法或回调为空时返回 `null`，调用方负责释放 |
| `Draw(SKCanvas canvas, int frame)` | 直接把帧绘制到已有画布，不创建位图；成功返回 `true` |
| `SavePNG(int frame, string filePath)` | 渲染并保存为 PNG；目录自动创建；成功返回 `true`，无法渲染返回 `false` |
| `SaveMp4(double fps, string filePath)` | 全部帧编码为 MP4；依赖 ffmpeg；不返回成功状态，不自动建目录 |
| `SaveGif(double fps, string filePath)` | 全部帧编码为 GIF；尺寸上限 1200，二值透明；不自动建目录 |

## 关键约束

- **file-based app 的类型顺序**：顶层语句必须在前，`class` 声明只能放在文件末尾；子类放错位置会导致编译失败。
- **透明位图（预乘 Alpha）**：`BuildFrame` 使用 `SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul)` 创建位图，支持透明通道，`SavePNG` 导出的 PNG 会保留 Alpha。

```csharp
var info = new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
var bmp = new SKBitmap(info);
```

- **清空画布决定透明度**：每帧都用 `canvas.Clear(...)` 重新开始。需要透明背景时传 `SKColors.Transparent`；需要实色背景时传具体颜色（如 `SKColors.White`）。
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
2. file-based app 中类声明是否位于顶层语句之后；
3. `DrawFrame` 是否每帧清空画布；
4. 是否误改 `OnFrame`，导致 `DrawFrame` 不生效；
5. 是否处理了 `BuildFrame` / `SavePNG` 的 `null` / `false` 返回；
6. 透明背景是否按需求用 `SKColors.Transparent`（或具体颜色）清空画布；
7. `fps` 是否大于 0；GIF 是否使用了合适的帧率；
8. `SaveMp4` / `SaveGif` 的输出目录是否已存在。

## 资源

- `references/api-and-pitfalls.md`：完整示例、逐帧技巧、三种导出方式的细节、常见问题。
- `assets/animation-template.cs`：基于 `SKCustomAnimation` 的可复制模板。
