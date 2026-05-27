namespace NewBeeMedia;

public class SwsContextHolder : IDisposable
{
    public int SrcW, SrcH, DstW, DstH;
    public AVPixelFormat SrcFmt, DstFmt;
    public SwsFlags Flags;

    public unsafe SwsContext* Context;

    public unsafe SwsContextHolder(int srcW, int srcH, AVPixelFormat srcFmt, int dstW, int dstH, AVPixelFormat dstFmt, SwsFlags flags)
    {
        SrcW = srcW;
        SrcH = srcH;
        SrcFmt = srcFmt;
        DstW = dstW;
        DstH = dstH;
        DstFmt = dstFmt;
        Flags = flags;
        Context = ffmpeg.sws_getContext(srcW, srcH, srcFmt, dstW, dstH,
            dstFmt, (int)flags, null, null, null);
    }

    public Boolean Match(int srcW, int srcH, AVPixelFormat srcFmt, int dstW, int dstH, AVPixelFormat dstFmt, SwsFlags flags)
    {
        return SrcW == srcW && SrcH == srcH && SrcFmt == srcFmt && DstW == dstW && DstH == dstH && DstFmt == dstFmt && Flags == flags;
    }

    public unsafe void Dispose()
    {
        if (Context != null)
        {
            ffmpeg.sws_freeContext(Context);
            Context = null;
        }
    }

    ~SwsContextHolder()
    {
        Dispose();
    }
}
