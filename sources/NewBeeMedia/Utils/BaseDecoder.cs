#region LGPL License
//
// DecoderStream.cs
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

public unsafe abstract class BaseDecoder
{
    #region Fields

    protected MediaReader m_file;
    protected AVCodecContext* m_pCodecCtx;
    protected AVStream* m_avStream;
    protected AVFormatContext* m_fmtCtx;
    protected uint m_streamIdx;
    protected bool m_disposed;
    private bool m_codecOpen = false;
    private Queue<AVPacket> m_packetQueue = new Queue<AVPacket>();
    private long m_rawPts;
    private long m_rawDts;
    private int m_packetQueueCapacity = 20;

    #endregion

    #region Properties

    internal Queue<AVPacket> PacketQueue
    {
        get { return m_packetQueue; }
    }

    public TimeSpan Duration
    {
        get { return m_file.Duration; }
    }

    public int PacketQueueCapacity
    {
        get { return m_packetQueueCapacity; }
        set { m_packetQueueCapacity = value; }
    }

    public long Pts
    {
        get { return m_rawPts; }
    }

    public long Dts
    {
        get { return m_rawDts; }
    }

    public int StreamIndex
    {
        get; private set;
    }

    public Boolean CodecOpen
    {
        get { return m_codecOpen; }
    }

    protected double _timeBase;

    public double Time
    {
        get { return this.Dts * _timeBase; }
    }

    #endregion

    #region Constructors / destructor

    public BaseDecoder(MediaReader file, int idx, AVStream* stream)
    {
        StreamIndex = idx;
        // Initialize instance variables
        m_disposed = false;
        m_file = file;
        m_avStream = stream;

        m_pCodecCtx = m_avStream->codec;
        m_fmtCtx = file._fmtCxt;

        // Open the decoding codec
        AVCodec* avCodec = ffmpeg.avcodec_find_decoder(m_pCodecCtx->codec_id);
        if (avCodec == null)
            throw new DecoderException("No decoder found");

        if (ffmpeg.avcodec_open2(m_pCodecCtx, avCodec, null) < 0)
            throw new DecoderException("Error opening codec");

        m_codecOpen = true;
    }

    ~BaseDecoder()
    {
        Dispose(false);
    }

    #endregion

    #region Methods

    internal virtual void GuessTimeBase()
    {
        if (this.m_avStream->time_base.den > 0 && this.m_avStream->time_base.num > 0)
        {
            _timeBase = m_avStream->time_base.ToDouble();
        }
    }

    protected AVPacket ReadNextPacket()
    {
        while (PacketQueue.Count == 0)
        {
            m_file.EnqueueNextPacket(); ;
        }
        
        AVPacket packet = PacketQueue.Dequeue();

        m_rawDts = packet.dts;
        m_rawPts = packet.pts;

        return packet;
    }

    protected void ClearQueue()
    {
        m_packetQueue.Clear();
        m_rawDts = -1;
        m_rawPts = -1;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!m_disposed)
        {
            m_disposed = true;

            // BROKEN: Throwing exception on video codec close with MPEG4
            //                if (m_codecOpen)
            //                    FFmpeg.avcodec_close(ref m_avCodecCtx);
        }
    }

    #endregion
}
