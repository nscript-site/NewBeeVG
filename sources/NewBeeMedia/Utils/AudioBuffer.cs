using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace NewBeeMedia;

public unsafe class AudioBuffer : IDisposable
{
    public Byte* Data { get; private set; }

    /// <summary>
    /// 数据长度
    /// </summary>
    public int Length;

    /// <summary>
    /// 最大容量。（为Data所指向内存的大小，超过最大容量则会发生指针越界错误）
    /// </summary>
    public readonly int Capacity;

    public AudioBuffer(int capacity = MediaConfig.AVCODEC_MAX_AUDIO_FRAME_SIZE)
    {
        Data = (Byte*)Marshal.AllocHGlobal(capacity);
        Capacity = capacity;
    }

    public void Dispose()
    {
        if (Data != null)
        {
            IntPtr p = (IntPtr)Data;
            Data = null;
            Marshal.FreeHGlobal(p);
        }
    }

    public Byte[] ToArray()
    {
        if (Data == null) return null;
        else if (Length == 0) return new Byte[] { };
        else
        {
            Byte[] data = new Byte[Length];
            Marshal.Copy((IntPtr)Data, data, 0, Length);
            return data;
        }
    }
}
