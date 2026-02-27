namespace NewBeeMedia;

using Geb.Image;

public unsafe class VideoStreamDecoder : BaseDecoder
{
    #region Fields

    private AVFrame* m_avFrame = null;
    private SwsContextHolder m_sws = null;
    private AVPacket* m_avPacket = null;

    #endregion

    #region Properties

    public int Width
    {
        get { return m_pCodecCtx->width; }
    }

    public int Height
    {
        get { return m_pCodecCtx->height; }
    }

    /// <summary>
    /// 帧率
    /// </summary>
    public double FrameRate
    {
        get
        {
            return FrameRateRational.ToDouble();
        }
    }

    public AVRational FrameRateRational
    {
        get
        {
            if (m_avStream->r_frame_rate.den > 0 && m_avStream->r_frame_rate.num > 0)
                return m_avStream->r_frame_rate;
            else
                return new AVRational { den = m_pCodecCtx->time_base.num, num = m_pCodecCtx->time_base.den };
        }
    }

    public long BitRate
    {
        get { return m_pCodecCtx->bit_rate; }
    }

    public long FrameCount
    {
        get { return (long)(FrameRate * m_file.RawDuration); }
    }

    public AVPixelFormat PixelFormat
    {
        get { return m_pCodecCtx->pix_fmt; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new VideoDecoderStream over a specific filename.
    /// </summary>
    /// <param name="Filename">File to decode</param>
    internal VideoStreamDecoder(MediaReader file, int idx, AVStream* stream)
        : base(file, idx, stream)
    {
        // allocate video frame
        m_avFrame = ffmpeg.av_frame_alloc();
        m_avPacket = ffmpeg.av_packet_alloc();
    }

    #endregion

    #region Methods

    public bool ReadFrame()
    {
        if (m_disposed == true) return false;

        bool result = false;
        try
        {
            ffmpeg.av_frame_unref(m_avFrame);
            int error;
            do
            {
                AVPacket packet = ReadNextPacket();
                ffmpeg.avcodec_send_packet(m_pCodecCtx, &packet).ThrowExceptionIfError();
                error = ffmpeg.avcodec_receive_frame(m_pCodecCtx, m_avFrame);
                ffmpeg.av_packet_unref(&packet);
            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

            error.ThrowExceptionIfError();
            result = true;
        }
        catch (System.IO.EndOfStreamException ex)
        {
        }

        return result;
    }

    internal override void GuessTimeBase()
    {
        if (this.m_avStream->time_base.den > 0 && this.m_avStream->time_base.num > 0)
        {
            _timeBase = m_avStream->time_base.ToDouble();
        }
        else
        {
            _timeBase = 1 / FrameRate;
        }
    }

    private SwsContextHolder GetSwsContextHolder(int srcW, int srcH, AVPixelFormat srcFmt, int dstW, int dstH, AVPixelFormat dstFmt, int flags)
    {
        if (m_sws == null)
        {
            m_sws = new SwsContextHolder(srcW, srcH, srcFmt, dstW, dstH, dstFmt, flags);
            return m_sws;
        }
        else if (m_sws.Match(srcW, srcH, srcFmt, dstW, dstH, dstFmt, flags) == true)
        {
            return m_sws;
        }
        else
        {
            m_sws.Dispose();
            m_sws = new SwsContextHolder(srcW, srcH, srcFmt, dstW, dstH, dstFmt, flags);
            return m_sws;
        }
    }

    public unsafe ImageU8 CurrentFrameU8(int width, int height)
    {
        if (m_avFrame == null || m_avFrame->data[0] == null) return null;

        ImageU8 image = new ImageU8(width, height);
        WriteToFrame((Byte*)image.Start, image.Width, AVPixelFormat.AV_PIX_FMT_GRAY8, width, height);
        return image;
    }

    /// <summary>
    /// 将缓存在本地的当前帧转换为图片。
    /// </summary>
    /// <param name="width">图片宽</param>
    /// <param name="height">图片高</param>
    /// <returns>得到的图片</returns>
    public unsafe ImageBgr24 CurrentFrameBgr24(int width, int height)
    {
        if (m_avFrame == null || m_avFrame->data[0] == null) return null;

        ImageBgr24 image = new ImageBgr24(width, height);
        WriteToFrame((Byte*)image.Start, image.Width * 3, AVPixelFormat.AV_PIX_FMT_BGR24, width, height);
        return image;
    }

    /// <summary>
    /// 将缓存在本地的当前帧存在图像中，避免再次分配内存
    /// </summary>
    /// <param name="image"></param>
    /// <returns>true代表存储成功，false代表未存储成功</returns>
    public unsafe bool CurrentFrame(ImageBgr24 image)
    {
        if (m_avFrame == null || m_avFrame->data[0] == null || image == null) return false;
        WriteToFrame((Byte*)image.Start, image.Width * 3, AVPixelFormat.AV_PIX_FMT_BGR24, image.Width, image.Height);
        return true;
    }

    /// <summary>
    /// 将缓存在本地的当前帧转换为图片。
    /// </summary>
    /// <param name="width">图片宽</param>
    /// <param name="height">图片高</param>
    /// <returns>得到的图片</returns>
    public unsafe ImageBgra32 CurrentFrameBgra32(int width, int height)
    {
        if (m_avFrame == null || m_avFrame->data[0] == null) return null;

        ImageBgra32 image = new ImageBgra32(width, height);
        WriteToFrame((Byte*)image.Start, image.Width * 4, AVPixelFormat.AV_PIX_FMT_BGRA, width, height);
        return image;
    }

    public unsafe bool WriteToFrame(byte* frameData, int stride, AVPixelFormat frameFmt, int width, int height)
    {
        if (m_avFrame == null || m_avFrame->data[0] == null) return false;

        SwsContextHolder sws = GetSwsContextHolder(this.Width, this.Height, this.PixelFormat, width, height, frameFmt, ffmpeg.SWS_BILINEAR);
        byte*[] dstData = { frameData };
        int[] dstLinesize = { stride };
        
        ffmpeg.sws_scale(sws.Context, m_avFrame->data,
           m_avFrame->linesize, 0, Height, dstData,
           dstLinesize);
        return true;
    }

    public ImageBgr24 CurrentFrameBgr24()
    {
        return CurrentFrameBgr24(Width, Height);
    }

    public ImageBgra32 CurrentFrameBgra32()
    {
        return CurrentFrameBgra32(Width, Height);
    }

    public ImageU8 NextFrameU8(int width, int height)
    {
        try
        {
            ReadFrame();
        }
        catch (System.IO.EndOfStreamException)
        {
            return null;
        }

        return CurrentFrameU8(width, height);
    }

    /// <summary>
    /// 获取下一帧替换当前帧，并返回图片
    /// </summary>
    /// <param name="width">图片宽度</param>
    /// <param name="height">图片高度</param>
    /// <returns>下一帧</returns>
    public ImageBgr24 NextFrameBgr24(int width, int height)
    {
        try
        {
            ReadFrame();
        }
        catch (System.IO.EndOfStreamException)
        {
            return null;
        }

        return CurrentFrameBgr24(width, height);
    }

    public ImageBgra32 NextFrameBgra32(int width, int height)
    {
        try
        {
            ReadFrame();
        }
        catch (System.IO.EndOfStreamException)
        {
            return null;
        }

        return CurrentFrameBgra32(width, height);
    }

    public ImageBgr24 NextFrameBgr24()
    {
        return NextFrameBgr24(Width, Height);
    }

    public ImageBgra32 NextFrameBgra32()
    {
        return NextFrameBgra32(Width, Height);
    }

    protected override void Dispose(bool disposing)
    {
        if (m_avFrame != null)
        {
            ffmpeg.av_frame_unref(m_avFrame);
            ffmpeg.av_free(m_avFrame);
            m_avFrame = null;
        }

        if (m_sws != null)
        {
            m_sws.Dispose();
        }

        base.Dispose(disposing);
    }

    double CalcTime(long pts)
	    {
        return pts * _timeBase;

        // AVStream* s = this.m_file.FormatContext->streams[0];
        // Console.WriteLine(s->pts);
        ////libffmpeg::AVStream* vs = AudioStream;
        //if (m_avStream->time_base.den > 0 && m_avStream->time_base.num > 0)
        //{
        //    return pts * m_avStream->time_base.num / (double)(m_avStream->time_base.den);
        //}
        //else if (m_avStream->r_frame_rate.den > 0 && m_avStream->r_frame_rate.num > 0)
        //{
        //    return pts * m_avStream->r_frame_rate.den / (double)(m_avStream->r_frame_rate.num);
        //}
        //else
        //{
        //    return pts * m_avCodecCtx.time_base.num / (double)(m_avCodecCtx.time_base.den);
        //}
	    }

    public unsafe bool Seek(double time, out ImageBgra32 image, bool seekKeyFrame = false)
    {
        image = null;
        if (base.m_disposed)
        {
            return false;
        }
        long timestamp = (long)(time / _timeBase);
        int num = ffmpeg.av_seek_frame(base.m_file._fmtCxt, this.StreamIndex, timestamp, ffmpeg.AVSEEK_FLAG_BACKWARD | ffmpeg.AVSEEK_FLAG_FRAME);
        ffmpeg.avcodec_flush_buffers(base.m_pCodecCtx);
        if (num < 0)
            return false;
        base.ClearQueue();
        if (!seekKeyFrame)
        {
            do
            {
                if (image != null) image.Dispose();

                image = NextFrameBgra32();
                if (image == null)
                    break;
            }
            while (!(CalcTime(base.Dts) >= time));
        }
        return true;
    }

    public unsafe bool Seek(double time, out ImageBgr24 image, bool seekKeyFrame = false)
    {
        image = null;
        if (base.m_disposed)
        {
            return false;
        }
        long timestamp = (long)(time / _timeBase);
        int num = ffmpeg.av_seek_frame(base.m_file._fmtCxt, this.StreamIndex, timestamp, ffmpeg.AVSEEK_FLAG_BACKWARD | ffmpeg.AVSEEK_FLAG_FRAME);
        ffmpeg.avcodec_flush_buffers(base.m_pCodecCtx);
        if (num < 0)
            return false;
        base.ClearQueue();
        if(!seekKeyFrame)
        {
            do
            {
                if (image != null) image.Dispose();

                image = NextFrameBgr24();
                if (image == null)
                    break;
            }
            while (!(CalcTime(base.Dts) >= time));
        }
        return true;
    }

    #endregion
}
