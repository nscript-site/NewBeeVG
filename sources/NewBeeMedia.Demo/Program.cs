namespace NewBeeMedia.Demo;

internal class Program
{
    static void Main(string[] args)
    {
        FFmpegUtils.Initialize();

        //MediaReaderRunner.Run();        // 测试读取视频流
        //MediaTranscoderRunner.Run();    // 测试转码，还存在问题
        MediaWriterRunner.Run();        // 测试写入视频流
    }
}
