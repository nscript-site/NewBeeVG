using System.Runtime.CompilerServices;

namespace NewBeeVG.Demo.Samples;

internal class TransformSample
{
    public static void Run([CallerFilePath] string filePath = "")
    {
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

        VGrid("*,*", [
            Panel([
                    TextBlock("filter").Align(0,0).Margin(20)
                 ])
                .Margin(200)
                .OnFrame(e=>{ e.Sender.Filters(Filters.DropShadow(SKColors.Black, 4, 4, 3, 3), Filters.Blur(3.8f*e.p, 3.8f*e.p)); })
                .Background(SKColors.Red),
            Image("./Assets/snows.jpg").Margin(200)
                .ColorFilters(ColorFilters.Gray())
        ]).Background(SKColors.DeepSkyBlue)
        .AsClip(out var clip4, 30, name: "filters");

        VGrid("*", [
            Rect(400,600).Align(0,0)
                .Shaders(Shaders.LinearGradientOnRect([ SKColors.Red, SKColors.Green, SKColors.Blue],[0, 0.5f, 1]))
        ]).Background(SKColors.DeepSkyBlue)
        .AsClip(out var clip5, 30, name: "shader");

        var style1 = (NBVisual v) => { 
            v.OnFrame(e =>
            {
                float v = (float)Easing.SineInOut(e.p);
                e.Sender.Shaders(Shaders.AlphaLinearGradientOnRect([0, 1], [0 + v, 1 + v]));
            });
        };

        var style2 = (NBVisual v) => {
            v.AsLayoutable()?.Align(0, 0);
        };

        Layer([
            VGrid($"*", 
            [
                Image("./Assets/snows.jpg").Align(0,0).Stretch(Stretch.Fill)
            ]).Background(SKColors.DeepSkyBlue),
            Rect(800,1200,cornerRadius:20).Styles(style1,style2)
        ]).AsClip(out var clip6, 30, name: "alpha shader");

        run(stage(bg: SKColors.Orange), [clip1,clip2,clip3,clip4,clip5,clip6]);
    }
}
