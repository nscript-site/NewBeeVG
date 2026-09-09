# 实现动画

NewBeeVG 支持三种方式实现动画:

- 通过 Markup 来实现动画
- 通过函数实现动画
- 集成 lottie 动画

## 通过 Markup 实现动画

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

## 通过函数实现动画

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

## 集成 lottie 动画

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
