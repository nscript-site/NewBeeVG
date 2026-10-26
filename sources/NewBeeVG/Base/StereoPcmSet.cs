using NewBeeMedia;
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

    /// <summary>
    /// 把混音结果写入 MediaWriter（交错立体声 float，从采样点 0 开始）。
    /// 要求 writer 的音频采样率与 <see cref="SampleRate"/> 一致、声道数为 2。
    /// </summary>
    /// <param name="writer">目标视频写入器。</param>
    public void WriteTo(MediaWriter writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        if (writer.AudioChannels != 2)
        {
            throw new ArgumentException(
                $"MediaWriter 的音频声道数为 {writer.AudioChannels}，混音数据需要立体声（2）。",
                nameof(writer));
        }

        if (writer.AudioSampleRate != SampleRate)
        {
            throw new ArgumentException(
                $"采样率不一致：MediaWriter 为 {writer.AudioSampleRate}Hz，混音数据为 {SampleRate}Hz。",
                nameof(writer));
        }

        var mixed = Mixing();

        if (mixed == null) return;

        try
        {
            writer.WriteAudio(mixed.Pcm, 0);
        }
        catch(Exception ex)
        {
            throw new InvalidOperationException($"写入音频失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将混音结果保存为 32 位浮点立体声 WAV 文件（IEEE float）。
    /// </summary>
    /// <param name="filePath">输出文件路径（建议使用 .wav 扩展名）；所在目录不存在时会自动创建。</param>
    /// <remarks>没有可混音数据时不生成文件；WAV 的采样率取自 <see cref="SampleRate"/>。</remarks>
    public void SaveWav(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath 不能为空", nameof(filePath));

        var mixed = Mixing();
        if (mixed == null) return;

        var pcm = mixed.Pcm;

        const short channels = 2;
        const short bitsPerSample = 32;          // IEEE float 每个样本 32 位
        const short blockAlign = channels * bitsPerSample / 8;
        const short formatIeeeFloat = 3;

        int dataSize = pcm.Length * sizeof(float);
        int riffSize = 36 + dataSize;

        // 确保目录存在
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        // ---------- RIFF ----------
        bw.Write((byte)'R'); bw.Write((byte)'I'); bw.Write((byte)'F'); bw.Write((byte)'F');
        bw.Write(riffSize);
        bw.Write((byte)'W'); bw.Write((byte)'A'); bw.Write((byte)'V'); bw.Write((byte)'E');

        // ---------- fmt ----------
        bw.Write((byte)'f'); bw.Write((byte)'m'); bw.Write((byte)'t'); bw.Write((byte)' ');
        bw.Write(16);                              // fmt 块大小
        bw.Write(formatIeeeFloat);                 // 3 = IEEE float
        bw.Write(channels);
        bw.Write(SampleRate);
        bw.Write(SampleRate * blockAlign);         // 每秒字节数
        bw.Write(blockAlign);
        bw.Write(bitsPerSample);

        // ---------- data ----------
        bw.Write((byte)'d'); bw.Write((byte)'a'); bw.Write((byte)'t'); bw.Write((byte)'a');
        bw.Write(dataSize);

        // BinaryWriter 按小端写入；交错 float 直接分块拷贝，避免一次性分配过大数组
        const int chunkBytes = 64 * 1024;
        var buffer = new byte[Math.Min(chunkBytes, Math.Max(dataSize, 1))];
        int offset = 0;

        while (offset < dataSize)
        {
            int count = Math.Min(buffer.Length, dataSize - offset);
            Buffer.BlockCopy(pcm, offset, buffer, 0, count);
            bw.Write(buffer, 0, count);
            offset += count;
        }
    }
}
