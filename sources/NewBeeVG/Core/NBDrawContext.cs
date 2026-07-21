namespace NewBeeVG;

public struct NBDrawContext
{
    public static NBDrawContext? Current { get; internal set; }

    public int frame;
    public int width;
    public int height;
    public double progress;
    public int durationFrames;
    public NBLayoutable? content;
    public NBLayoutable? mask;
}