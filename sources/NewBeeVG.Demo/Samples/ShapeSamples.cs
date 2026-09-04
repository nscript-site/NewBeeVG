namespace NewBeeVG.Demo.Samples;

internal class ShapeSamples
{
    public static void Run()
    {
        var clip1 = clip(
           name: "clip1",
           frames: 30,
           builder: (ctx, clip) =>
           {
               var easing = Easing.SineInOut;
               float v = (float)easing(ctx.progress);

               return
                HGrid("*,*,*,*",
                [
                    Rect(v*100,200,SKColors.Green,20).Align(0,0),
                    Rect(v*100,0,SKColors.Green).Align(0,null),
                    Ellipse(v*100,v*200,SKColors.Green).Align(0,0),
                    VecPath(p=>p.AddRect(new SKRect(0,0,v*100,v*200)),SKColors.Green, new NBBorder(2,SKColors.Red)).Align(0,0)
                ]);
           }
        );

        run(stage(bg: SKColors.Orange), [clip1]);
    }
}
