using System.Collections.Generic;

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
        var checkerboard = Image(Textures.GenerateCheckerboardBitmap(960, 540, cellSize: 40, color1: SKColors.Black, color2: SKColors.White)).Id("checkerboard");
        var diagonalStripes = Image(Textures.GenerateDiagonalStripesBitmap(960, 540, stripeWidth: 30, gapWidth: 20, angle: 45, color: SKColors.Black, gapColor: SKColors.Transparent)).Id("diagonalStripes");
        var vignette = Image(Textures.GenerateVignetteBitmap(960, 540, color: SKColors.Black, centerX: 0.5f, centerY: 0.5f, radius: 400, alpha: 0, edgeAlpha: 200)).Id("vignette");
        var cloud = Image(Textures.GenerateCloudBitmap(960, 540, cloudColor: SKColors.White, baseFrequencyX: 0.02f, baseFrequencyY: 0.02f, numOctaves: 5, maxAlpha: 200)).Id("cloud");
        var wood = Image(Textures.GenerateWoodBitmap(960, 540, woodColor: new SKColor(160, 120, 80), ringColor: new SKColor(100, 70, 40), frequency: 0.03f, seed: 0)).Id("wood");
        var dotMatrix = Image(Textures.GenerateDotMatrixBitmap(960, 540, dotRadius: 4f, spacingX: 20f, spacingY: 20f, color: SKColors.Black, alpha: 255)).Id("dotMatrix");
        var linearGradient = Image(Textures.GenerateLinearGradientBitmap(960, 540, startColor: SKColors.Red, endColor: SKColors.Blue, startX: 0, startY: 0, endX: 1, endY: 1)).Id("linearGradient");
        var filmGrain = Image(Textures.GenerateFilmGrainBitmap(960, 540, density: 0.2f, seed: 42)).Id("filmGrain");
        var filmBurn = Image(Textures.GenerateFilmBurnBitmap(960, 540, seed: 100)).Id("filmBurn");
        var vhs = Image(Textures.GenerateVHSTextureBitmap(960, 540, seed: 55)).Id("vhs");
        var dustAndDirt = Image(Textures.GenerateDustAndDirtBitmap(960, 540, count: 400, seed: 123)).Id("dustAndDirt");
        var glitch = Image(Textures.GenerateGlitchBitmap(960, 540, seed: 200)).Id("glitch");
        var halftone = Image(Textures.GenerateHalftoneBitmap(960, 540, seed: 300)).Id("halftone");
        var pixelate = Image(Textures.GeneratePixelateBitmap(960, 540, blockSize: 25, seed: 400)).Id("pixelate");
        var dynamicTransition = Image(Textures.GenerateDynamicTransitionBitmap(960, 540, seed: 500)).Id("dynamicTransition");

        NBDrawingClip[] BuildClips()
        {
            var list = new List<NBDrawingClip>();
            var images = new List<NBImage> {
                noise, smoke, scratches, raindrops, snow, dust, flare, scanlines, grain,
                checkerboard, diagonalStripes, vignette, cloud,
                wood, dotMatrix, linearGradient, filmGrain, filmBurn,
                vhs, dustAndDirt, glitch, halftone, pixelate, dynamicTransition
            };
            foreach (var image in images)
            {
                Console.WriteLine(image.Id);
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
