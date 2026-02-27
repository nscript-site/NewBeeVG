namespace NewBeeMedia;

public class MediaClip
{
    public MediaSource Source { get; set; }
    public double Start { get; set; }
    public double End { get; set; }
    public double Duration { get { return Math.Max(0, End - Start); } }
}
