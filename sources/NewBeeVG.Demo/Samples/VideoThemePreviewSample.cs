using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class VideoThemePreviewSample
{
    public static void Run()
    {
        var themes = NBVideoThemes.AllThemes();
        List<NBClip> clips = new();
        foreach(var item in themes)
        {
            var bmp = item.Preview();
            Image(bmp).AsClip(out var clip, 10, name: item.Name);
            clips.Add(clip);
        }

        run(stage(1920, 1080, bg: SKColors.White), clips);
    }
}
