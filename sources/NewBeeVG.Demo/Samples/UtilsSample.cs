using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class UtilsSample
{
    public static void Run()
    {
        // 输出:
        // Start...
        // End...
        using var _ = defer(() => Console.WriteLine("End ..."));
        Console.WriteLine("Start ...");
    }
}
