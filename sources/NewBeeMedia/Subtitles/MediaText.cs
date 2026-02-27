namespace NewBeeMedia.Subtitles;

/// <summary>
/// 媒体文字。代表媒体上出现的一段文字
/// </summary>
public class MediaText
{
    /// <summary>
    /// 文字内容
    /// </summary>
    public String Content { get; set; }

    /// <summary>
    /// 在视频上出现的时间
    /// </summary>
    public double Start { get; set; }

    /// <summary>
    /// 在视频上消失的时间
    /// </summary>
    public double End { get; set; }

    /// <summary>
    /// 在视频上出现的位置。如果位置是变化的，这里应是最初位置
    /// </summary>
    public Rect? Rect { get; set; }
}
