# 使用 style

可以通过 `Styles` 扩展方法，在多个组件中共享设置。该方法原型如下:

```csharp
public static T Styles<T>(this T t, params Action<NBVisual>?[] styles) where T : NBVisual
```

下面是 style 使用示例[style.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/style.cs): 

```csharp
#!/usr/bin/env dotnet

var style1 = (NBVisual v) => {
    v.As<NBText>()?
        .AddStroke(SKColors.Orange, 55).AddStroke(SKColors.White, 50).AddStroke(SKColors.Red, 40)
        .FontSize(120).Foreground(SKColors.Black).Align(0, 0);
};

var style2 = (NBVisual v) => {
    v.As<NBText>()?
        .OnFrame(e =>
        {
            var txt = e.SenderAs<NBText>()?.Text ?? "";
            var idx = "求关注".IndexOf(txt);
            if (idx < 0) idx = 0;
            var frame = e.frame % 10;
            var animateIdx = e.frame % 30 / 10;
            if (animateIdx == idx)
            {
                float p = frame / 10.0f;
                p = (float)Easing.BackIn(p);
                e.Sender.RenderTransform(SKMatrix.CreateScale(1 + 0.2f * p, 1 + 0.2f * p));
            }
            else
            {
                e.Sender.RenderTransform();
            }
        });
};

HStack([
    TextBlock("求").Styles(style1, style2),
    TextBlock("关").Styles(style1, style2),
    TextBlock("注").Styles(style1, style2),
]).Align(0,0).Spacing(20)
.AsClip(out var clip, 30, name: "clip");

run(stage(1920, 1080, bg: SKColors.White), [clip]);
```