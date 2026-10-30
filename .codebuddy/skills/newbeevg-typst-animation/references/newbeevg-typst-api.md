# NewBeeVG Typst 动画接口

## 1. 唯一入口

```csharp
public static NBTypstAnimation TypstAnim(string path);
```

- `path`：`.typ` 文件路径，相对路径按运行时目录解析；
- 返回 `NBTypstAnimation`，可直接放入布局容器；
- 这一入口只负责“整篇文档逐页编译”，不接收运行时参数。

## 2. 对象模型

```csharp
public class NBTypstAnimation : NBBaseLoopAnimation
{
    public string Workspace { get; protected set; }
    public string? RawTypeContent { get; protected set; }
    public List<SKPicture> TypstFrames { get; protected set; }
}
```

继承自 `NBBaseLoopAnimation`：

```csharp
public int TotalFrames { get; protected set; }
public NBAnimateLoopMode LoopMode { get; internal set; } // 默认 Stop
public double Speed { get; internal set; }               // 默认 1.0
```

| 成员 | 含义 |
| --- | --- |
| `Workspace` | 编译工作区，设为 `.typ` 文件所在目录 |
| `RawTypeContent` | 读取到的原始 Typst 源码 |
| `TypstFrames` | 每页一张 `SKPicture` |
| `TotalFrames` | 成功渲染的页数，也是动画帧数 |
| `LoopMode` | `Stop` 停在最后一页；`Loop` 循环；`AutoReverse` 往复 |
| `Speed` | 播放倍速，不改变页数 |

## 3. 编译生命周期

`TypstAnim(path)` 内部：

1. 读取 `.typ` 全文；
2. 将 `Workspace` 设为 `.typ` 所在目录；
3. 立即调用 Typst 编译器；
4. 遍历 `doc.PageCount`，对每页执行 `RenderPageToSvg(i)`；
5. 用 `SKSvg` 把每页 SVG 转成 `SKPicture`；
6. 加入 `TypstFrames`，设置 `TotalFrames = TypstFrames.Count`。

因此：

- 构造时就会发生编译，首帧有明显延迟；
- 编译失败不会抛出到调用方，错误写入 `DecodeException`；
- 文件不存在时 `DecodeException` 为 `IOException`；
- 节点仍然可以参与布局，但不绘制内容。

## 4. 页面尺寸

```csharp
protected override SKSize? GetImageSize()
```

- 返回第一页 `SKPicture.CullRect.Size`；
- 所有页面都按第一页的尺寸映射到节点矩形；
- **各页尺寸必须一致**，否则后续页会被拉伸或裁切；
- 页面比例决定节点比例，最终由布局缩放。

推荐：

| 画幅 | Typst 页面 |
| --- | --- |
| 16:9 | `960pt × 540pt` |
| 4:3 | `960pt × 720pt` |
| 1:1 | `900pt × 900pt` |
| 竖屏 9:16 | `540pt × 960pt` |

## 5. 播放语义

```csharp
NextFrame(TotalFrames, LoopMode, Speed)
```

- 当前帧号来自 clip 的 `NBDrawContext.Current.frame`；
- 页号 = 帧号；
- `Stop`：帧号超过页数后钳制到最后一页；
- `Loop`：对页数取模；
- `AutoReverse`：正向到末页后反向；
- `Speed`：帧号乘以倍速后再取页。

示例：

```csharp
var anim = TypstAnim("./assets/typst/anim_plot.typ"); // 60 页

// 60 帧正好播完
anim.AsClip(anim.TotalFrames, name: "plot");

// 循环播放
anim.LoopMode(NBAnimateLoopMode.Loop).AsClip(120, name: "plot-loop");

// 慢放一半
anim.Speed(0.5).AsClip(120, name: "plot-slow");
```

`NBTypstAnimation` 是可视节点，不实现 `ISKAnimation`，没有 `SavePNG` / `SaveMp4` / `SaveGif`。预览或导出使用 `run(...)`：

```csharp
run(stage(1920, 1080, bg: SKColors.White), clips(), args: args);
```

## 6. 编译选项

`NBTypstAnimation` 内部使用：

```csharp
new TypstCompilerOptions
{
    WorkspaceRoot = Workspace,
    CustomFontPaths = ["./fonts", "./assets/typography"],
    IncludeSystemFonts = true,
    PackagePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "typst", "packages")
}
```

影响：

- `WorkspaceRoot`：`#include` / `#import` / 图片相对路径的基准；
- `CustomFontPaths`：相对**当前工作目录**，不是 `.typ` 目录；
- `IncludeSystemFonts`：系统字体可用；
- `PackagePath`：`@preview/...` 包缓存位置。

## 7. 性能

| 因素 | 影响 |
| --- | --- |
| 页数 | 每页一张 `SKPicture`，线性增加内存 |
| 页面尺寸 | 像素面积越大，SVG/SKPicture 占用越高 |
| CeTZ 图元 | 路径、文字、渐变越多，编译与绘制越慢 |
| 字体 | 缺失字体触发回退，可能改变排版 |
| 包 | 首次加载 `@preview/...` 需要解析缓存 |

建议：

- 60–90 页、400×240 或 960×540 通常足够；
- 数百页时先降低页面尺寸或减少步骤；
- 只编译一次，播放阶段不再重编译，适合重复预览/导出；
- 页面内容固定，不要在 Typst 中依赖外部网络资源。

## 8. 最小示例

```csharp
#!/usr/bin/env dotnet

var anim = TypstAnim("./assets/typst/steps.typ");

HStack([
    anim.LoopMode(NBAnimateLoopMode.Stop).Align(0, 0),
]).AsClip(anim.TotalFrames, name: "steps");

run(stage(1920, 1080, bg: SKColors.White), clips(), args: args);
```
