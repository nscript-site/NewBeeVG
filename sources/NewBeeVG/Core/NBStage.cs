using SkiaSharp;

namespace NewBeeVG;

public class NBStage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; } = 25;
    public SKColor? Background { get; set; }

    public void DrawBackground(SKCanvas canvas, bool preview)
    {
        if (Background != null)
        {
            var skColor = Background.Value;
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = skColor,
                IsAntialias = true
            };
            
            canvas.DrawRect(new SKRect(0, 0, Width, Height), paint);
        }
        else if(preview == true)
        {
            using var shader = DrawingHelper.CreateCheckerboardShader(20, SKColors.White, SKColor.Parse("#D2D2D2"));
            using var paint = new SKPaint
            {
                Shader = shader
            };

            canvas.DrawRect(new SKRect(0, 0, Width, Height), paint);
        }
    }
}
