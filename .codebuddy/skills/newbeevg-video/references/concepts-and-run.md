# 核心概念、clip 时间线与 run

## 1. 结构

```text
NBWorkspace
└── NBWork（一个视频项目）
    ├── NBStage（画幅 / 帧率 / 背景 / 音频采样率）
    └── NBTrack（轨道）
        └── NBClip / NBDrawingClip（片段）
```

- `NBWorkspace.Current`：当前工作区，`run` 会创建并赋值。
- `NBWork`：持有 `Stage` 与 `Tracks`，是导出时的工作单元。
- `NBTrack`：按 `StartFrame` 顺序渲染 clip；`IsVisible` 可整体隐藏。
- `NBClip`：最核心概念，持有 `StartFrame`、`DurationFrames`、`Name` 与绘制逻辑。
- `NBDrawingClip`：由布局组件（`NBVisual`）经 `AsClip` 产生的 clip。

`run` 默认把传入的所有 clip 放进同一条 track；`clipLists` 里的片段会 Prepare 后合并到这条 track。

## 2. clip 时间规则

```csharp
public int? StartFrame { get; internal set; }   // null = 自动接在前一个 clip 后
public int DurationFrames { get; set; }         // <= 0 = 持续到最后一帧
```

| 参数 | 取值 | 含义 |
| --- | --- | --- |
| `frames` | `> 0` | 持续帧数 |
| `frames` | `<= 0`（常用 `-1`） | 持续到最后一帧 |
| `start` | 省略 / `null` | 自动紧接前一个 clip |
| `start` | `0` / 正整数 | 从指定帧开始 |
| `start` | `-1` | 显式起点，配合 `frames = -1` 常用于装饰层 |

装饰层典型写法：

```csharp
VGrid("100,*,100", [
    TextBlock("Logo").Align(1, -1).Margin(20, 20, 50, 0),
    null,
    TextBlock("Footer").Align(1, 1).Margin(20, 0, 50, 20)
]).AsClip(-1, -1, name: "decoration");
```

## 3. `AsClip`

`NBVisual` 的扩展方法（`NBVisual.cs`）：

```csharp
public static NBDrawingClip AsClip<T>(this T widget, out NBDrawingClip value,
    int frames, int? start = null, string name = "clip") where T : NBVisual;

public static NBDrawingClip AsClip<T>(this T widget,
    int frames, int? start = null, string name = "clip") where T : NBVisual;
```

`NBClip` 本身也可直接登记：

```csharp
public static NBClip AsClip<T>(this T widget, out NBClip value, string name = "clip") where T : NBClip;
public static NBClip AsClip<T>(this T widget, string name = "clip") where T : NBClip;
```

用法：

```csharp
// 不需要引用
HStack([...]).AsClip(60, name: "intro");

// 需要引用
HStack([...]).AsClip(out var intro, 60, name: "intro");

// 从第 30 帧开始，持续 90 帧
HStack([...]).AsClip(90, 30, name: "body");

// 全程装饰
TextBlock("Logo").AsClip(-1, -1, name: "logo");
```

- `AsClip` 会自动把 clip 加入 `clips()`；
- `frames` 是第二个位置参数，`start` 是第三个；`name` 建议用命名参数；
- `out var` 返回的 clip 可用于后续 `Bind`、`ReferenceClip`、`IsVisible` 等。

## 4. `clips()` 与手动 clip

```csharp
var c1 = HStack([...]).AsClip(60, name: "a");
var c2 = HStack([...]).AsClip(60, name: "b");

run(stage(1920, 1080), clips());            // 收集所有 AsClip 过的片段
run(stage(1920, 1080), [c1, c2]);           // 也可以显式传列表
```

`clips()` 返回的是内部收集列表；不需要手动 `new List`。

其他创建方式：

```csharp
clip(builder, name: "custom", frames: 60);
drawing((ctx, clip, canvas) => { ... }, name: "raw", frames: 60);
```

## 5. `run`：预览与导出

最新签名（以 `Methods.cs` 源码注释为准）：

```csharp
public static int run(
    NBStage stage,
    IList<NBClip> clips,
    IList<NBClip>[]? clipLists = null,
    string[]? args = null)
```

### 5.1 `args` 的三种模式

| `args` | 行为 | 返回值 |
| --- | --- | --- |
| `null` / 空数组 | `start()` 启动 Avalonia 预览窗口，阻塞到窗口关闭 | `-1` |
| 长度 1：`[mp4Path]` | 将所有片段导出为 MP4 | 总帧数 |
| 长度 ≥ 2：`[frame, pngPath]` | 只渲染指定帧并保存 PNG | 总帧数 |

- 帧号不是整数：打印错误并返回 `-1`；
- 帧号越界：只打印提示，仍会尝试渲染；
- 没有可导出内容：返回 `0`；
- `args` 第 3 个及之后的元素忽略；
- 预览模式会启动桌面 UI，必须在主线程调用。

### 5.2 命令行用法

```csharp
run(stage(1920, 1080, bg: SKColors.Black), clips(), args: args);
```

```bash
dotnet myvideo.cs                 # 预览
dotnet myvideo.cs out.mp4         # 导出 MP4
dotnet myvideo.cs 42 out.png      # 导出第 42 帧
```

