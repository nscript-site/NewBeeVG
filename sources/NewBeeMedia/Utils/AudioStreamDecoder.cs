#region LGPL License
//
// AudioDecoderStream.cs
//
// Author:
//   Justin Cherniak (justin.cherniak@gmail.com
//
// Copyright (C) 2008 Justin Cherniak
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//
#endregion

namespace NewBeeMedia;

public unsafe class AudioStreamDecoder : BaseDecoder
{
    private byte[] m_buffer;

    internal AudioStreamDecoder(MediaReader file, int streamIdx, AVStream* stream)
        : base(file, streamIdx, stream)
    {
        m_buffer = new byte[MediaConfig.AVCODEC_MAX_AUDIO_FRAME_SIZE];
    }

    /// <summary>
    /// Number of channels in the audio stream.
    /// </summary>
    public int Channels
    {
        get { return m_pCodecCtx->channels; }
    }

    /// <summary>
    /// Sample rate of the stream in bits per second
    /// </summary>
    public int SampleRate
    {
        get { return m_pCodecCtx->sample_rate; }
    }

    /// <summary>
    /// Returns the sample size in bits.
    /// </summary>
    public int SampleSize
    {
        get
        {
            switch (m_pCodecCtx->sample_fmt)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                    return 8;
                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                    return 16;
                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                    return 32;
                default:
                    throw new Exception("Unknown sample size.");
            }
        }
    }

    /// <summary>
    /// Average bytes per second of the stream
    /// </summary>
    public int UncompressedBytesPerSecond
    {
        get { return (Channels * SampleRate * SampleSize) / 8; }
    }

    //public Boolean Seek(double time)
    //{
        
    //    if (m_disposed == true) return false;
    //    long seekTarget = (long)(time * (double)ffmpeg.AV_TIME_BASE);
    //    int flag = ffmpeg.AVSEEK_FLAG_BACKWARD | ffmpeg.AVSEEK_FLAG_FRAME;
    //    if (time <= 0) flag = ffmpeg.AVSEEK_FLAG_FRAME;
    //    int val = ffmpeg.av_seek_frame(m_file._fmtCxt, m_avStream->index, seekTarget, flag);
    //    ffmpeg.avcodec_flush_buffers(m_pCodecCtx);
    //    if (val < 0) return false;
    //    return true;
    //}

    public void ReadAudioFrame(AudioBuffer frame)
    {
        //frame.Length = 0;
        //if (m_disposed == true) return;
        //while (true)
        //{
        //    AVPacket packet = ReadNextPacket();
        //    if (packet.size > 0)
        //    {
        //        int outSize;
        //        int bytesDecoded;
        //        Int16* data = (Int16*)frame.Data;
        //        byte* packetData = (byte*)packet.data;
        //        int bytesRemaining = packet.size;
        //        while (bytesRemaining > 0)
        //        {
        //            outSize = frame.Capacity - frame.Length;
                    
        //            bytesDecoded = ffmpeg.avcodec_decode_audio4(m_pCodecCtx, data, &outSize, &packet);
        //            if (bytesDecoded < 0) break;

                    

        //            frame.Length += outSize;
        //            data += (outSize >> 1);
        //            bytesRemaining -= bytesDecoded;

        //            if (outSize > 0) break;
        //        }
        //        ffmpeg.av_free_packet(&packet);
        //    }

        //    if (frame.Length > 0) break;
        //}
    }

    /// <summary>
    /// 获取指定时间段的音频数据。
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public byte[] GetPCM16K(double start, double end)
    {
        throw new NotImplementedException();
    }
}
