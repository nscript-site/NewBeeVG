using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeMedia.Demo;

internal class MediaTranscoderRunner
{
    public static void Run()
    {
        var filePath = "D:/测试数据/videos/01.avi";
        var outputFilePath = $"{filePath}_transcoded.mp4";
        if(File.Exists(outputFilePath)) File.Delete(outputFilePath);

        try
        {
            MediaTranscoder.Transcode(filePath, outputFilePath,
                null,
                p => { Console.WriteLine($"\t{p.ToString("P2")}"); });
            Console.WriteLine($"Transcoding completed successfully. Output file: {outputFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during transcoding: {ex.Message}");
        }
    }
}
