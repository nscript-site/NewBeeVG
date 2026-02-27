namespace NewBeeMedia;

public static class MediaConfig
{
    public static bool GPU_nVidia = false;
    public static bool GPU_QuickSync = false;

    public enum AVLogLevel
    {
        Trace, Debug, Error, Warning
    }

    public const int AVCODEC_MAX_AUDIO_FRAME_SIZE = 1024 * 8;

    /// <summary>
    /// 设置日志级别
    /// </summary>
    /// <param name="level"></param>
    public static void SetLogLevel(AVLogLevel level)
    {
        switch (level)
        {
            case AVLogLevel.Debug:
                ffmpeg.av_log_set_level(ffmpeg.AV_LOG_DEBUG);
                break;
            case AVLogLevel.Trace:
                ffmpeg.av_log_set_level(ffmpeg.AV_LOG_TRACE);
                break;
            case AVLogLevel.Warning:
                ffmpeg.av_log_set_level(ffmpeg.AV_LOG_WARNING);
                break;
            case AVLogLevel.Error:
            default:
                ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
                break;
        }
    }
}
