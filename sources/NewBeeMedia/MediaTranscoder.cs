namespace NewBeeMedia;

public unsafe class MediaTranscoder
{
    public struct StreamContext
    {
        public AVCodecContext* dec_ctx;
        public AVCodecContext* enc_ctx;
    }

    double _progressing = 0;

    AVFormatContext* ifmt_ctx = null;
    AVFormatContext* ofmt_ctx = null;
    StreamContext* stream_ctx = null;

    public int OpenInputFile(String filename)
    {
        int ret;
        uint i;

        AVFormatContext* ifmt_ctx = null;
        if ((ret = ffmpeg.avformat_open_input(&ifmt_ctx, filename, null, null)) < 0)
        {
            ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Cannot open input file\n");
            return ret;
        }

        this.ifmt_ctx = ifmt_ctx;

        if ((ret = ffmpeg.avformat_find_stream_info(ifmt_ctx, null)) < 0)
        {
            ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Cannot find stream information\n");
            return ret;
        }

        stream_ctx = (StreamContext*)ffmpeg.av_mallocz_array(ifmt_ctx->nb_streams, (ulong)sizeof(StreamContext));
        if (stream_ctx == null)
            return ffmpeg.AVERROR(ffmpeg.ENOMEM);

        for (i = 0; i < ifmt_ctx->nb_streams; i++)
        {
            AVStream* stream = ifmt_ctx->streams[i];

            if (stream->codecpar->codec_id == AVCodecID.AV_CODEC_ID_NONE) continue;

            AVCodec* dec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
            AVCodecContext* codec_ctx;
            if (dec == null)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Failed to find decoder for stream #{i}\n");
                return ffmpeg.AVERROR_DECODER_NOT_FOUND;
            }
            codec_ctx = ffmpeg.avcodec_alloc_context3(dec);
            if (codec_ctx == null)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Failed to allocate the decoder context for stream #{i}\n");
                return ffmpeg.AVERROR(ffmpeg.ENOMEM);
            }
            ret = ffmpeg.avcodec_parameters_to_context(codec_ctx, stream->codecpar);
            if (ret < 0)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Failed to copy decoder parameters to input decoder context for stream #{i}\n");
                return ret;
            }
            /* Reencode video & audio and remux subtitles etc. */
            if (codec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO
                    || codec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                if (codec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                    codec_ctx->framerate = ffmpeg.av_guess_frame_rate(ifmt_ctx, stream, null);
                /* Open decoder */
                ret = ffmpeg.avcodec_open2(codec_ctx, dec, null);
                if (ret < 0)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Failed to open decoder for stream #{i}\n");
                    return ret;
                }
            }
            stream_ctx[i].dec_ctx = codec_ctx;
        }

        ffmpeg.av_dump_format(ifmt_ctx, 0, filename, 0);
        return 0;
    }

    public int OpenOutputFile(String filename)
    {
        AVStream* out_stream;
        AVStream* in_stream;
        AVCodecContext* dec_ctx;
        AVCodecContext* enc_ctx;
        AVCodec* encoder;
        int ret;
        uint i;

        AVFormatContext* ofmt_ctx = null;
        ffmpeg.avformat_alloc_output_context2(&ofmt_ctx, null, null, filename);
        if (ofmt_ctx == null)
        {
            ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Could not create output context\n");
            return ffmpeg.AVERROR_UNKNOWN;
        }

        this.ofmt_ctx = ofmt_ctx;
        for (i = 0; i < ifmt_ctx->nb_streams; i++)
        {
            in_stream = ifmt_ctx->streams[i];
            dec_ctx = stream_ctx[i].dec_ctx;
            if (dec_ctx == null) continue;

            out_stream = ffmpeg.avformat_new_stream(ofmt_ctx, null);
            if (out_stream == null)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Failed allocating output stream\n");
                return ffmpeg.AVERROR_UNKNOWN;
            }

            if (dec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO
                    || dec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                /* in this example, we choose transcoding to same codec */
                encoder = ffmpeg.avcodec_find_encoder(dec_ctx->codec_id);
                if (encoder == null)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_FATAL, "Necessary encoder not found\n");
                    return ffmpeg.AVERROR_INVALIDDATA;
                }
                enc_ctx = ffmpeg.avcodec_alloc_context3(encoder);
                if (enc_ctx == null)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_FATAL, "Failed to allocate the encoder context\n");
                    return ffmpeg.AVERROR(ffmpeg.ENOMEM);
                }

                /* In this example, we transcode to same properties (picture size,
                * sample rate etc.). These properties can be changed for output
                * streams easily using filters */
                if (dec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    enc_ctx->height = dec_ctx->height;
                    enc_ctx->width = dec_ctx->width;
                    enc_ctx->sample_aspect_ratio = dec_ctx->sample_aspect_ratio;
                    /* take first format from list of supported formats */
                    if (encoder->pix_fmts != null)
                        enc_ctx->pix_fmt = encoder->pix_fmts[0];
                    else
                        enc_ctx->pix_fmt = dec_ctx->pix_fmt;
                    /* video time_base can be set to whatever is handy and supported by encoder */
                    enc_ctx->time_base = ffmpeg.av_inv_q(dec_ctx->framerate);
                }
                else
                {
                    enc_ctx->sample_rate = dec_ctx->sample_rate;
                    enc_ctx->channel_layout = dec_ctx->channel_layout;
                    enc_ctx->channels = ffmpeg.av_get_channel_layout_nb_channels(enc_ctx->channel_layout);
                    /* take first format from list of supported formats */
                    enc_ctx->sample_fmt = encoder->sample_fmts[0];
                    enc_ctx->time_base = new AVRational { num = 1, den = enc_ctx->sample_rate };
                }

                if ((ofmt_ctx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    enc_ctx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;

                /* Third parameter can be used to pass settings to encoder */
                ret = ffmpeg.avcodec_open2(enc_ctx, encoder, null);
                if (ret < 0)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Cannot open video encoder for stream #{i}\n");
                    return ret;
                }
                ret = ffmpeg.avcodec_parameters_from_context(out_stream->codecpar, enc_ctx);
                if (ret < 0)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Failed to copy encoder parameters to output stream #{i}\n");
                    return ret;
                }

                out_stream->time_base = enc_ctx->time_base;
                stream_ctx[i].enc_ctx = enc_ctx;
            }
            else if (dec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_UNKNOWN)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_FATAL, $"Elementary stream #{i} is of unknown type, cannot proceed\n");
                return ffmpeg.AVERROR_INVALIDDATA;
            }
            else
            {
                /* if this stream must be remuxed */
                ret = ffmpeg.avcodec_parameters_copy(out_stream->codecpar, in_stream->codecpar);
                if (ret < 0)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Copying parameters for stream #{i} failed\n");
                    return ret;
                }
                out_stream->time_base = in_stream->time_base;
            }

        }
        ffmpeg.av_dump_format(ofmt_ctx, 0, filename, 1);

        if ((ofmt_ctx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
        {
            ret = ffmpeg.avio_open(&ofmt_ctx->pb, filename, ffmpeg.AVIO_FLAG_WRITE);
            if (ret < 0)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Could not open output file '{filename}'");
                return ret;
            }
        }

        /* init muxer, write output file header */
        ret = ffmpeg.avformat_write_header(ofmt_ctx, null);
        if (ret < 0)
        {
            ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Error occurred when opening output file\n");
            return ret;
        }

        return 0;
    }

    public int Transcode(String inputFilePath, String outputFilePath, Action<ImageBgr24, double> onFrameImage = null, Action<double> onProgressing = null)
    {
        _progressing = 0;
        int ret;
        AVPacket packet = new AVPacket { data = null, size = 0 };
        AVFrame* frame = null;
        AVMediaType type;
        int stream_index;
        uint i;
        int got_frame;

        if ((ret = OpenInputFile(inputFilePath)) < 0)
            goto end;
        if ((ret = OpenOutputFile(outputFilePath)) < 0)
            goto end;

        /* read all packets */
        while (true)
        {
            if ((ret = ffmpeg.av_read_frame(ifmt_ctx, &packet)) < 0)
                break;
            stream_index = packet.stream_index;
            type = ifmt_ctx->streams[packet.stream_index]->codecpar->codec_type;
            ffmpeg.av_log(null, ffmpeg.AV_LOG_DEBUG, $"Demuxer gave frame of stream_index {stream_index}\n");

            if (type == AVMediaType.AVMEDIA_TYPE_VIDEO)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_DEBUG, "Going to reencode&filter the frame\n");
                frame = ffmpeg.av_frame_alloc();
                if (frame == null)
                {
                    ret = ffmpeg.AVERROR(ffmpeg.ENOMEM);
                    break;
                }

                var tbase = ifmt_ctx->streams[stream_index]->time_base;
                Console.WriteLine($"input pts:{packet.pts * tbase.num / (double)tbase.den}");

                tbase = stream_ctx[stream_index].dec_ctx->time_base;

                long pts1 = packet.pts;

                ffmpeg.av_packet_rescale_ts(&packet,
                                    ifmt_ctx->streams[stream_index]->time_base,
                                    stream_ctx[stream_index].dec_ctx->time_base);

                long pts2 = packet.pts;

                while (true)
                {
                    ret = ffmpeg.avcodec_decode_video2(stream_ctx[stream_index].dec_ctx, frame, &got_frame, &packet);
                    if (ret < 0 || got_frame != 0) break;
                }

                if (ret < 0)
                {
                    ffmpeg.av_frame_free(&frame);
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Decoding failed\n");
                    break;
                }

                // AVRational inStreamTimeBase = ifmt_ctx->streams[stream_index]->time_base;
                // AVRational outStreamTimeBase = ofmt_ctx->streams[stream_index]->time_base;
                // AVRational inStreamTimeBase2 = stream_ctx[stream_index].dec_ctx->time_base;
                // AVRational outStreamTimeBase2 = stream_ctx[stream_index].enc_ctx->time_base;
                // Console.WriteLine($"InTimeBase: {inStreamTimeBase2.num / (double)inStreamTimeBase2.den}");
                // Console.WriteLine($"OutTimeBase: {outStreamTimeBase2.num / (double)outStreamTimeBase2.den}");

                if (got_frame != 0)
                {
                    double duration = (double)(ifmt_ctx->duration / (double)ffmpeg.AV_TIME_BASE);
                    double current = frame->pts * tbase.num / (double)tbase.den;

                    _progressing = Math.Max(_progressing, current / duration);
                    _progressing = Math.Min(_progressing, 1);
                    if (onProgressing != null) onProgressing(_progressing);

                    using (ImageBgr24 imgFrame = ReadFrame(frame))
                    {
                        if (onFrameImage != null)
                            onFrameImage(imgFrame, current);

                        AVFrame* pNewFrame = ffmpeg.av_frame_alloc();
                        if (ffmpeg.avpicture_alloc((AVPicture*)pNewFrame, (AVPixelFormat)frame->format, frame->width,
                                frame->height) < 0)
                        {
                            ffmpeg.avpicture_free((AVPicture*)pNewFrame);
                            throw new Exception("Cannot allocate video picture.");
                        }

                        pNewFrame->width = frame->width;
                        pNewFrame->height = frame->height;
                        pNewFrame->format = (int)frame->format;
                        pNewFrame->pts = frame->pts;

                        WriteFrame(imgFrame, pNewFrame);

                        ret = WriteFrame(pNewFrame, (uint)stream_index);
                        ffmpeg.av_frame_free(&pNewFrame);
                    }

                    ffmpeg.av_frame_free(&frame);
                    if (ret < 0)
                        goto end;
                }
                else
                {
                    ffmpeg.av_frame_free(&frame);
                }
            }
            else
            {
                if (stream_ctx[stream_index].dec_ctx != null)
                {
                    /* remux this frame without reencoding */
                    ffmpeg.av_packet_rescale_ts(&packet,
                                        ifmt_ctx->streams[stream_index]->time_base,
                                        ofmt_ctx->streams[stream_index]->time_base);

                    ret = ffmpeg.av_interleaved_write_frame(ofmt_ctx, &packet);
                    if (ret < 0)
                        goto end;
                }
            }
            ffmpeg.av_packet_unref(&packet);
        }

        /* flush filters and encoders */
        for (i = 0; i < ofmt_ctx->nb_streams; i++)
        {
            /* flush encoder */
            ret = FlushEncoder(i);
            if (ret < 0)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Flushing encoder failed\n");
                goto end;
            }
        }

        ffmpeg.av_write_trailer(ofmt_ctx);
    end:
        ffmpeg.av_packet_unref(&packet);
        ffmpeg.av_frame_free(&frame);
        for (i = 0; i < ifmt_ctx->nb_streams; i++)
        {
            ffmpeg.avcodec_free_context(&stream_ctx[i].dec_ctx);
            if (ofmt_ctx != null && ofmt_ctx->nb_streams > i && ofmt_ctx->streams[i] != null && stream_ctx[i].enc_ctx != null)
                ffmpeg.avcodec_free_context(&stream_ctx[i].enc_ctx);
        }

        ffmpeg.av_free(stream_ctx);

        AVFormatContext* ifmt = this.ifmt_ctx;
        ffmpeg.avformat_close_input(&ifmt);

        if (ofmt_ctx != null && 0 == (ofmt_ctx->oformat->flags & ffmpeg.AVFMT_NOFILE))
            ffmpeg.avio_closep(&ofmt_ctx->pb);
        ffmpeg.avformat_free_context(ofmt_ctx);

        _progressing = 1;
        if (onProgressing != null) onProgressing(_progressing);

        if (ret < 0)
            ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, $"Error occurred: {ret}\n");

        return ret != 0 ? 1 : 0;
    }

    int FlushEncoder(uint stream_index)
    {
        int ret;
        int got_frame;

        if (0 == (stream_ctx[stream_index].enc_ctx->codec->capabilities &
                    ffmpeg.AV_CODEC_CAP_DELAY))
            return 0;

        while (true)
        {
            ffmpeg.av_log(null, ffmpeg.AV_LOG_INFO, $"Flushing stream #{stream_index} encoder\n");
            ret = WriteFrame(null, stream_index, &got_frame);
            if (ret < 0)
                break;
            if (got_frame == 0)
                return 0;
        }
        return ret;
    }

    int WriteFrame(AVFrame* filt_frame, uint stream_index)
    {
        int get_frame = 0;
        while(get_frame == 0)
        {
            int ret = WriteFrame(filt_frame, stream_index, &get_frame);
            if (ret < 0) return ret;
        }
        return 0;
    }

    int WriteFrame(AVFrame* frame, uint stream_index, int* got_frame)
    {
        int ret;
        int got_frame_local;
        AVPacket enc_pkt;

        if (got_frame == null)
            got_frame = &got_frame_local;

        // ffmpeg.av_log(null, ffmpeg.AV_LOG_INFO, "Encoding frame\n");
        /* encode filtered frame */
        enc_pkt.data = null;
        enc_pkt.size = 0;
        ffmpeg.av_init_packet(&enc_pkt);
        while (true)
        {
            if (ifmt_ctx->streams[stream_index]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                ret = ffmpeg.avcodec_encode_video2(stream_ctx[stream_index].enc_ctx, &enc_pkt,
                    frame, got_frame);
            else
                ret = ffmpeg.avcodec_encode_audio2(stream_ctx[stream_index].enc_ctx, &enc_pkt,
                    frame, got_frame);

            if (ret <= 0 || *got_frame != 0) break;
        }

        if (ret < 0)
            return ret;
        if (0 == (*got_frame))
            return 0;

        /* prepare packet for muxing */
        enc_pkt.stream_index = (int)stream_index;

        var tbase = ofmt_ctx->streams[stream_index]->time_base;
        var enc_base = stream_ctx[stream_index].enc_ctx->time_base;
        var dec_base = stream_ctx[stream_index].dec_ctx->time_base;
        var sbase = new AVRational { den = tbase.den * enc_base.den * dec_base.num, num = tbase.num * enc_base.num * dec_base.den };

        // long pts0 = filt_frame->pts;
        long pts1 = enc_pkt.pts;

        ffmpeg.av_packet_rescale_ts(&enc_pkt,
                            dec_base,
                            ofmt_ctx->streams[stream_index]->time_base);

        long pts2 = enc_pkt.pts;


        ffmpeg.av_log(null, ffmpeg.AV_LOG_DEBUG, "Muxing frame\n");

        //Console.WriteLine($"out pts:{enc_pkt.pts * tbase.num / (double)tbase.den}");

        /* mux encoded frame */
        ret = ffmpeg.av_interleaved_write_frame(ofmt_ctx, &enc_pkt);
        return ret;
    }

    private Byte[] m_buff;

    public unsafe ImageBgr24 ReadFrame(AVFrame* frame)
    {
        ImageBgr24 image = new ImageBgr24(frame->width, frame->height);
        ReadFrame(frame, (Byte*)image.Start, image.Width * 3, AVPixelFormat.AV_PIX_FMT_BGR24, frame->width, frame->height);
        return image;
    }

    public unsafe void WriteFrame(ImageBgr24 image, AVFrame* frame)
    {
        WriteFrame(frame, (Byte*)image.Start, image.Width * 3, AVPixelFormat.AV_PIX_FMT_BGR24, image.Width, image.Height);
    }

    public unsafe bool ReadFrame(AVFrame* frame, byte* frameData, int stride, AVPixelFormat frameFmt, int width, int height)
    {
        if (frame == null || frame->data[0] == null) return false;

        int buffStride = (width + 12) * 3;
        if (m_buff == null || m_buff.Length < buffStride * height) m_buff = new byte[buffStride * height ];

        //Console.WriteLine($"FW:{frame->width},FH:{frame->height},FS:{frame->linesize[0]},{frame->linesize[1]},{frame->linesize[2]},FMT:{(AVPixelFormat)frame->format}");
        //Console.WriteLine($"DW:{width},DH:{height},DS:{stride},FMT:{frameFmt}");

        fixed (Byte* pBuff = m_buff)
        {
            SwsContextHolder sws = GetSwsContextHolder(frame->width, frame->height, (AVPixelFormat)frame->format, width, height, frameFmt, ffmpeg.SWS_BILINEAR);
            byte*[] dstData = { pBuff };
            int[] dstLinesize = new int[ffmpeg.AV_NUM_DATA_POINTERS];
            dstLinesize[0] = buffStride;

            ffmpeg.sws_scale(sws.Context, frame->data,
               frame->linesize, 0, frame->height, dstData,
               dstLinesize);

            for(int h = 0; h < height; h++)
            {
                Span<Byte> spanBuff = new Span<byte>(pBuff + h * buffStride, width * 3);
                Span<Byte> spanDst = new Span<byte>(frameData + h * stride, width * 3);
                spanBuff.CopyTo(spanDst);
            }
        }

        return true;
    }

    public unsafe bool WriteFrame(AVFrame* frame, byte* frameData, int stride, AVPixelFormat frameFmt, int width, int height)
    {
        if (frame == null || frame->data[0] == null) return false;

        SwsContextHolder sws = GetSwsContextHolder(width, height, frameFmt, frame->width, frame->height, (AVPixelFormat)frame->format, ffmpeg.SWS_BILINEAR);

        byte*[] srcData = { frameData };
        int[] srcLinesize = { stride };

        ffmpeg.sws_scale(sws.Context,
           srcData,
           srcLinesize, 
           0, 
           height,
           frame->data,
           frame->linesize);
        return true;
    }

    private SwsContextHolder m_sws;
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
}
