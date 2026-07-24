using System.Runtime.CompilerServices;

namespace NewBeeVG.Demo.Samples;

internal class TransformSample
{
    public static void Run([CallerFilePath] string filePath = "")
    {
        NBDrawingClip GetClip(string name, int transformType)
        {
            return clip(
                name: name,
                frames: 30,
                builder: (ctx, clip) =>
                {
                    var easing = Easing.SineInOut;
                    double v = easing(ctx.progress);
                    var m = SKMatrix.CreateScale(1 + (float)v, 1 + (float)v);
                    if (transformType == 1)
                        m = SKMatrix.CreateTranslation(0, (float)v * 100);
                    else if (transformType == 2)
                        m = SKMatrix.CreateRotation((float)v);
                    return
                    Panel([
                            Panel([
                                TextBlock(name).Align(0,0).Margin(20).Opacity(0.5+0.5*v)
                                    .RenderTransform(m)
                                ]).Margin(200).RenderTransform(m)
                            .Background(SKColors.Red)
                        ]).Background(SKColors.DeepSkyBlue);
                }
            );
        }

        var clip1 = GetClip("scale", 0);
        var clip2 = GetClip("translate", 1);
        var clip3 = GetClip("rotation", 2);

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

        var clip6 = clip(
             name: "alpha shader",
             frames: 30,
             builder: (ctx, clip) =>
             {
                 return
                 VGrid($"*", [
                         Image("./Assets/snows.jpg")
                            .Align(0,0).Stretch(Stretch.Fill)
                     ]).Background(SKColors.DeepSkyBlue);
             }
             ,
             mask: (ctx, clip) =>
             {
                 return
                    Panel([Rect(800,1200,cornerRadius:20)
                    .Align(0,0)
                        .OnFrame(e=>{
                            float v = (float)Easing.SineInOut(e.p);
                            e.Sender.Shaders(Shaders.AlphaLinearGradientOnRect([0,1],[0 + v,1 + v]));
                        })
                    ]);
             }
         );

        run(stage(bg: SKColors.Orange), [clip1,clip2,clip3,clip4,clip5,clip6]);
    }
}
