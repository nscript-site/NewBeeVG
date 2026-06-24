using System.Runtime.CompilerServices;

namespace NewBeeVG.Demo.Samples;

internal class PySample
{
    public static void Run([CallerFilePath] string filePath = "")
    {
        Utils.EmbedPython(filePath);

        dynamic m = py_module("./Assets/module_test.py");
        using(py_gil())
        {
            var f = m.foo();
            Console.WriteLine(f);
        }
        dynamic m2 = py_module("./Assets/plot.py");
        using (py_gil())
        {
            var img = m2.plot_3d_data();
            SKBitmap bmp = py_imdecode(img);
            Console.WriteLine($"width: {bmp.Width}, height: {bmp.Height}");
        }
    }
}
