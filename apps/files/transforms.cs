#!/usr/bin/env dotnet

NBVisual Build(string name, Action<NBVisual> opacity, Action<NBVisual> transform)
{
    Panel([
        TextBlock(name).Align(0,0).Margin(20).Styles(opacity, transform)
    ]).Margin(200)
    .Background(SKColors.Red).Id("panel").Styles(transform).Ref(out var content);
    return content;
}

var s_opacity = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.Opacity(0.5 + 0.5 * v);
    });
};

var s_scale = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.RenderTransform(SKMatrix.CreateScale(1 + v, 1 + v));
    });
};

var s_translate = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.RenderTransform(SKMatrix.CreateTranslation(0, v * 100));
    });
};

var s_rotation = (NBVisual v) =>
{
    v.OnFrame(e =>
    {
        float v = (float)Easing.SineInOut(e.p);
        e.Sender.RenderTransform(SKMatrix.CreateRotation(v));
    });
};

Build("scale", s_opacity, s_scale).AsClip(out var clip1, 30, name: "scale");
Build("translate", s_opacity, s_translate).AsClip(out var clip2, 30, name: "translate");
Build("rotation", s_opacity, s_rotation).AsClip(out var clip3, 30, name: "rotation");

run(stage(bg: SKColors.Orange), [clip1,clip2,clip3]);