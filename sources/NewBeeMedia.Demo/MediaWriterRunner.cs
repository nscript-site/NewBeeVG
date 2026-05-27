using Geb.Image;

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
            var img = new ImageBgr24(640, 480);
            for (int i = 0; i < 100; i++)
            {
                var c = new Bgr24((byte)(i * 2), (byte)(i * 3), (byte)(i * 4));
                img.Fill(c);
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