### 5.3 MP4 导出细节

- 内部调用 `save(...)`；
- 目标文件已存在时拒绝覆盖，只打印提示；
- 依赖 ffmpeg（会调用 `NBGlobal.CheckOrLoadFFmpeg()`）；
- 目录不会自动创建，需先确保路径存在。

### 5.4 PNG 导出细节

- 使用 `DrawingHelper.Save(bitmap, path)`；
- 目录不存在会自动创建；
- 只渲染 `args[0]` 指定的一帧。

### 5.5 `clipLists` 合并多批片段

```csharp
run(stage(1920, 1080), mainClips, [overlayClips, captionClips], args);
```

每组片段都会先 `Prepare`，再与主列表合并到同一条 track；适合把片段构建拆成多个方法/文件。

## 6. `stage`

```csharp
NBStage stage(int width = 1080, int height = 1920,
              SKColor? bg = null, double fps = 25.0)
```

- `Width` / `Height`：输出像素尺寸；
- `FrameRate`：默认 25；
- `Background`：为 `null` 时预览显示棋盘格，导出时不铺底色（透明）；
- `AudioFrameRate`：默认 44100。

```csharp
var stage = stage(1920, 1080, bg: SKColors.Black, fps: 30);
```

## 7. 3D 能力（可选）

`NewBeeVG.ThreeD` 提供 `Canvas3D` 及一组 3D 节点，可像普通组件一样放进布局并 `AsClip`：

```csharp
Canvas3D(800, 800, SKColors.White)
    .Camera(PerspectiveCamera(Vec3(10, 12, -25)))
    .Nodes([
        GroundGrid(10, 1, SKColors.DimGray, 1),
        Line3D(Vec3(), Vec3(0, 0, len), SKColors.Red, thickness)
            .OnFrameT(e => e.Sender.End = Vec3(0, 0, len * e.pf)),
    ])
    .Align(0, 0);
```

- `PerspectiveCamera(Vec3(x, y, z))`：透视相机；
- `GroundGrid(...)`：地面网格；
- `Line3D(from, to, color, thickness)`：线；
- `OnFrameT`：带缓动的逐帧回调，`e.pf` 为进度；
- 需要旋转/缩放相机时：

```csharp
Canvas3D(800, 800, SKColors.White)
    .Ref(out var canvas3d)
    .OnFrame(e =>
    {
        canvas3d.RotateCamera(2f, 0);
        canvas3d.ZoomCamera(-0.1f);
    })
    .Camera(PerspectiveCamera(Vec3(-10, -12, 25)))
    .Nodes([...]);
```

3D 动画同样遵循 clip 时间线，可与 2D 布局、文字、特效混排。

## 8. 片段准备与失败处理

`run` 会遍历 clip 调用 `Prepare(stage)`；单个 clip 失败只打印：

```text
Clip[名称 prepare fail: 原因]
```

不会中断其他片段。导出前若发现画面缺失，先检查对应 clip 的 `Prepare` 是否抛异常。

## 9. 音频片段

`AudioFile(path)` 创建 `NBAudioClip`：只包含音频，不参与画面绘制。

```csharp
public static NBAudioClip AudioFile(string path);
```

音频 clip 用 `AsClip("audio")` 登记名称，再用 `LinkTo` 对齐到目标画面 clip：

```csharp
// 音频链接到 clip1 的第 0 帧
AudioFile("./Assets/audio01.mp3")
    .AsClip("audio").LinkTo("clip1");

// 音频链接到 clip1 的第 10 帧（相对 clip1 的起点）
AudioFile("./Assets/audio01.mp3")
    .AsClip("audio").LinkTo("clip1", 10);
```

`LinkTo` 有两个重载：

```csharp
public static T LinkTo<T>(this T widget, string clipName, int startFrame = 0) where T : NBClip;
public static T LinkTo<T>(this T widget, T referenceClip, int startFrame = 0) where T : NBClip;
```

行为：

- `AsClip("audio")` 使用 `NBClip` 的 `AsClip` 重载：设置 `Name`，并加入 `clips()` 与名称字典；
- `LinkTo("clip1", 10)` 设置 `ReferenceClip = clip1`、`ReferenceClipStartFrame = 10`；
- 音频自身的 `StartFrame` 仍为 `null`，track 计算时间线时解析为 `clip1.StartFrame + 10`；
- 目标 clip 必须**先创建并登记名称**，否则 `Methods.clip("clip1")` 返回 `null`，链接不会生效；
- `Prepare` 时按音频文件真实时长更新 `DurationFrames`：`ceil(秒数 × stage.FrameRate)`，至少 1 帧；
- 音频 PCM 会混入 `NBTrack.MixedAudioData`，MP4 导出时写入音轨；
- 文件不存在时打印 `音频文件不存在`，保留默认 1 帧，不中断其他片段。

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

注意：

- 音频 clip 本身不需要 `frames` / `start`；
- 必须在画面 clip 之后创建，确保目标名称已登记；
- 路径相对于运行时目录；
- 多个音频可分别创建并 `LinkTo` 到不同目标。

