#!/usr/bin/env dotnet

var style1 = (NBVisual v) => {
    v.As<NBText>()?
        .FontSize(80).Foreground(SKColors.Black).Align(0, 0)
        .LetterSpacing(10 + 24).Padding(100);
};

var style2 = (NBVisual v) => {
    v.As<NBText>()?
        .FontSize(80).AddStroke(SKColors.Orange, 12).AddStroke(SKColors.Red, 10)
        .Foreground(SKColors.Black).Align(0, 0)
        .LetterSpacing(10).Padding(100);
};

Panel([
    TextBlock("求关注").Styles(style1),
    TextBlock("求关注").Styles(style2).FrameMask(FrameMasks.RectExpandMask())
])
.AsClip(out var clip1, 30, name: "qiuguanzhu");

run(stage(1920, 1080, bg: SKColors.White), [clip1]);