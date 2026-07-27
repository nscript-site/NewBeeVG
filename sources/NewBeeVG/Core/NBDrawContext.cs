namespace NewBeeVG;

public struct NBDrawContext
{
    public static NBDrawContext? Current { get; internal set; }

    public static NBDrawContext CurrentOrDefault => Current ?? new NBDrawContext();

    public int frame;
    public int width;
    public int height;
    public double progress;
    public int durationFrames;
    public NBLayoutable? content;
    public NBLayoutable? mask;
}