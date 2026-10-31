using HelixToolkit.Nex.Graphics;
using NStyles;
using static NewBeeVG.Demo.Samples.MyAnimation;

namespace NewBeeVG.Demo.Samples;

internal class LayoutSample
{
    public static void Run()
    {
        var anim = new MyAnimation(300, 300, 30);
        Panel([
            SkiaAnim(anim)
            .Margin(100,0,100,0)
            .Align(0,0)
        ]).Background(SKColors.Red).Align(0, 0).AsClip(frames: 40, name: "anim");
        run(stage(1920, 1080, bg: SKColors.White));
    }

    public class MyAnimation : SKCustomAnimation
    {
        public MyAnimation(
           int width,
           int height,
           int frames)
           : base(width, height, frames)
        {
        }

        protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
        {
            base.DrawFrame(canvas, size, frame);
            canvas.Clear(SKColors.Blue);
        }
    }
}
