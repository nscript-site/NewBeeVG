using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class TexturesSample
{
    public static void Run()
    {
        // 生成各种纹理并展示
        var noise = Image(Textures.GenerateNoiseBitmap(960, 540)).Id("noise");
        var smoke = Image(Textures.GenerateSmokeBitmap(960, 540, color: SKColors.White, alpha: 150)).Id("smoke");
        var scratches = Image(Textures.GenerateScratchesBitmap(960, 540, count: 30, seed: 42)).Id("scratches");
        var raindrops = Image(Textures.GenerateRaindropsBitmap(960, 540, count: 150, seed: 100)).Id("raindrops");
        var snow = Image(Textures.GenerateSnowflakesBitmap(960, 540, count: 100, alpha: 200)).Id("snow");
        var dust = Image(Textures.GenerateDustBitmap(960, 540, count: 500, seed: 55)).Id("dust");
        var flare = Image(Textures.GenerateLensFlareBitmap(960, 540, centerX: 0.3f, centerY: 0.4f, maxRadius: 150)).Id("flare");
        var scanlines = Image(Textures.GenerateScanlinesBitmap(960, 540, lineSpacing: 4, lineThickness: 2, alpha: 80)).Id("scanlines");
        var grain = Image(Textures.GenerateGrainBitmap(960, 540, density: 0.08f, seed: 123)).Id("grain");

        NBDrawingClip[] BuildClips()
        {
            var list = new List<NBDrawingClip>();
            var images = new List<NBImage>() { noise, smoke, scratches, raindrops, snow, dust, flare, scanlines, grain };
            foreach (var image in images)
            {
                Panel([
                    image.Align(null,null),
                    TextBlock(image.Id??"").Font(40, SKColors.White).Margin(50).Align(-1,-1)
                ]).AsClip(out var clip1, frames: 40, name: image.Id ?? "image");
                list.Add(clip1);
            }
            return list.ToArray();
        }

        run(stage(1920, 1080, bg: SKColors.Orange), BuildClips());
    }
}
