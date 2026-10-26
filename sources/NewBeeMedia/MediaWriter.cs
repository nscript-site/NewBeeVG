using SkiaSharp;

namespace NewBeeMedia;

public unsafe class MediaWriter : MediaHandler, IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int AvInterruptCb(void* ctx);

    public delegate int InterruptCallback();

    private const int Iframeinterval = 3000;

    private static readonly bool Hwnvidia = true;
    private static bool _hwqsv = true;
    private readonly byte[] _convOut = new byte[65536];
    private readonly AutoResetEvent _frameWritten = new AutoResetEvent(false);
    private readonly bool _isAudio;
    private readonly AutoResetEvent _recordingClosed = new AutoResetEvent(false);
    private bool _abort;
    private byte[] _audioBuffer = new byte[65536];
    private int _audioBufferSizeCurrent;
    private AVCodecContext* _audioCodecContext;
    private AVFrame* _audioFrame, _videoFrame;

    private AVIOInterruptCB_callback_func _aviocb;
    private AVPixelFormat _avPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;

    private bool _closing;
    private GCHandle _convHandle;
    private AVFormatContext* _formatContext;
    private int _frameNumber;

    private bool _ignoreAudio;
    private AvInterruptCb _interruptCallback;

    private IntPtr _interruptCallbackAddress;

    private bool _isConstantFramerate;

    private DateTime _keyframeInterval;
    private long _lastAudioPts;

    private DateTime _lastPacket;
    private double _maxLevel = -1;
    private bool _opened;
    private SwsContext* _pConvertContext;
    private SwrContext* _swrContext;
    private AVCodec* _videoCodec;
    private AVCodecContext* _videoCodecContext;
    private AVStream* _videoStream, _audioStream;
    private int _width, _height;
    private double _framerate;

    public DateTime CreatedDate;

    public double FrameRate { get { return _framerate; } }

    /// <summary>
    /// 音频采样格式
    /// </summary>
    public AVSampleFormat AudioSampleFormat { get { return _audioCodecContext!=null? _audioCodecContext->sample_fmt : AVSampleFormat.AV_SAMPLE_FMT_NONE; } }

    /// <summary>音频采样率（Hz）。</summary>
    public int AudioSampleRate { get; private set; } = 44100;

    /// <summary>音频声道数。</summary>
    public int AudioChannels { get; private set; } = 2;

    public string Filename;
    public int InChannels = 1;
    public int InSampleRate = 44100;
    public bool IsFile = true;
    public bool IsTimelapse;
    private readonly string movflags = ""; //"faststart";
    public int OutChannels = 1;
    public int OutSampleRate = 44100;
    public long SizeBytes;

    public int Timeout = 5000;
    private bool disposedValue;

    public MediaWriter(string fileName, AVCodecID audioCodec, DateTime created) : base()
    {
        Open(fileName, -1, -1, AVCodecID.AV_CODEC_ID_NONE, 0, audioCodec, created);
        _isAudio = true;
    }

    public MediaWriter(string fileName, AVCodecID audioCodec) : base()
    {
        Open(fileName, -1, -1, AVCodecID.AV_CODEC_ID_NONE, 0, audioCodec, DateTime.Now);
        _isAudio = true;
    }

    public MediaWriter(string fileName, int width, int height, AVCodecID videoCodec, DateTime created) : base()
    {
        Open(fileName, width, height, videoCodec, 0, AVCodecID.AV_CODEC_ID_NONE, created);
    }

    /// <summary>
    /// 创建视频写入器（H.264 + MP3 音轨）。
    /// </summary>
    /// <param name="containAudio">为 false 时不创建音轨。</param>
    public MediaWriter(string fileName, int width, int height, double framerate, bool containAudio = true) : base()
    {
        Open(fileName, width, height, AVCodecID.AV_CODEC_ID_H264, framerate,
            containAudio ? AVCodecID.AV_CODEC_ID_MP3 : AVCodecID.AV_CODEC_ID_NONE,
            DateTime.Now);
    }

    /// <summary>
    /// 创建带音轨的视频写入器，并显式指定音频采样率与声道数。
    /// </summary>
    /// <param name="audioSampleRate">音频采样率（Hz），需为编码器支持的值（MP3：32000/44100/48000）。</param>
    /// <param name="audioChannels">音频声道数，默认 2（立体声）。</param>
    /// <param name="containAudio">为 false 时不创建音轨。</param>
    public MediaWriter(string fileName, int width, int height, double framerate,
        int audioSampleRate, int audioChannels = 2, bool containAudio = true) : base()
    {
        Open(fileName, width, height, AVCodecID.AV_CODEC_ID_H264, framerate,
            containAudio ? AVCodecID.AV_CODEC_ID_MP3 : AVCodecID.AV_CODEC_ID_NONE,
            DateTime.Now, audioSampleRate, audioChannels);
    }

    public MediaWriter(string fileName, int width, int height, AVCodecID videoCodec, int framerate,
        AVCodecID audioCodec, DateTime created) : base()
    {
        Open(fileName, width, height, videoCodec, framerate, audioCodec, created);
    }
    
    public bool Closed => !_opened;

    public int InterruptCb(void* ctx)
    {
        if ((DateTime.UtcNow - _lastPacket).TotalMilliseconds > Timeout || _abort)
        {
            if (!_abort)
            {
                Logger.LogMessage("Writer Timeout");
            }

            Logger.LogMessage("Aborting Writer");
            _abort = true;
            return 1;
        }
        return 0;
    }

    public int InterruptCb()
    {
        if ((DateTime.UtcNow - _lastPacket).TotalMilliseconds > Timeout || _abort)
        {
            return 1;
        }
        return 0;
    }

    private void Open(string fileName, int width, int height, AVCodecID videoCodec, double framerate, AVCodecID audioCodec, DateTime created,
        int audioSampleRate = 44100, int audioChannels = 2)
    {
        CreatedDate = created;
        Filename = fileName;
        _abort = false;
        _ignoreAudio = false;

        if (videoCodec != AVCodecID.AV_CODEC_ID_NONE)
        {
            IsTimelapse = framerate != 0;

            if (((width & 1) != 0) || ((height & 1) != 0))
            {
                throw new ArgumentException("Video file resolution must be a multiple of two.");
            }
        }

        int i;
        _lastPacket = DateTime.UtcNow;
        var outputFormat = ffmpeg.av_guess_format(null, fileName, null);
        if (outputFormat == null)
        {
            switch (videoCodec)
            {
                default:
                case AVCodecID.AV_CODEC_ID_MPEG1VIDEO:
                    outputFormat = ffmpeg.av_guess_format("mpeg1video", null, null);
                    break;
            }
        }

        _formatContext = ffmpeg.avformat_alloc_context();

        if (_formatContext == null)
        {
            throw new Exception("Cannot allocate format context.");
        }

        _interruptCallback = InterruptCb;
        _interruptCallbackAddress = Marshal.GetFunctionPointerForDelegate(_interruptCallback);

        _aviocb = new AVIOInterruptCB_callback_func
        {
            Pointer = _interruptCallbackAddress
        };
        _formatContext->interrupt_callback.callback = _aviocb;
        _formatContext->interrupt_callback.opaque = null;


        _formatContext->oformat = outputFormat;

        AVDictionary* opts = null;

        if (audioCodec != AVCodecID.AV_CODEC_ID_NONE)
        {
            AddAudioStream(audioCodec, audioSampleRate, audioChannels);
            OpenAudio();
        }

        if (videoCodec != AVCodecID.AV_CODEC_ID_NONE)
        {
            _width = width;
            _height = height;
            //_bitRate = videoBitRate;
            _framerate = framerate;
            _isConstantFramerate = framerate > 0;

            AddVideoStream(videoCodec);
            OpenVideo();

            if (videoCodec == AVCodecID.AV_CODEC_ID_MPEG1VIDEO)
            {
                ffmpeg.av_dict_set(&opts, "pkt_size", "1316", 0);
                ffmpeg.av_dict_set(&opts, "buffer_size", "65535", 0);

                //ffmpeg.av_dict_set(&opts, "crf", "30", 0);
            }
        }

        if (movflags != "")
            ffmpeg.av_dict_set(&opts, "movflags", movflags, 0);

        if ((outputFormat->flags & ffmpeg.AVFMT_NOFILE) != ffmpeg.AVFMT_NOFILE)
        {
            i = ffmpeg.avio_open2(&_formatContext->pb, fileName, ffmpeg.AVIO_FLAG_WRITE, null, &opts);
            if (i < 0)
            {
                throw new Exception("Cannot create the video file. (" + i + ")");
            }
        }

        i = ffmpeg.avformat_write_header(_formatContext, null);
        if (i < 0)
        {
            throw new Exception("Cannot write header - check disk space (" + i + ")");
        }

        ffmpeg.av_dict_free(&opts);

        _frameNumber = 0;
        _keyframeInterval = DateTime.UtcNow;
        _opened = true;
    }

    public void Close()
    {
        if (_closing)
            return;

        _closing = true;

        Task.Run(() => DoClose());
        _recordingClosed.WaitOne();
    }

    private void DoClose()
    {
        _frameWritten.Reset();
        _frameWritten.WaitOne(200);
        if (_formatContext != null)
        {
            if (IsFile)
            {
                if (_opened)
                {
                    Flush();
                }

                if (_formatContext->pb != null)
                {
                    ffmpeg.av_write_trailer(_formatContext);
                }
            }
            if (_formatContext->pb != null)
            {
                var pinprt = &_formatContext->pb;
                ffmpeg.avio_closep(pinprt);
                _formatContext->pb = null;
            }

            _audioBuffer = null;


            if (_videoFrame != null)
            {
                fixed (AVFrame** pinprt = &_videoFrame)
                {
                    ffmpeg.av_frame_free(pinprt);
                    _videoFrame = null;
                }
            }

            if (_audioFrame != null)
            {
                fixed (AVFrame** pinprt = &_audioFrame)
                {
                    ffmpeg.av_frame_free(pinprt);
                    _audioFrame = null;
                }
            }

            if (_videoCodecContext != null)
            {
                fixed (AVCodecContext** c = &_videoCodecContext)
                {
                    ffmpeg.avcodec_free_context(c);
                }
            }

            if (_audioCodecContext != null)
            {
                fixed (AVCodecContext** c = &_audioCodecContext)
                {
                    ffmpeg.avcodec_free_context(c);
                }
            }

            if (_formatContext->streams != null)
            {
                var j = (int)_formatContext->nb_streams;
                for (var i = j - 1; i >= 0; i--)
                {
                    var stream = _formatContext->streams[i];
                    if (stream != null)
                    {
                        stream->discard = AVDiscard.AVDISCARD_ALL;
                    }
                }
            }

            _videoStream = null;
            _audioStream = null;

            ffmpeg.avformat_free_context(_formatContext);
            _formatContext = null;
        }

        if (_pConvertContext != null)
        {
            ffmpeg.sws_freeContext(_pConvertContext);
            _pConvertContext = null;
        }
        if (_swrContext != null)
        {
            ffmpeg.swr_close(_swrContext);
            _swrContext = null;
        }

        if (IsFile)
        {
            try
            {
                var fi = new FileInfo(Filename);
                SizeBytes = fi.Length;
            }
            catch
            {
                SizeBytes = 0;
            }
        }

        if (_convHandle.IsAllocated)
            _convHandle.Free();
        _opened = false;
        _recordingClosed.Set();
    }

    /// <summary>
    /// 写入交错立体声浮点 PCM 字节流（样本范围 [-1, 1]）。
    /// 采样率与声道必须与构造 MediaWriter 时一致；<paramref name="offset"/> 单位为采样点。
    /// </summary>
    public void WriteAudio(byte[] soundBuffer, int soundBufferSize, long offset)
    {
        if (!_opened)
        {
            throw new Exception("An audio file was not opened yet.");
        }
        AddAudioSamples(soundBuffer, soundBufferSize, offset);

        if (_isAudio)
        {
            _frameWritten.Set();
        }
    }

    /// <summary>
    /// 写入交错立体声浮点 PCM（样本范围 [-1, 1]）。
    /// 采样率必须等于构造 MediaWriter 时指定的 <c>audioSampleRate</c>。
    /// </summary>
    /// <param name="stereoPcm">交错样本：<c>[L0, R0, L1, R1, ...]</c></param>
    /// <param name="offsetSamples">起始采样点；-1 表示紧接上一次写入。</param>
    public void WriteAudio(float[] stereoPcm, long offsetSamples = -1)
    {
        if (stereoPcm == null || stereoPcm.Length < 2) return;

        var bytes = new byte[stereoPcm.Length * sizeof(float)];
        Buffer.BlockCopy(stereoPcm, 0, bytes, 0, bytes.Length);
        WriteAudio(bytes, bytes.Length, offsetSamples);
    }

    public void WriteFrame(ImageBgr24 frame)
    {
        WriteFrame((byte*)frame.Start, frame.Width * 3, frame.Width, frame.Height, AVPixelFormat.AV_PIX_FMT_BGR24, DateTime.MinValue);
    }

    public void WriteFrame(ImageU8 frame)
    {
        WriteFrame((byte*)frame.Start, frame.Width, frame.Width, frame.Height, AVPixelFormat.AV_PIX_FMT_GRAY8, DateTime.MinValue);
    }

    public void WriteFrame(ImageBgra32 frame)
    {
        WriteFrame((byte*)frame.Start, frame.Width * 4, frame.Width, frame.Height, AVPixelFormat.AV_PIX_FMT_BGRA, DateTime.MinValue);
    }

    public void WriteFrame(SKBitmap frame)
    {
        var format = frame.ColorType switch
        {
            SKColorType.Bgra8888 => AVPixelFormat.AV_PIX_FMT_BGRA,
            SKColorType.Rgba8888 => AVPixelFormat.AV_PIX_FMT_RGBA,
            SKColorType.Gray8 => AVPixelFormat.AV_PIX_FMT_GRAY8,
            _ => throw new NotSupportedException($"Unsupported SKColorType: {frame.ColorType}")
        };
        WriteFrame((byte*)frame.GetPixels(), frame.RowBytes, frame.Width, frame.Height, format, DateTime.MinValue);
    }

    protected void WriteFrame(Byte* frameData0, int stride, int width, int height, AVPixelFormat pfmt, DateTime timestamp)
    {
        if (!_opened)
        {
            throw new Exception("A file was not opened yet.");
        }
        if (ffmpeg.avcodec_is_open(_videoCodecContext) <= 0)
            throw new Exception("codec is not open");

        if ((width != _videoCodecContext->width) || (height != _videoCodecContext->height))
        {
            throw new Exception("Frame size cannot change during recording.");
        }
        
        byte*[] srcData = { frameData0 };
        int[] srcLinesize = { stride };

        if (_pConvertContext == null)
        {
            _pConvertContext = ffmpeg.sws_getCachedContext(_pConvertContext, _videoCodecContext->width,
                _videoCodecContext->height, pfmt, _videoCodecContext->width, _videoCodecContext->height,
                _videoCodecContext->pix_fmt, (int)SwsFlags.SWS_FAST_BILINEAR, null, null, null);
        }

        if (Log("SWS_SCALE",
            ffmpeg.sws_scale(_pConvertContext, srcData, srcLinesize, 0, _videoCodecContext->height,
                _videoFrame->data, _videoFrame->linesize)))
        {
            return;
        }

        if (!_isConstantFramerate)
        {
            var pts = (long)(timestamp - CreatedDate).TotalMilliseconds;
            _videoFrame->pts = pts;
        }
        else
        {
            _videoFrame->pts = _frameNumber;
            _frameNumber++;
        }

        var packet = new AVPacket();
        //ffmpeg.av_init_packet(&packet);

        packet.data = null;
        packet.size = 0;
        if (Convert.ToInt32((DateTime.UtcNow - _keyframeInterval).TotalMilliseconds) >= Iframeinterval)
        {
            _videoFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _keyframeInterval = DateTime.UtcNow;
        }
        var ret = ffmpeg.avcodec_send_frame(_videoCodecContext, _videoFrame);
        while (ret >= 0)
        {
            ret = ffmpeg.avcodec_receive_packet(_videoCodecContext, &packet);
            if (ret == 0)
            {
                if (packet.size > 0)
                {
                    if (packet.pts != ffmpeg.AV_NOPTS_VALUE)
                        packet.pts = ffmpeg.av_rescale_q(packet.pts, _videoCodecContext->time_base,
                            _videoStream->time_base);
                    if (packet.dts != ffmpeg.AV_NOPTS_VALUE)
                        packet.dts = ffmpeg.av_rescale_q(packet.dts, _videoCodecContext->time_base,
                            _videoStream->time_base);

                    packet.stream_index = _videoStream->index;
                    // write the compressed frame to the media file
                    _lastPacket = DateTime.UtcNow;
                    ret = ffmpeg.av_interleaved_write_frame(_formatContext, &packet);
                }
            }
        }

        ffmpeg.av_packet_unref(&packet);
        _frameWritten.Set();
    }

    private void Flush()
    {
        if (_opened)
        {
            if (_videoStream != null && _videoCodecContext != null)
            {
                _lastPacket = DateTime.UtcNow;
                var packet = new AVPacket();
                // ffmpeg.av_init_packet(&packet);

                var ret = ffmpeg.avcodec_send_frame(_videoCodecContext, null);
                while (ret >= 0)
                {
                    ret = ffmpeg.avcodec_receive_packet(_videoCodecContext, &packet);
                    if (ret == 0)
                    {
                        if (packet.size > 0)
                        {
                            if (packet.pts != ffmpeg.AV_NOPTS_VALUE)
                                packet.pts = ffmpeg.av_rescale_q(packet.pts, _videoCodecContext->time_base,
                                    _videoStream->time_base);
                            if (packet.dts != ffmpeg.AV_NOPTS_VALUE)
                                packet.dts = ffmpeg.av_rescale_q(packet.dts, _videoCodecContext->time_base,
                                    _videoStream->time_base);

                            packet.stream_index = _videoStream->index;
                            // write the compressed frame to the media file
                            ret = ffmpeg.av_interleaved_write_frame(_formatContext, &packet);
                        }
                    }
                }
                ffmpeg.av_packet_unref(&packet);
            }
            if (_audioStream != null && _audioCodecContext != null)
            {
                // 把缓冲区里不足一帧的剩余样本补零编码，避免尾部数据被丢弃
                FlushAudioRemainder();

                _lastPacket = DateTime.UtcNow;
                var packet = new AVPacket();
                //ffmpeg.av_init_packet(&packet);

                var ret = ffmpeg.avcodec_send_frame(_audioCodecContext, null);
                while (ret >= 0)
                {
                    ret = ffmpeg.avcodec_receive_packet(_audioCodecContext, &packet);
                    if (ret == 0)
                    {
                        if (packet.size > 0)
                        {
                            if (packet.pts != ffmpeg.AV_NOPTS_VALUE)
                                packet.pts = ffmpeg.av_rescale_q(packet.pts, _audioCodecContext->time_base,
                                    _audioStream->time_base);
                            if (packet.dts != ffmpeg.AV_NOPTS_VALUE)
                                packet.dts = ffmpeg.av_rescale_q(packet.dts, _audioCodecContext->time_base,
                                    _audioStream->time_base);

                            packet.stream_index = _audioStream->index;
                            // write the compressed frame to the media file
                            ret = ffmpeg.av_interleaved_write_frame(_formatContext, &packet);
                        }
                    }
                }
                ffmpeg.av_packet_unref(&packet);
            }
        }
    }

    /// <summary>
    /// 取一个编码帧包含的采样数（MP3 为 1152）。
    /// </summary>
    private int GetAudioFrameSize()
    {
        int size = _audioCodecContext != null ? _audioCodecContext->frame_size : 0;
        return size > 0 ? size : 1024;
    }

    /// <summary>
    /// 保证内部音频缓冲能容纳指定字节数，不足时按 2 倍扩容。
    /// </summary>
    private void EnsureAudioBuffer(int required)
    {
        if (required <= _audioBuffer.Length) return;

        int size = _audioBuffer.Length;
        while (size < required) size *= 2;

        Array.Resize(ref _audioBuffer, size);
    }

    /// <summary>
    /// 把输入缓冲中凑满一帧的数据用 swr 转成编码器格式并写入。
    /// 输入固定为交错立体声 float（FLT），输出为编码器的平面格式（MP3 为 S32P）。
    /// </summary>
    private void AddAudioSamples(byte[] soundBuffer, int soundBufferSize, long offsetByHz)
    {
        if (_audioStream == null || _audioCodecContext == null || _ignoreAudio || soundBufferSize <= 0)
            return;

        int frameSize = GetAudioFrameSize();
        const int inChannels = 2;
        int srcSize = frameSize * inChannels * sizeof(float);

        EnsureAudioBuffer(_audioBufferSizeCurrent + soundBufferSize);

        Buffer.BlockCopy(soundBuffer, 0, _audioBuffer, _audioBufferSizeCurrent, soundBufferSize);
        _audioBufferSizeCurrent += soundBufferSize;

        int remaining = _audioBufferSizeCurrent;
        int cursor = 0;

        int outChannels = _audioCodecContext->ch_layout.nb_channels;
        int bytesPerSample = ffmpeg.av_get_bytes_per_sample(_audioCodecContext->sample_fmt);
        int outPlaneSize = frameSize * bytesPerSample;

        long pts = offsetByHz >= 0 ? offsetByHz : _lastAudioPts;

        fixed (byte* p = _audioBuffer)
        {
            byte* inPtr = p;

            while (remaining >= srcSize)
            {
                byte* outBase = (byte*)_convHandle.AddrOfPinnedObject().ToPointer();

                // 交错输入只需要一个指针；平面输出每个声道一个指针
                byte*[] inPlanes = { inPtr };
                var outPlanes = new byte*[outChannels];
                for (int c = 0; c < outChannels; c++)
                {
                    outPlanes[c] = outBase + c * outPlaneSize;
                }

                int converted;
                fixed (byte** pin = inPlanes)
                fixed (byte** pout = outPlanes)
                {
                    converted = ffmpeg.swr_convert(_swrContext, pout, frameSize, pin, frameSize);
                }

                if (converted <= 0)
                {
                    if (converted < 0) _ignoreAudio = true;
                    break;
                }

                int produced = converted;
                if (produced < frameSize)
                {
                    // MP3 等编码器要求固定 frame_size，不足一帧的部分补零
                    int gap = (frameSize - produced) * bytesPerSample;
                    for (int c = 0; c < outChannels; c++)
                    {
                        Array.Clear(_convOut, c * outPlaneSize + produced * bytesPerSample, gap);
                    }
                    produced = frameSize;
                }

                ffmpeg.av_frame_unref(_audioFrame);
                _audioFrame->nb_samples = produced;
                _audioFrame->format = (int)_audioCodecContext->sample_fmt;
                _audioFrame->sample_rate = _audioCodecContext->sample_rate;
                ffmpeg.av_channel_layout_default(&_audioFrame->ch_layout, outChannels);
                _audioFrame->pts = pts;

                // align 使用整帧平面大小，保证平面偏移与 outPlanes 一致
                int dstSamplesSize = ffmpeg.av_samples_get_buffer_size(null, outChannels, produced,
                    _audioCodecContext->sample_fmt, outPlaneSize);

                if (Log("FILL_AUDIO",
                    ffmpeg.avcodec_fill_audio_frame(_audioFrame, outChannels,
                        _audioCodecContext->sample_fmt, outBase, dstSamplesSize, outPlaneSize)))
                {
                    _ignoreAudio = true;
                    _audioBufferSizeCurrent = 0;
                    return;
                }

                if (EncodeAndWriteAudioFrame() == false)
                {
                    _ignoreAudio = true;
                    _audioBufferSizeCurrent = 0;
                    return;
                }

                inPtr += srcSize;
                cursor += srcSize;
                remaining -= srcSize;

                pts += produced;
                _lastAudioPts = pts;
            }
        }

        if (cursor > 0)
        {
            Buffer.BlockCopy(_audioBuffer, cursor, _audioBuffer, 0, remaining);
        }
        _audioBufferSizeCurrent = remaining;
    }

    /// <summary>
    /// 把当前音频帧送入编码器，并写出所有产出的数据包。
    /// </summary>
    private bool EncodeAndWriteAudioFrame()
    {
        var packet = new AVPacket();
        var ret = ffmpeg.avcodec_send_frame(_audioCodecContext, _audioFrame);

        while (ret >= 0)
        {
            ret = ffmpeg.avcodec_receive_packet(_audioCodecContext, &packet);
            if (ret != 0) break;

            if (packet.pts != ffmpeg.AV_NOPTS_VALUE)
            {
                packet.pts = ffmpeg.av_rescale_q(packet.pts,
                    _audioCodecContext->time_base, _audioStream->time_base);
            }

            if (packet.dts != ffmpeg.AV_NOPTS_VALUE)
            {
                packet.dts = ffmpeg.av_rescale_q(packet.dts,
                    _audioCodecContext->time_base, _audioStream->time_base);
            }

            packet.stream_index = _audioStream->index;
            packet.flags |= ffmpeg.AV_PKT_FLAG_KEY;
            _lastPacket = DateTime.UtcNow;

            if (Log("WRITE_AUDIO_FRAME", ffmpeg.av_interleaved_write_frame(_formatContext, &packet)))
            {
                ffmpeg.av_packet_unref(&packet);
                return false;
            }
        }

        ffmpeg.av_packet_unref(&packet);
        return true;
    }

    /// <summary>
    /// 关闭前把缓冲中不足一帧的剩余样本补零编码，避免音频尾部丢失。
    /// </summary>
    private void FlushAudioRemainder()
    {
        if (_audioCodecContext == null || _audioBufferSizeCurrent <= 0) return;

        int frameSize = GetAudioFrameSize();
        int srcSize = frameSize * 2 * sizeof(float);

        var padded = new byte[srcSize];
        Buffer.BlockCopy(_audioBuffer, 0, padded, 0, Math.Min(_audioBufferSizeCurrent, srcSize));
        _audioBufferSizeCurrent = 0;

        AddAudioSamples(padded, srcSize, -1);
    }

    private unsafe void OpenVideo()
    {
        _maxLevel = -1;

        ffmpeg.avcodec_parameters_from_context(_videoStream->codecpar, _videoCodecContext);

        _videoFrame = ffmpeg.av_frame_alloc();

        if (_videoFrame == null)
            throw new Exception("Cannot allocate video frame.");

        _videoFrame->width = _videoCodecContext->width;
        _videoFrame->height = _videoCodecContext->height;
        _videoFrame->format = (int)_videoCodecContext->pix_fmt;

        fixed(AVFrame** pFrame = &_videoFrame)
        {
            if (ffmpeg.av_frame_get_buffer(_videoFrame, 32) < 0)
            {
                ffmpeg.av_frame_free(pFrame);
                _videoFrame = null;
                throw new Exception("Cannot allocate video picture.");
            }
        }
    }

    private void GetVideoCodec(AVCodecID baseCodec)
    {
        if (baseCodec == AVCodecID.AV_CODEC_ID_H264)
        {
            if (Hwnvidia && MediaConfig.GPU_nVidia)
            {
                _avPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
                _videoCodec = ffmpeg.avcodec_find_encoder_by_name("nvenc_h264");
                if (_videoCodec != null)
                {
                    if (TryOpenVideoCodec(baseCodec))
                    {
                        Logger.LogMessage("using Nvidia hardware encoder");
                        return;
                    }
                }
            }

            if (_hwqsv && MediaConfig.GPU_QuickSync)
            {
                _avPixelFormat = AVPixelFormat.AV_PIX_FMT_NV12;
                _videoCodec = ffmpeg.avcodec_find_encoder_by_name("h264_qsv");
                if (_videoCodec != null)
                {
                    if (TryOpenVideoCodec(baseCodec))
                    {
                        Logger.LogMessage("using Intel QSV hardware encoder");
                        return;
                    }
                    Logger.LogMessage("Install Intel Media Server Studio and restart iSpy to use QSV");
                    _hwqsv = false;
                }
            }
        }

        _avPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
        _videoCodec = ffmpeg.avcodec_find_encoder(baseCodec);

        if (TryOpenVideoCodec(baseCodec))
        {
            Logger.LogMessage("using software encoder");
            return;
        }

        Logger.LogMessage("could not open any encoder codec");
        throw new Exception("Failed opening any codec");
    }

    private bool TryOpenVideoCodec(AVCodecID baseCodec)
    {
        _videoCodecContext = ffmpeg.avcodec_alloc_context3(_videoCodec);

        _videoCodecContext->codec_id = baseCodec;
        _videoCodecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;

        _videoCodecContext->width = _width;
        _videoCodecContext->height = _height;

        if (_isConstantFramerate)
        {
            if(_framerate == (int)(_framerate))
            {
                _videoCodecContext->time_base.num = 1;
                _videoCodecContext->time_base.den = (int)_framerate;
            }
            else
            {
                _videoCodecContext->time_base.num = 1000000;
                _videoCodecContext->time_base.den = (int)(_framerate*1000000);
            }
        }
        else
        {
            _videoCodecContext->time_base.num = 1;
            switch (baseCodec)
            {
                default:
                    _videoCodecContext->time_base.den = 1000;
                    break;
            }
        }

        _videoCodecContext->pix_fmt = _avPixelFormat;

        //ffmpeg.av_opt_set(_videoCodecContext->priv_data, "tune", "zerolatency", 0);

        switch (_videoCodecContext->codec_id)
        {
            case AVCodecID.AV_CODEC_ID_MPEG1VIDEO:
                _videoCodecContext->bit_rate = 700000;
                /* frames per second */
                //_videoCodecContext->gop_size = 12; /* emit one intra frame every ten frames */
                _videoCodecContext->max_b_frames = 0;
                //ffmpeg.av_opt_set(_videoCodecContext->priv_data, "crf", "30.0", 0);
                //ffmpeg.av_opt_set(_videoCodecContext->priv_data, "preset", "slow", 0);
                ffmpeg.av_opt_set(_videoCodecContext->priv_data, "pkt_size", "1316", 0);
                break;
            case AVCodecID.AV_CODEC_ID_H264:
                ffmpeg.av_opt_set(_videoCodecContext->priv_data, "profile", "main", 0);
                ffmpeg.av_opt_set(_videoCodecContext->priv_data, "preset", "veryfast", 0);
                ffmpeg.av_opt_set(_videoCodecContext->priv_data, "tune", "zerolatency", 0);
                //_videoCodecContext->qmin = 16;
                //_videoCodecContext->qmax = 26;

                //_videoCodecContext->coder_type = ffmpeg.FF_CODER_TYPE_AC;
                //_videoCodecContext->flags |= ffmpeg.CODEC_FLAG_LOOP_FILTER;
                //_videoCodecContext->scenechange_threshold = 40;
                //_videoCodecContext->gop_size = 40;
                //_videoCodecContext->max_b_frames = 0;
                //_videoCodecContext->max_qdiff = 4;
                //_videoCodecContext->me_method = 7;
                //_videoCodecContext->me_range = 16;
                //_videoCodecContext->me_cmp |= 1;
                //_videoCodecContext->me_subpel_quality = 6;
                //_videoCodecContext->qmin = 10;
                //_videoCodecContext->qmax = 51;
                //_videoCodecContext->qcompress = 0.6f;
                //_videoCodecContext->keyint_min = 2;
                //_videoCodecContext->trellis = 0;
                //_videoCodecContext->level = 13;
                //_videoCodecContext->refs = 1;
                //ffmpeg.av_opt_set(_videoCodecContext->priv_data, "tune", "zerolatency", 0);


                break;
            case AVCodecID.AV_CODEC_ID_HEVC:
                ffmpeg.av_opt_set(_videoCodecContext->priv_data, "x265-params", "qp=20", 0);
                ffmpeg.av_opt_set(_videoCodecContext->priv_data, "preset", "slow", 0);
                _videoCodecContext->qmin = 16;
                _videoCodecContext->qmax = 26;
                _videoCodecContext->max_qdiff = 4;
                //_videoCodecContext->sample_aspect_ratio.num = _width;
                //_videoCodecContext->sample_aspect_ratio.den = _height;
                break;
            default:
                //_videoCodecContext->bit_rate = _bitRate;
                break;
        }

        if ((_formatContext->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) == ffmpeg.AVFMT_GLOBALHEADER)
        {
            _videoCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER; 
        }

        int cdc;
        try
        {
            cdc = ffmpeg.avcodec_open2(_videoCodecContext, _videoCodec, null);
        }
        finally
        {
        }
        if (cdc >= 0)
            return true;

        fixed (AVCodecContext** ctx = &_videoCodecContext)
        {
            ffmpeg.avcodec_free_context(ctx);
        }
        _videoCodecContext = null;
        return false;
    }

    private void AddVideoStream(AVCodecID codecId)
    {
        GetVideoCodec(codecId);

        _videoStream = ffmpeg.avformat_new_stream(_formatContext, null);
        if (_videoStream == null)
        {
            throw new Exception("Failed creating new video stream.");
        }

        _videoStream->time_base.num = _videoCodecContext->time_base.num;
        _videoStream->time_base.den = _videoCodecContext->time_base.den;
    }

    private void OpenAudio()
    {
        var codec = ffmpeg.avcodec_find_encoder(_audioCodecContext->codec_id);

        if (codec == null)
        {
            throw new Exception("Cannot find audio codec.");
        }

        ffmpeg.av_opt_set(_audioCodecContext->priv_data, "tune", "zerolatency", 0);

        var ret = ffmpeg.avcodec_open2(_audioCodecContext, codec, null);

        if (ret < 0)
        {
            throw new Exception("Cannot open audio codec.");
        }

        ffmpeg.avcodec_parameters_from_context(_audioStream->codecpar, _audioCodecContext);

        _audioFrame = ffmpeg.av_frame_alloc();
    }

    /// <summary>
    /// 创建音频流。
    /// 输入统一按「交错立体声 float（FLT）」处理，由 swr 转换为编码器所需格式。
    /// </summary>
    /// <param name="audioCodec">音频编码器。</param>
    /// <param name="sampleRate">采样率（Hz），需为编码器支持的值。</param>
    /// <param name="channels">声道数，通常为 2。</param>
    private void AddAudioStream(AVCodecID audioCodec, int sampleRate = 44100, int channels = 2)
    {
        var codec = ffmpeg.avcodec_find_encoder(audioCodec);
        if (codec == null)
        {
            throw new Exception("Cannot find audio codec.");
        }

        _audioStream = ffmpeg.avformat_new_stream(_formatContext, null);

        if (_audioStream == null)
        {
            throw new Exception("Failed creating new audio stream.");
        }

        _audioCodecContext = ffmpeg.avcodec_alloc_context3(codec);

        _audioCodecContext->codec_id = audioCodec;
        _audioCodecContext->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;

        _audioCodecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP; //AV_SAMPLE_FMT_S32P;

        if (audioCodec == AVCodecID.AV_CODEC_ID_MP3)
            _audioCodecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_S32P;

        _audioCodecContext->sample_rate = sampleRate;

        AudioSampleRate = sampleRate;
        AudioChannels = channels;

        // 输出声道布局（立体声）
        ffmpeg.av_channel_layout_uninit(&_audioCodecContext->ch_layout);
        ffmpeg.av_channel_layout_default(&_audioCodecContext->ch_layout, channels);

        _audioStream->time_base.num = _audioCodecContext->time_base.num = 1;
        _audioStream->time_base.den = _audioCodecContext->time_base.den = sampleRate;
        _audioCodecContext->bits_per_raw_sample = 32;

        if ((_formatContext->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) == ffmpeg.AVFMT_GLOBALHEADER)
        {
            _audioCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        if ((codec->capabilities & ffmpeg.AV_CODEC_CAP_EXPERIMENTAL) != 0)
        {
            _audioCodecContext->strict_std_compliance = -2;
        }

        // 输入固定为交错立体声 float；输出为编码器的平面格式（MP3 为 S32P）
        AVChannelLayout inLayout = default;
        ffmpeg.av_channel_layout_default(&inLayout, 2);

        fixed(SwrContext** pCxt = &_swrContext)
        {
            ffmpeg.swr_alloc_set_opts2(
                pCxt,
                &_audioCodecContext->ch_layout,
                _audioCodecContext->sample_fmt,
                sampleRate,
                &inLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                sampleRate,
                0,
                null);
        }

        ffmpeg.av_channel_layout_uninit(&inLayout);

        Throw("SWR_INIT", ffmpeg.swr_init(_swrContext));

        _convHandle = GCHandle.Alloc(_convOut, GCHandleType.Pinned);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            Close();
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~MediaWriter()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
