using System.Collections.ObjectModel;

namespace NewBeeMedia;

public unsafe class MediaReader : IDisposable
{
    internal AVFormatContext* _fmtCxt;
    private SortedList<int, BaseDecoder> _streams;
    private string _filename;
    private bool _disposed = false;

    #region Properties

    protected unsafe ReadOnlyCollection<BaseDecoder> Streams
    {
        get { return new ReadOnlyCollection<BaseDecoder>(_streams.Values); }
    }

    public AudioStreamDecoder AudioStream
    {
        get
        {
            foreach (var item in _streams)
            {
                if (item.Value is AudioStreamDecoder)
                {
                    return item.Value as AudioStreamDecoder;
                }
            }
            return null;
        }
    }

    public VideoStreamDecoder? VideoStream { get; private set; }

    public int FrameWidth { get; private set; }
    public int FrameHeight { get; private set; }

    public int VideoStreamCount { get { return VideoStream != null ? 1 : 0; }}

    public int AudioStreamCount { get { return AudioStream != null ? 1 : 0; } }

    public string Filename
    {
        get { return _filename; }
    }

    public string FileFormat
    {
        get { unsafe { return ((IntPtr)(_fmtCxt->iformat->name)).ToString(); } }
    }

    /// <summary>
    /// Duration of the stream
    /// </summary>
    public TimeSpan Duration
    {
        get { return new TimeSpan((long)(RawDuration * 1e7)); }
    }

    public double RawDuration
    {
        get
        {
            double duration = (double)(_fmtCxt->duration / (double)ffmpeg.AV_TIME_BASE);
            if (duration < 0)
                duration = 0;
            return duration;
        }
    }

    public long FrameCount
    {
        get { return VideoStream == null ? 0 : VideoStream.FrameCount; }
    }

    #endregion

    public MediaReader(FileInfo File) : this(File.FullName) { }

    public unsafe MediaReader(string Filename)
    {
        if (String.IsNullOrEmpty(Filename))
            throw new ArgumentNullException("Filename");

        _filename = Filename;

        _fmtCxt = ffmpeg.avformat_alloc_context();

        // Open the file with FFmpeg
        var pFormatContext = _fmtCxt;
        ffmpeg.avformat_open_input(&pFormatContext, Filename, null, null).ThrowExceptionIfError();
        ffmpeg.avformat_find_stream_info(_fmtCxt, null).ThrowExceptionIfError();
        ffmpeg.av_dump_format(_fmtCxt, -1, Filename, 0);

        if (_fmtCxt->nb_streams < 1)
            throw new DecoderException("视频文件错误：无法找到流");

        _streams = new SortedList<int, BaseDecoder>();
        for (int i = 0; i < _fmtCxt->nb_streams; i++)
        {
            AVStream* stream = _fmtCxt->streams[i];
            
            switch (stream->codec->codec_type)
            {
                case AVMediaType.AVMEDIA_TYPE_VIDEO:
                    _streams.Add(i, new VideoStreamDecoder(this, i, stream));
                    break;
                case AVMediaType.AVMEDIA_TYPE_AUDIO:
                    _streams.Add(i, new AudioStreamDecoder(this, i, stream));
                    break;
                case AVMediaType.AVMEDIA_TYPE_UNKNOWN:
                case AVMediaType.AVMEDIA_TYPE_DATA:
                case AVMediaType.AVMEDIA_TYPE_SUBTITLE:
                default:
                    _streams.Add(i, null);
                    break;
            }
        }

        foreach (var item in _streams)
        {
            if (item.Value is VideoStreamDecoder)
            {
                if(VideoStream == null)
                    VideoStream = item.Value as VideoStreamDecoder;
            }
        }

        if (this.VideoStream != null)
        {
            VideoStream.GuessTimeBase();
            FrameWidth = VideoStream.Width;
            FrameHeight = VideoStream.Height;
        }
        if (this.AudioStream != null) AudioStream.GuessTimeBase();
    }

    public ImageU8 NextFrameU8()
    {
        return FrameWidth > 0 ? VideoStream.NextFrameU8(FrameWidth, FrameHeight) : null;
    }

    public ImageBgr24 NextFrameBgr24()
    {
        return FrameWidth > 0 ? VideoStream.NextFrameBgr24(FrameWidth, FrameHeight) : null;
    }

    public ImageBgra32 NextFrameBgra32()
    {
        return FrameWidth > 0 ? VideoStream.NextFrameBgra32(FrameWidth, FrameHeight) : null;
    }

    internal void EnqueueNextPacket()
    {
        AVPacket packet = new AVPacket();
        packet.data = null;
        if (ffmpeg.av_read_frame(_fmtCxt, &packet) < 0 || packet.data == null || packet.size == 0)
            throw new System.IO.EndOfStreamException();

        BaseDecoder dest = null;
        if (_streams.TryGetValue(packet.stream_index, out dest) && dest != null)
        {
            // 如果队列满了。进行释放
            while (dest.PacketQueue.Count > 0 && dest.PacketQueue.Count >= dest.PacketQueueCapacity)
            {
                AVPacket p = dest.PacketQueue.Dequeue();
                ffmpeg.av_packet_unref(&p);
            }

            dest.PacketQueue.Enqueue(packet);
        }
        else
            ffmpeg.av_packet_unref(&packet);
    }

    //public Boolean Seek(double time, Boolean seekKeyFrame = false)
    //{
    //    VideoDecoder video = this.VideoStream;
    //    if (video != null) return video.Seek(time, seekKeyFrame);
    //    return false;
    //}

    //public Boolean SeekFile2(long bytes)
    //{
    //    VideoDecoder video = this.VideoStream;
    //    //if (video != null) return video.Seek(time, seekKeyFrame);
    //    return false;
    //}

    //public Boolean SeekFile(long bytes)
    //{
    //    long fileSize = ffmpeg.avio_size(this.FormatContext->pb);
    //    VideoDecoder video = this.VideoStream;
    //    if (video != null) return video.SeekBytes2(bytes, fileSize);
    //    return false;
    //}

    public void Close()
    {
        Dispose(true);
    }

    ~MediaReader()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;

            if (disposing)
            {
                _streams = null;
            }

            if (_fmtCxt != null)
            {
                var p = _fmtCxt;
                ffmpeg.avformat_close_input(&p);
                _fmtCxt = null;
            }
        }
    }

    public IReadOnlyDictionary<string, string> GetContextInfo()
    {
        AVDictionaryEntry* tag = null;
        var result = new Dictionary<string, string>();
        while ((tag = ffmpeg.av_dict_get(_fmtCxt->metadata, "", tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
        {
            var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
            var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
            result.Add(key, value);
        }

        return result;
    }
}
