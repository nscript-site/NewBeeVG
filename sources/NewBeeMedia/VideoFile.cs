namespace NewBeeMedia;

/// <summary>
/// 视频文件的门面类
/// </summary>
public class VideoFile
{
    //public static IEnumerable<ImageBgr24> Mixin(Func<List<ImageBgr24>> func, params IEnumerable<ImageBgr24>[] its)
    //{
    //    if (its == null || its.Length == 0 || func == null) yield break;
    //    var list = new List<ImageBgr24>();
    //}

    /// <summary>
    /// 隔一定时间，抽取一帧进行处理
    /// </summary>
    /// <param name="filePath">视频路径</param>
    /// <param name="frameInternal">帧间隔时间，单位为秒</param>
    /// <param name="action">回调函数 func(float time, float duration, ImageBgr32 image)</param>
    /// <param name="cancel">可撤销令牌)</param>
    public static void ForEach(string filePath, float frameInternal = 0.1f, Action<float, float, ImageBgr24> action = null, CancellationToken? cancel = null)
    {
        frameInternal = Math.Max(0.01f, frameInternal);

        double nextTime = 0;

        using (MediaReader mediaReader = new MediaReader(filePath))
        {
            VideoStreamDecoder videoReader = mediaReader.VideoStream;
            var duration = videoReader.Duration.TotalSeconds;
            if (videoReader != null)
            {
                int width = videoReader.Width;
                int height = videoReader.Height;

                while (true)
                {
                    if (cancel != null && cancel.Value.IsCancellationRequested) break;

                    bool hasFrame = videoReader.ReadFrame();

                    if (hasFrame == false) break;
                    var time = videoReader.Time;
                    if (time >= nextTime)
                    {
                        nextTime = Math.Max(nextTime + frameInternal, time + 0.01);
                        var img = videoReader.CurrentFrameBgr24(width, height);
                        action?.Invoke((float)time, (float)duration, img);
                        //yield return img;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 隔一定时间，抽取一帧进行处理，通过 yield 返回。 
    /// </summary>
    /// <param name="filePath">视频路径</param>
    /// <param name="frameInternal">帧间隔时间，单位为秒</param>
    /// <param name="action">回调函数 func(float time, float duration, ImageBgr32 image)</param>
    /// <param name="cancel">可撤销令牌)</param>
    /// <returns>可迭代的图像序列</returns>
    public static IEnumerable<ImageBgr24> ForEachYield(string filePath, float frameInternal = 0.1f, Action<float, float, ImageBgr24> action = null, CancellationToken? cancel = null)
    {
        frameInternal = Math.Max(0.01f, frameInternal);

        double nextTime = 0;

        using (MediaReader mediaReader = new MediaReader(filePath))
        {
            VideoStreamDecoder videoReader = mediaReader.VideoStream;
            var duration = videoReader.Duration.TotalSeconds;
            if (videoReader != null)
            {
                int width = videoReader.Width;
                int height = videoReader.Height;

                while (true)
                {
                    if (cancel != null && cancel.Value.IsCancellationRequested) 
                        yield break;

                    bool hasFrame = videoReader.ReadFrame();

                    if (hasFrame == false) break;
                    var time = videoReader.Time;
                    if (time >= nextTime)
                    {
                        nextTime = Math.Max(nextTime + frameInternal, time + 0.01);
                        var img = videoReader.CurrentFrameBgr24(width, height);
                        action?.Invoke((float)time, (float)duration, img);
                        yield return img;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取视频的封面图
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static byte[]? GetFaceImage(string filePath, int height = 480)
    {
        const int MinSize = 120;

        height = Math.Max(MinSize, 480);

        try
        {
            using (MediaReader mediaReader = new MediaReader(filePath))
            {
                var videoReader = mediaReader.VideoStream;
                if (videoReader == null) return null;

                var duration = videoReader.Duration.TotalSeconds;
                if (videoReader != null)
                {
                    float scale = height / (float)videoReader.Height;
                    int width = (int)Math.Round(videoReader.Width * scale);
                    width = Math.Max(MinSize, width);
                    bool hasFrame = videoReader.ReadFrame();
                    if (hasFrame == false) return null;
                    using var img = videoReader.CurrentFrameBgr24(width, height);
                    if (img == null) return null;
                    return img.ToJpegData();
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }
}
