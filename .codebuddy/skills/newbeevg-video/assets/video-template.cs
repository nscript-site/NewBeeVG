#!/usr/bin/env dotnet

// 需要先准备一个 SKCustomAnimation 子类，放在 modules/MyAnimation.cs
// 仓库 apps/files 下由 Directory.Build.props 提供 NewBeeVG 引用；
// 仓库外运行时取消下一行注释并改成当前包版本：
// #:package NewBeeVG@1.0.1.15
#:include modules/MyAnimation.cs

// ------------------------------------------------------------
// 片头
// ------------------------------------------------------------
{
    VStack([
        TextBlock("视频演示").FontSize(200).Foreground(SKColors.White)
    ]).Align(0, 0)
      .AsClip(30, name: "start");
}

// ------------------------------------------------------------
// 内容：在布局中嵌入两个 SkiaAnim
// ------------------------------------------------------------
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

    var anim1 = new MyAnimation(600, 400, frames, content);
    var anim2 = new MyAnimation(600, 400, frames, content2);

    HGrid("*,*", [
        SkiaAnim(anim1),
        SkiaAnim(anim2),
    ]).AsClip(frames, name: "clip1");
}

// ------------------------------------------------------------
// 片尾
// ------------------------------------------------------------
{
    VStack([
        TextBlock("视频结束").FontSize(200).Foreground(SKColors.White)
    ]).Align(0, 0)
      .AsClip(30, name: "end");
}

// ------------------------------------------------------------
// 装饰层：frames = -1 持续到结尾，start = -1
// ------------------------------------------------------------
{
    VGrid("100,*,100", [
        TextBlock("Logo").Align(1, -1).Margin(20, 20, 50, 0),
        null,
        TextBlock("Footer").Align(1, 1).Margin(20, 0, 50, 20)
    ]).AsClip(-1, -1, name: "decoration");
}

// ------------------------------------------------------------
// 运行
//   dotnet video-template.cs             → 预览
//   dotnet video-template.cs out.mp4     → 导出 MP4
//   dotnet video-template.cs 42 out.png  → 导出第 42 帧
// ------------------------------------------------------------
run(stage(1920, 1080, bg: SKColors.Orange), clips(), args: args);
