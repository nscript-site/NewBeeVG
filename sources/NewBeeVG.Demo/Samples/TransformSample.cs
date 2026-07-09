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

        run(stage(bg: SKColors.Orange), [clip1,clip2,clip3]);
    }
}
