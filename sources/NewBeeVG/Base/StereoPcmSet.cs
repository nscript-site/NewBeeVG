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
    /// 将所有音频片段按起始帧对齐后叠加为一条从第 0 帧开始的立体声 PCM。
    /// </summary>
    /// <returns>
    /// 混音结果（<see cref="StereoPcmData.Frame"/> = 0）；没有可混音数据时返回 <c>null</c>。
    /// 输出样本会逐个限幅到 [-1, 1]，避免多轨叠加后溢出。
    /// </returns>
    public StereoPcmData? Mixing()
    {
        if (IsEmpty) return null;
        if (SampleRate <= 0)
            throw new InvalidOperationException($"无效的采样率: {SampleRate}");
        if (FrameRate <= 0)
            throw new InvalidOperationException($"无效的帧率: {FrameRate}");

        // 一帧对应的采样点数（允许小数，按四舍五入对齐到最近的采样点）
        double samplesPerFrame = SampleRate / FrameRate;

        var placements = new List<(float[] Pcm, int Offset, int Frames)>(Data.Count);
        long totalFrames = 0;

        foreach (var item in Data)
        {
            // 跳过空数据或不足一个立体声帧的数据
            if (item?.Pcm == null || item.Pcm.Length < 2) continue;

            int frames = item.Pcm.Length / 2;

            // 起始帧换算为采样点；负值按 0 处理，避免写入越界
            long offset = item.Frame > 0
                ? (long)Math.Round(item.Frame * samplesPerFrame)
                : 0;

            long end = offset + frames;
            if (end > totalFrames) totalFrames = end;

            // 超出 int 可表达的偏移无法作为数组下标，直接跳过
            if (offset > int.MaxValue) continue;

            placements.Add((item.Pcm, (int)offset, frames));
        }

        if (placements.Count == 0 || totalFrames <= 0) return null;
        if (totalFrames > int.MaxValue / 2)
            throw new InvalidOperationException($"混音结果过大: {totalFrames} 采样帧");

        // 交错立体声：[L0, R0, L1, R1, ...]
        var output = new float[(int)totalFrames * 2];

        // 叠加：同一时间点的左右声道分别累加
        foreach (var (pcm, offset, frames) in placements)
        {
            int dst = offset * 2;
            for (int i = 0; i < frames; i++)
            {
                output[dst] += pcm[i * 2];
                output[dst + 1] += pcm[i * 2 + 1];
                dst += 2;
            }
        }

        // 限幅到 [-1, 1]，避免多轨叠加后削波
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = Math.Clamp(output[i], -1f, 1f);
        }

        return new StereoPcmData
        {
            Pcm = output,
            Frame = 0
        };
    }
}
