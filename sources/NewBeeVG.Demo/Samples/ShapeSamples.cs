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
                HGrid("*,*,*,*,*",
                [
                    Rect(v*100,200,SKColors.Green,20).Align(0,0),
                    Rect(v*100,0,SKColors.Green).Align(0,null),
                    Ellipse(v*100,v*200,SKColors.Green).Align(0,0),
                    // 创建矢量路径
                    VecPath(RectPath(0,0,v*200,v*200),SKColors.Green, new NBBorder(2,SKColors.Red)).Align(0,0),                    
                    // 创建两个矢量路径之间的补间动画
                    VecPath(RectPath(0,0,200,200),CirclePath(100,100,100), v, SKColors.Green).Align(0,0)
                ]);
           }
        );

        run(stage(bg: SKColors.Orange), [clip1]);
    }
}
