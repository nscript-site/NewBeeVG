using System.Runtime.CompilerServices;
using System.Xml.Linq;

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

        var clip4 = clip(
                name: "filter",
                frames: 30,
                builder: (ctx, clip) =>
                {
                    var easing = Easing.SineInOut;
                    float v = (float)easing(ctx.progress);
                    
                    // 滤镜链1：阴影
                    var shadowFilter = SKImageFilter.CreateDropShadow(4, 4, 3, 3, SKColors.Black);
                    // 滤镜链2：轻微整体模糊
                    var blurFilter = SKImageFilter.CreateBlur(3.8f*v, 3.8f*v);

                    float[] grayMat = {
                        0.299f,0.587f,0.114f,0,0,
                        0.299f,0.587f,0.114f,0,0,
                        0.299f,0.587f,0.114f,0,0,
                        0,0,0,1,0
                    };

                    var filterGray = SKColorFilter.CreateColorMatrix(grayMat);

                    return
                    VGrid("*,*",[
                            Panel([
                                TextBlock("filter").Align(0,0).Margin(20).Filter()
                                ]).Margin(200)
                                .Filter(shadowFilter, blurFilter)
                                .Background(SKColors.Red),
                            Image("./Assets/snows.jpg").Margin(200)
                            .ColorFilter(filterGray)
                        ]).Background(SKColors.DeepSkyBlue);
                }
            );

        var clip5 = clip(
                name: "shader",
                frames: 30,
                builder: (ctx, clip) =>
                {
                    var shader = (SKRect rect) => SKShader.CreateLinearGradient(rect.LeftMiddle, rect.RightMiddle,
                        new SKColor[] { SKColors.Red, SKColors.Green, SKColors.Blue },
                        new float[] { 0, 0.5f, 1 },
                        SKShaderTileMode.Clamp);

                    return
                    VGrid("*", [
                            Rect(400,600).Align(0,0)
                            .Shader(shader)
                        ]).Background(SKColors.DeepSkyBlue);
                }
            );

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
                 var easing = Easing.SineInOut;
                 double v = easing(ctx.progress);

                 var alphaShader = (SKRect rect) =>
                 {
                     var pair = (rect.LeftMiddle, rect.RightMiddle);
                     return SKShader.CreateAlphaLinearGradient(pair.Interpolate(0 + v), pair.Interpolate(1 + v), [0, 1], [0, 1]);
                 };

                 return
                    Panel([Rect(800,1200,cornerRadius:20).Align(0,0)
                    .Shader(alphaShader)
                ]);
             }
         );

        run(stage(bg: SKColors.Orange), [clip1,clip2,clip3,clip4,clip5,clip6]);
    }
}
