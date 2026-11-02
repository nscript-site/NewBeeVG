#!/usr/bin/env dotnet

#:include ../GlintTextA.cs

bool debug = true;
if (args.Length > 0) debug = args[0] == "debug";   // 不带参数 = 出测试图；传 "debug" 以外的值 = 正式出片

int fps = 25;
var theme = GlintTextA.ContentTheme.NightCopper();

GlintTextA.ContentSetting Base() => new()
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

SkiaAnim(new GlintTextA(1920,1080, fps * 5, Base(), theme))
.AsClip(fps * 5, name:$"GlintTextA" );

// ============================================================
//  ★ 样式一览：同一句话、同一帧，只换 TextStyle
// ============================================================
var styles = new (string Name, GlintTextA.TextStyle Style)[]
{
    ("纯文字",  GlintTextA.TextStyle.Plain),
    ("按钮",    GlintTextA.TextStyle.Button),
    ("下划线",  GlintTextA.TextStyle.Underline),
    ("高亮底",  GlintTextA.TextStyle.Mark),
    ("左竖条",  GlintTextA.TextStyle.Bar),
    ("方括号",  GlintTextA.TextStyle.Bracket),
};
foreach (var (name, st) in styles)
{
    var s = Base();
    s.Style = st;
    s.Glint = 0f;
    SkiaAnim(new GlintTextA(1920,1080, fps * 5, s, theme))
    .AsClip(fps * 5, name:$"样式_{name}" );
}

// ============================================================
//  ★ 掠金过程：光带从左走到右，扫完不留痕
// ============================================================
{
    var s = Base();
    s.Style = GlintTextA.TextStyle.Plain;
    SkiaAnim(new GlintTextA(1920,1080, fps * 5, s, theme))
    .AsClip(fps * 5, name:$"掠金" );
}

// 掠金强度：同一帧三档
foreach (var (nm, gl) in new (string, float)[] { ("关", 0f), ("中", 0.5f), ("满", 1.0f) })
{
    var s = Base();
    s.Glint = gl;
    SkiaAnim(new GlintTextA(1920,1080, fps * 5, s, theme))
    .AsClip(fps * 5, name:$"掠金_{nm}" );
}

// ============================================================
//  ★ 亮底：字比底色暗，掠金改成直接给字镀亮金
// ============================================================
{
    var s = Base();
    s.Text = "把城墙卖了";
    s.Style = GlintTextA.TextStyle.Underline;
    SkiaAnim(new GlintTextA(1920,1080, fps * 5, s, theme))
    .AsClip(fps * 5, name:$"下划线掠金" );
}


// 主题 Background 透明：不清屏，导出带 alpha 的 PNG（叠加到别的画面上用）
{
    var bgContent = new GlintTextA.ContentSetting { Text = "四百年前的一张纸" };
    var bgTheme = GlintTextA.ContentTheme.NightCopper();
    bgTheme.Background = SKColors.Transparent;
    using var bgAnim = new GlintTextA(1920, 1080, 90, bgContent, bgTheme);

    if (debug == true) bgAnim.SavePNG(89, "output/glinttexta/透明底_089.png");

    SkiaAnim(new GlintTextA(1920, 1080, 90, bgContent, bgTheme))
        .AsClip(90, name: "透明底");
}

if (debug == false) run(stage(1920,1080), clips());