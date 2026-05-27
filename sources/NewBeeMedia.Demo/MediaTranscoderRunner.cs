using Geb.Image;

namespace NewBeeMedia.Demo;

internal class MediaTranscoderRunner
{
    public static void Run()
    {
        var filePath = "D:/测试数据/videos/01.mp4";
        var outputFilePath = $"{filePath}_transcoded.mp4";
        if(File.Exists(outputFilePath)) File.Delete(outputFilePath);

        try
        {
            var ok = MediaTranscoder.Transcode(filePath, outputFilePath,
                // 对每帧图像进行处理的回调，这里我们在图像上绘制一个红色圆形
                (img,t) => 
                {
                    img.FillCircle(100,100, Bgr24.RED, 50);
                },
                p => { Console.WriteLine($"\t{p:P2}"); });

            Console.WriteLine(ok
                ? $"Transcoding completed successfully. Output file: {outputFilePath}"
                : "Transcoding failed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during transcoding: {ex.Message}");
        }
    }
}
