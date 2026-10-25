# SKAnimation API 与常见问题

## 接口定义

```csharp
public interface ISKAnimation
{
    int Frames { get; }
    int Width { get; }
    int Height { get; }
    SKBitmap? BuildFrame(int frame);
    bool SavePNG(int frame, string filePath);
}
```

`SKAnimation` 是默认实现：通过构造函数传入的 `OnFrame` 回调逐帧绘制。除接口成员外，`SKAnimation` 还提供两个整体导出方法：

```csharp
public void SaveMp4(double fps, string filePath);
public void SaveGif(double fps, string filePath);
```

## 完整示例

生成 90 帧、1080×1920 的动画：中心圆呼吸缩放、外圈刻度旋转、底部显示帧号。

```csharp
using NewBeeVG;
using SkiaSharp;

const int Width = 1080;
const int Height = 1920;
const int Frames = 90;

var anim = Methods.skanim(new SKSize(Width, Height), Frames, (canvas, size, frame) =>
{
    canvas.Clear(new SKColor(0xFF101820));

    float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

    float cx = size.Width / 2f;
    float cy = size.Height / 2f;
    float radius = 160f + 60f * MathF.Sin(t * MathF.PI * 2f);

    using (var fill = new SKPaint { IsAntialias = true, Color = new SKColor(0xFFF0B429) })
    {
        canvas.DrawCircle(cx, cy, radius, fill);
    }

    canvas.Save();
    canvas.Translate(cx, cy);
    canvas.RotateDegrees(t * 360f);

    using (var tick = new SKPaint
    {
        IsAntialias = true,
        Color = SKColors.White,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 8
    })
    {
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f * MathF.PI / 180f;
            float x0 = MathF.Cos(angle) * (radius + 40f);
            float y0 = MathF.Sin(angle) * (radius + 40f);
            float x1 = MathF.Cos(angle) * (radius + 90f);
            float y1 = MathF.Sin(angle) * (radius + 90f);
            canvas.DrawLine(x0, y0, x1, y1, tick);
        }
    }

    canvas.Restore();

    using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei");
    using var font = new SKFont(typeface, 48);
    using var textPaint = new SKPaint { IsAntialias = true, Color = SKColors.White };

    canvas.DrawText(
        $"Frame {frame:000} / {Frames - 1:000}",
        cx,
        size.Height - 140f,
        SKTextAlign.Center,
        font,
        textPaint);
});
```

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

### 宽高出现小数偏差

`skanim` 接收浮点 `SKSize`，但内部强制转换为 `int`，小数被截断。需要四舍五入时先传入 `new SKSize(MathF.Round(w), MathF.Round(h))`。

### 并发渲染

`SKAnimation` 不保存绘制状态，可并发调用 `BuildFrame`，前提是回调内部不访问共享可变状态。`SaveMp4` / `SaveGif` 内部持有编码器状态，不要并发调用。
