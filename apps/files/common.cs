public class BS
{
    public static void Foo()
    {
        Console.WriteLine("xxx");
    }

    public static NBLayoutableClip Clip()
    {
        var clip1 = clip(
            name: "clip1",
            frames: 30,
            builder: (ctx, clip) =>
            {
                var easing = Easing.SineInOut;
                double v = easing(ctx.progress) * 2 * Math.PI;
                var r = 900;
                var x = r * Math.Sin(v);
                var y = r * Math.Cos(v);
                return
                HGrid("*", [
                    TextBlock("Clip1")
                        .Align(0,0)
                        .Margin(x, y, 0,0).FontSize(200)
                    ]);
            }
        );
        return clip1;
    }
}