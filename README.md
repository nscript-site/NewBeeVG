<p align="center">
  <img src="./assets/logo200.jpg" alt="logo" />
</p>

NewBeeVG(NewBee Visual Content Generator) 是基于 [NewBeeUI](https://github.com/nscript-site/NewBeeUI) 的程序化动画/视频内容生成工具，通过 AI 编程，可以快速的制作视频内容。

在设计上，受到 [remotion](https://github.com/remotion-dev/remotion)(Make videos programmatically with React) 的启发，但要比 remotion 更间接易用。基于 nodejs 的东东，谁用谁知道 .....，吐槽 ......。我用 Trae + remotion/skills 跑了两小时，各种七七八八的 bug，痛不欲生 ...... 一怒之下，开发了这个。

NewBeeVG 集成了：
- [Avalonia](https://avaloniaui.net/) 的 UI 布局系统, 完整复用 Avalonia 成熟布局计算逻辑，统一规范界面与画布元素排版能力
  - 基础布局容器：内置 Grid、StackPanel、Canvas、WrapPanel 等标准布局容器；
  - 元素间距控制：统一支持 Padding（内边距）、Margin（外边距）属性；
  - 对齐与拉伸：提供 HorizontalAlignment / VerticalAlignment 对齐、Stretch 尺寸拉伸策略；
  - 尺寸约束：支持 Width/Height、Min/Max 尺寸限制、比例自适应、内容自适应布局；
  - 跨平台兼容：布局逻辑完全跨平台统一，Windows/macOS/Linux 渲染排版无差异；
- [SkiaSharp](https://github.com/mono/SkiaSharp) 底层 2D 矢量图形渲染内核, 基于 Google Skia 2D 图形库作为底层画布绘制引擎，承担全框架 2D 绘制能力
  - 矢量绘图：路径、矩形、圆形、圆角、贝塞尔曲线、多边形、渐变填充、描边、蒙版裁剪；
  - 文本渲染：多行文本、字体缓存、字重 / 字号 / 字间距调整、文字渐变；
  - 图像处理：PNG/JPG/WebP 解码、纹理缓存、图像缩放、滤镜、透明度混合；
- [Typst](https://typst.app/) 专业结构化排版引擎, 内置 Typst 编译内核，支持在画布内嵌入专业学术 / 文档排版内容，实现图文混排
  - 语法支持：数学公式、图表、化学方程式等；
  - 实时渲染：将 Typst 源码编译为矢量图形，直接嵌入布局容器，和 UI 元素自由混排；
  - 批量导出：支持无头模式批量编译文档、生成论文插图、公式矢量图；
  - 场景适配：学术性动画。
- [HelixToolkit.Nex](https://github.com/helix-toolkit/helix-toolkit-nex) 3D 引擎，提供完整 3D 实时 / 离线渲染、动画能力
  - 完整 3D 管线：Vulkan 硬件光栅渲染、PBR 材质、光照系统、阴影、后处理；
  - 模型支持：模型文件加载、材质资源、相机控制；

NewBeeVG 还可通过 [PythonNet](https://github.com/pythonnet/pythonnet) 集成 python 生态。

# 编译运行

需要下载 [NewBeeVG](https://github.com/nscript-site/NewBeeVG) 的源码和 [NewBeeUI](https://github.com/nscript-site/NewBeeUI) 的源码，放在相同的目录下，运行 ·NewBeeVG.slnx· 文件即可打开项目。

由于 [NewBeeVG](https://github.com/nscript-site/NewBeeVG) 引用了 [HelixToolkit.Nex](https://github.com/helix-toolkit/helix-toolkit-nex) 项目，下载时需要加上 --recurse-submodules 参数，否则会导致编译失败。下载示例: 

```bash
git clone --recurse-submodules https://github.com/nscript-site/NewBeeVG.git
```

# 简单示例

[app/files](https://github.com/nscript-site/NewBeeVG/tree/main/apps/files) 下是基于文件的应用示例，推荐使用基于文件的应用来使用 NewBeeVG。

[NewBeeVG.Demo](https://github.com/nscript-site/NewBeeVG/tree/main/sources/NewBeeVG.Demo) 是传统项目的使用示例。


下面是一个简单的单文件应用示例，源代码为 [animate.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/animate.cs)

```csharp
#!/usr/bin/env dotnet

font("阿里巴巴普惠体 2.0");

VStack([
    TextBlock("输入你的文字").Font(120, SKColors.Black).Align(0,0).Id("Text"),
    TextBlock("输入你的文字").Font(120, SKColors.Black)
        .OnFrame(e=> { e.Sender.Opacity(e.p);  e.SenderLayoutable?.Margin(0,e.p * 200,0,0); })
])
.Align(0, 0)
.AsClip(out var clip1, frames: 40, name: "animate");

VStack([
    TextBlock("Code").Font(80, SKColors.Orange).Align(0,0),
    TypstFile("./typst/code1.typ").MaxHeight(800).Align(0,0),
    TextBlock("生成视频的全部代码").Font(40, SKColors.Black).Align(0,-1),
]).Align(0,0).AsClip(out var clip2, frames: 120, name: "code");

TextBlock("NewBee VG").FontSize(40).Margin(20).Align(1, -1)
.AsClip(out var logo, frames: -1, start: 0, name: "logo");

run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, logo]);
```

上面代码创建了 3 个 clip。

- animate: 40 帧的文字动画，OnFrame 事件驱动每帧画面变化
- code: 120 帧的嵌入 tyspt 文件渲染画面，高亮显示源代码。成功渲染需要本地安装 [typst](https://typst.app/)，能正确编译/预览对应的 typ 文件
- logo: logo 层。frames: -1, start: 0 表示从第 0 帧开始，一直持续到视频结束

下面代码表示，运行 [clip1, clip2, logo] 组成的 Track 项目，视频尺寸为 (1920,1080)，背景色为白色:

```csharp
run(stage(1920, 1080, bg: SKColors.White), [clip1, clip2, logo]);
```

由于 clip1 和 clip2 没有设置 start，系统默认为 clip1 为视频的第一个片段，clip2 紧跟  clip1。系统会自动判断视频的长度为 clip1 的长度 + clip2 的长度。而 logo 设置了 start，frames 为 -1 (代表视频结束)，系统也会自动计算 logo 的长度为视频总长度。

进入 `app/files` 目录，执行 `dotnet animate.cs` 命令即可执行上述文件:

效果如下：

 <img src="./assets/record-animate.gif" alt="logo" />

点击 `track`，可以看到整个 track 的动画。点击每个 clip，可以看到单独 clip 的动画。右上角的两个按钮可以将内容导出为 mp4 视频和 gif 动画。导出 mp4 视频时不会进行缩放。如果画面尺寸大于 1000，导出 gif 时会缩放到 1000。

下面代码可以直接导出视频文件：

```csharp
save("youtpath.mp4", stage(1920, 1080, bg: SKColors.White), [clip1, clip2, logo]);
```

## 排版布局

撰写中 ...

## 实现动画

### Markup 实现动画

可以通过 Markup 方式创建 NBVisual，然后监听 NBVisual 的 OnFrameUpdated 事件，可以在每一帧渲染前动态更改显示的内容及属性，生成动画。

事件参数如下:

```csharp
public class NBFrameUpdateEvent
{
    public NBVisual Sender { get; set; } = default!;

    public NBDrawContext Ctx { get; set; } = default!;

    public NBFrameUpdateEvent(NBDrawContext Ctx)
    {
        this.Ctx = Ctx;
    }

    public NBLayoutable? SenderLayoutable => Sender as NBLayoutable;

    public T? SenderAs<T>() where T : NBVisual
    {
        return Sender as T;
    }

    /// <summary>
    /// 在 clip 中的进度，double 值
    /// </summary>
    public double progress => Ctx.progress;

    /// <summary>
    /// 在 clip 中的进度，double 值
    /// </summary>
    public double p => Ctx.progress;

    /// <summary>
    /// 在 clip 中的帧编号
    /// </summary>
    public int frame => Ctx.frame;

    /// <summary>
    /// 在 clip 中的进度，float 值
    /// </summary>
    public float pf => (float)Ctx.progress;
}

```

可以通过 `NBVisual` 的 `OnFrame` 扩展方法，方便的响应 OnFrameUpdated 事件。例子：

```csharp
TextBlock("输入你的文字").Font(120, SKColors.Black)
    .OnFrame(e=> { e.Sender.Opacity(e.p);  e.SenderLayoutable?.Margin(0,e.p * 200,0,0); })
```

### 通过函数实现动画

如果动画逻辑比较复杂，通过 Markup 监听自身的 OnFrame 较难实现。您可以通过函数方式的，动态创建每一帧的内容，来实现动画。

可以通过 Markup 方式创建，示例如下:

```csharp
var clip2 = clip(
    name: "clip2",
    frames: 30,
    builder: (ctx, clip) =>
    {
        var easing = Easing.SineInOut;
        double v = easing(ctx.progress);

        return
        Panel([
                TextBlock("Clip2").FontSize(200)
                .Align(0,-1)
                .Margin(0, 100 + (ctx.height - 500) * v, 0,0),

                HStack([
                    TextBlock("AAAA"),                            
                    TextBlock("BBBB")
                    ])
                .Background(SKColors.Red)
                .Align(0,1)
                .Margin(100)
            ]);
    }
);
```

也可以直接调用 skia 的 API 来创建，示例如下：

```csharp
var clip3 = drawing(
    name: "drawing",
    frames: 10,
    builder: (ctx, clip, canvas) =>
    {
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Red,
            IsAntialias = true,
        };
        var radius = 100 + 200 * ctx.progress;
        canvas.DrawCircle(ctx.width / 2, ctx.height / 2, (float)radius, paint);
    }
);
```

> [!NOTE]
> 优先使用 Markup 方式来实现动画。

### 集成 lottie 动画

可以直接集成 lottie 动画文件（目前仅支持 json 格式的 lottie 动画），示例如下([lottie.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/lottie.cs)): 

```csharp
#!/usr/bin/env dotnet

HStack([
    LottieFile("./assets/fire.json").Size(100,100).Align(0, 1),
    LottieFile("./assets/fire.json").Size(200,200).Speed(2).Align(0, 1),
])
.Align(0, 0)
.AsClip(out var clip1, frames: 100, name: "animate");

run(stage(1920, 1080, bg: SKColors.Orange), [clip1]);
```

设置 `Speed` 可以控制 lotties 的播放速度。

还可以直接引用压缩包里的 lotties 文件:

```csharp
LottieFile("./lotties.zip","file.json")
```

LottieFile(lottieBase,"00bcfb5847863498e4430cc64831ca56.json").Size(100,400).Align(0,0)

> [!NOTE]
> 使用我们开发的 [NBView](https://zenapp.work/nbview/) ([微软商店下载 NBView](https://apps.microsoft.com/detail/9pj0gw4jfwpw))或 [NBView Pro](https://zenapp.work/nbviewpro/) ([微软商店下载 NBView Pro](https://apps.microsoft.com/detail/9nv1nq63t3sg)) 可以很方便的浏览 lottie 动画素材，能够免压缩直接浏览压缩包里的 lottie 文件。NBView Pro 支持百万级 lotties 动画文件的语义检索!

## Filter、Mask 与 Shader

撰写中 ...

## 嵌入 typst 动画

撰写中 ...

## 嵌入 3D 动画

嵌入 3D 动画参考 [3d.md](https://github.com/nscript-site/NewBeeVG/blob/main/docs/3d.md)

## 嵌入 python 生态

嵌入 python 生态参考 [py-embed.md](https://github.com/nscript-site/NewBeeVG/blob/main/docs/py-embed.md)

## TODO

- [x] File Based App 示例
- [x] 导出视频
- [x] 集成 3D 模块 
- [ ] 完善排版布局文档
- [x] 完善实现动画文档
- [ ] 完善 Filter 与特效文档
- [ ] 完善嵌入 typst 动画文档
- [ ] 完善嵌入 3D 动画文档
- [x] 完善嵌入 python 生态文档