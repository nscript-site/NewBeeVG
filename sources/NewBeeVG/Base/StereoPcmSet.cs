using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG;

internal class StereoPcmData
{
    public float[] Pcm { get; init; }
    public int Frame { get; init; }
}

internal class StereoPcmDataSet
{
    public int SampleRate = 48000;
    public double FrameRate = 25;
    public List<StereoPcmData> Data = new List<StereoPcmData>();

    public bool IsEmpty => Data.Count == 0;

    /// <summary>
    /// 进行混音
    /// </summary>
    /// <returns></returns>
    public StereoPcmData? Mixing()
    {
        throw new NotImplementedException();
    }
}
