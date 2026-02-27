namespace NewBeeMedia;

public static class Extentions
{
    public static AVRational Inverse(this AVRational val)
    {
        return new AVRational { den = val.num, num = val.den };
    }
}
