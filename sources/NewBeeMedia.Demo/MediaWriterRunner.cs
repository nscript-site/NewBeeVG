using Geb.Image;
using SkiaSharp;
using System.Drawing;

namespace NewBeeMedia.Demo;

internal class MediaWriterRunner
{
    public static void Run()
    {
        var filePath = "D:/测试数据/videos/MediaWriterRunner_test.mp4";
        if(File.Exists(filePath)) File.Delete(filePath);

        try
        {
            using var writer = new MediaWriter(filePath, 640, 480, 30);
            var img = new SKBitmap(640, 480);
            for (int i = 0; i < 100; i++)
            {
                using var canvas = new SKCanvas(img);

                canvas.Clear(SKColors.White);

                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SKColors.Red,
                    Style = SKPaintStyle.Fill
                };

                canvas.DrawCircle(100 + i, 100 + i, 50, paint);
                writer.WriteFrame(img);
                Console.WriteLine($"Written frame {i}");
            }
            Console.WriteLine($"Media writing completed successfully. Output file: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during media writing: {ex.Message}");
        }
    }
}
