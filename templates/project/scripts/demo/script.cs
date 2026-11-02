#!/usr/bin/env dotnet

// 引用其他文件
#:include modules/GlintTextA.cs

int fps = 25;
var theme = GlintTextA.ContentTheme.NightCopper();

// 只用已有组件制作视频片段
{
    VStack([
        TextBlock("Hello, NewVeeVG Project").Font(80, SKColors.Orange).Ref(out var tb1)
        .Margin(100,0,0,100).Align(-1,0)
        .OnFrame(e=>tb1.Margin(100 + e.frame * 20,0,0,100))
    ]).Align(-1, 0).AsClip(frames: 40, name: "by_widgets");    
}

// 使用 modules 里的特效模板制作视频片段
{
    var content = new GlintTextA.ContentSetting()
    {
        Text = "世界第一只股票",
        Style = GlintTextA.TextStyle.Plain,
        Glow = 0.55f,
        Grain = 0.24f,
        Dust = 26,
        Vignette = 0.48f,
        Glint = 0.90f,
        AnimRatio = 0.50f,
    };

    content.Style = GlintTextA.TextStyle.Plain;
    SkiaAnim(new GlintTextA(1920,1080, fps * 5, content, theme))
    .AsClip(fps * 5, name:$"by_module" );   
}

// 符合使用两者制作视频片段
{
    var content = new GlintTextA.ContentSetting()
    {
        Text = "世界第一只股票",
        Style = GlintTextA.TextStyle.Plain,
        Glow = 0.55f,
        Grain = 0.24f,
        Dust = 26,
        Vignette = 0.48f,
        Glint = 0.90f,
        AnimRatio = 0.50f,
    };

    content.Style = GlintTextA.TextStyle.Plain;

    HGrid("*,*", [
        TextBlock("Mixed").Font(80, SKColors.Orange).Ref(out var tb1)
            .Margin(100,0,0,100).Align(-1,0)
            .OnFrame(e=>tb1.Opacity(e.pf)),
        SkiaAnim(new GlintTextA(600,400, fps * 5, content, theme)).Ref(out var sk)
            .OnFrame(e=>sk.Opacity(e.pf))
            .Align(0,0)
    ])
    .AsClip(fps * 5, name:$"mix" );   
}

// 音频链接到 name 为 by_widgets 的 第0帧（初始帧）
// AudioFile("./assets/audio01.mp3")
//     .AsClip("audio").LinkTo("by_widgets",0);

/**************************************************************************************************
 dotnet <file.cs>						// 预览视频
 dotnet <file.cs> -- "@dump"			// 将视频的元数据及clip信息输出在控制台
 dotnet <file.cs> -- demo.mp4			// 视频保存为 mp4
 dotnet <file.cs> -- 3 3.png			// 把视频中第 3 帧保存为 png
 dotnet <file.cs> -- 3 @dump			// 把视频中第 3 帧转码为 png，以base64输出控制台
 dotnet <file.cs> -- mix				// 预览 name=mix 的片段
 dotnet <file.cs> -- mix mix.mp4	    // 把 name=mix 的片段保存为 mp4
 dotnet <file.cs> -- mix 3 "@dump"	    // 把 name=mix 的片段的第3帧转码为 png，以base64输出控制台
**************************************************************************************************/
run(stage(1920, 1080, fps:fps, bg: SKColors.White), args: args);
