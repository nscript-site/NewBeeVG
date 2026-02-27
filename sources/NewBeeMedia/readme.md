#Geb.Media

Geb.Media 是基于 ffmpeg 库的音视频处理库，接口设计的简单易用。

```csharp
    String inputPath = @"test.mp4";
    String outPath = @"./out.mp4";
    Stopwatch sw = new Stopwatch();
    sw.Start();
    MediaReader mediaReader = new MediaReader(inputPath);
    MediaWriter mediaWriter = new MediaWriter(outPath, mediaReader.FrameWidth, mediaReader.FrameHeight);
    int frmIdx = 0;
    while(true)
    {
        var img = mediaReader.NextFrameBgr24();
        if (img != null)
        {
            Console.WriteLine($"Decode Frame {frmIdx.ToString().PadLeft(6, '0')}: Width - {img.Width}, Height - {img.Height} ");
            mediaWriter.WriteFrame(img);
            Console.WriteLine($"Encode Frame {frmIdx.ToString().PadLeft(6, '0')}: Width - {img.Width}, Height - {img.Height} ");
            frmIdx++;
            img.Dispose();
        }
        else
            break;
    }
    mediaReader.Close();
    mediaWriter.Close();
    sw.Stop();
    Console.WriteLine("Duration:" + sw.Elapsed.TotalSeconds.ToString("0.000") + "s");
    Console.ReadLine();
```

## VideoFile 门面类

VideoFile 提供了对视频文件常见的门面操作

> 对视频每间隔一段时间抽取一帧进行处理:

```csharp
VideoFile.ForEach("D:\\测试数据\\多语种测试视频\\en.mp4", 0.1f, 
    (float time, float duration, ImageBgr24 image) => {
        Console.WriteLine($"{time.ToString("0.000")}/{duration.ToString("0.000")} Image({image.Width}/{image.Height})");
        image?.Dispose();
    }
);
```