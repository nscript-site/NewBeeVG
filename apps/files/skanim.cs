#!/usr/bin/env dotnet

#:include modules/MyAnimation.cs

// title clip
{
    VStack([
        TextBlock("视频演示").FontSize(200).Foreground(SKColors.White)
    ]).Align(0,0)
    .AsClip(30, name: "start");
}

// clip 1, 演示在画面中嵌入两个 SkiaAnim
{
    var content = new MyAnimation.ContentSetting
    {
        From = 0,
        To = 12580,
        Format = "N0",
        FontSize = 160f
    };
    var content2 = new MyAnimation.ContentSetting
    {
        From = 0,
        To = 12580,
        Format = "N0",
        FontSize = 160f
    };
    int frames = 90;
    var anim = new MyAnimation(600, 400, frames, content);
    var anim2 = new MyAnimation(600, 400, frames, content);

    HGrid("*,*", [
        SkiaAnim(anim),
        SkiaAnim(anim2),
    ]).AsClip(frames, name: "clip1");
}

// 第三个 clip
{
     VStack([
        TextBlock("视频结束").FontSize(200).Foreground(SKColors.White)
    ]).Align(0,0)
    .AsClip(30, name: "end");
}

// 装饰 clip，AsClip 时第一个参数设置为 -1，
{
    VGrid("100,*,100", [
        TextBlock("Logo").Align(1,-1).Margin(20,20,50,0),
        null,
        TextBlock("Footer").Align(1,1).Margin(20,0,50,20)
    ]).AsClip(-1, -1, name: "decoration");
}

// clips() 会收集前面创建的所有 clips
run(stage(1920, 1080, bg: SKColors.Orange), clips(), args:args);