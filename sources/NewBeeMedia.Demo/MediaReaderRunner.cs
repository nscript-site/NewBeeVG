namespace NewBeeMedia.Demo;

internal class MediaReaderRunner : BaseRunner
{
    public static void Run()
    {
        string filePath = GetFilePath();
        try
        {
            MediaReader reader = new MediaReader(filePath);
            Console.WriteLine();
            Console.WriteLine($"MediaReader created successfully for file: {filePath}");
            Console.WriteLine($"\tVideo Stream Count: {reader.VideoStreamCount}");
            Console.WriteLine($"\tAudio Stream Count: {reader.AudioStreamCount}");
            Console.WriteLine($"\tDuration: {reader.Duration.TotalSeconds} seconds");

            var vs = reader.VideoStream;
            if(vs == null)
            {
                Console.WriteLine($"No video stream found in the file: {filePath}");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Video Stream Info:");
            Console.WriteLine($"\tWidth: {vs.Width}");
            Console.WriteLine($"\tHeight: {vs.Height}");
            Console.WriteLine($"\tFrame Rate: {vs.FrameRate}");

            var audioStream = reader.AudioStream;
            if (audioStream != null)
            {
                Console.WriteLine();
                Console.WriteLine($"Audio Stream Info:");
                Console.WriteLine($"\tSample Rate: {audioStream.SampleRate}");
                Console.WriteLine($"\tChannels: {audioStream.Channels}");
                Console.WriteLine($"\tUncompressedBytesPerSecond: {audioStream.UncompressedBytesPerSecond}");
            }
            else
            {
                Console.WriteLine($"No audio stream found in the file: {filePath}");
            }

            Console.WriteLine();
            Console.WriteLine($"Reading frames from the video stream:");
            int frame = 0;
            while (vs.ReadFrame())
            {
                using var img = vs.NextFrameSKBitmap(vs.Width, vs.Height);
                if (img != null)
                {
                    Console.WriteLine($"\tFrame {frame}: {img.Width}x{img.Height}");
                }
                else
                {
                    Console.WriteLine($"\tFailed to read frame {frame}.");
                }
                frame++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating the MediaReader:{filePath}");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
