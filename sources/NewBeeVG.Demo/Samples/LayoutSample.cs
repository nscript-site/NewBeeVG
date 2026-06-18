namespace NewBeeVG.Demo.Samples;

internal class LayoutSample
{
    public static void Run()
    {
        var c = new NBBox();
        c.Align(null, null);
        c.Padding(10, 20);
        var c2 = new NBBox();
        c2.Size(20,100);
        c2.Align(0, 0);
        c2.Margin(10);
        c.Child = c2;
        c.MeasureInfinity();
        c.Arrange(0,0,1000, 1000);
        Console.WriteLine(c.Bounds);
        Console.WriteLine(c2.Bounds);
    }
}
