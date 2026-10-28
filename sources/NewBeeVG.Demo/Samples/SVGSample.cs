using HelixToolkit.Nex.Graphics;
using Svg.Skia;
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
        .AsClip(30);

        Panel().Size(200, 200).Align(0, 0).Background(SKColors.Gold).AsClip(30,name:"transparent");

        run(stage(), clips());
    }
}
