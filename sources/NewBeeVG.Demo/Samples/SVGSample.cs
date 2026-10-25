using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class SVGSample
{
    public static void Run([CallerFilePath] string filePath = "")
    {
        var svg = $"""
                    <svg xmlns="http://www.w3.org/2000/svg" width="300" height="200" viewBox="0 0 300 200">            
                        <rect width="100" height="100" fill="#f5f5f5"/>
                        <text x="140" y="108" font-size="24" fill="#222">Hello SVG</text>
                    </svg>
                    """;

        VGrid($"*", [
                SVG(svg).Align(0,0)
        ])
        .Background(SKColors.DeepSkyBlue)
        .AsClip(out var clip4, 30);

        run(stage(bg: SKColors.Orange), [clip4]);
        //save("out.mp4", stage(bg: SKColors.Orange), [clip1, clip2, clip3, clip4, clip5, clip6]);
    }
}
