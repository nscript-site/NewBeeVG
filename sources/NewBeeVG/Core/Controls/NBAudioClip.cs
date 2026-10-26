using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NewBeeVG.Internal;
using SkiaSharp;

namespace NewBeeVG;

public class NBAudioClip : NBClip
{
    private string? _audioFilePath;
    private bool _error = false;

    public NBAudioClip(string? audioFilePath = null, int? start = null, string name = "audio")
    {
        Name = name;
        ControlBuilder = null;
        _audioFilePath = audioFilePath;
        DurationFrames = 1;
        StartFrame = start;
        ContainsAudio = true;
    }

    public override void Render(SKCanvas canvas, NBStage stage, int frame, bool includeStageBackground, bool preview)
    {
    }

    int cacheExportSampleRate = -1;
    float[]? cacheExportData = null;

    /// <summary>
    /// 导出浮点格式（float，范围 -1.0 到 1.0）的立体声 PCM。
    /// 返回交错排列的样本数组：<c>[L0, R0, L1, R1, ...]</c>。
    /// </summary>
    /// <param name="sampleRate">目标采样率（Hz），默认 44100；源采样率不同时自动重采样。</param>
    /// <returns>
    /// 交错立体声样本；文件未配置、读取失败或参数非法时返回 <c>null</c>。
    /// 单声道源会复制为左右两路；多声道源只取前两个声道。
    /// </returns>
    public override float[]? ExportStereoPcm(int sampleRate = 44100)
    {
        if (_error == true) return null;

        if (cacheExportData != null && cacheExportSampleRate == sampleRate) return cacheExportData;

        if (sampleRate <= 0)
        {
            Console.WriteLine($"[NBAudioClip({Name})] 无效的采样率: {sampleRate}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(_audioFilePath)) return null;

        var path = Path.GetFullPath(_audioFilePath);
        if (File.Exists(path) == false)
        {
            Console.WriteLine($"[NBAudioClip({Name})] 音频文件不存在: {path}");
            return null;
        }

        try
        {
            // AudioFileReader 输出 32 位浮点样本，可直接按 -1.0 ~ 1.0 读取
            using var reader = new AudioFileReader(path);

            // 源采样率与目标不一致时进行重采样
            ISampleProvider provider = reader;
            if (reader.WaveFormat.SampleRate != sampleRate)
            {
                provider = new WdlResamplingSampleProvider(reader, sampleRate);
            }

            int channels = provider.WaveFormat.Channels;
            if (channels <= 0)
            {
                Console.WriteLine($"[NBAudioClip({Name})] 无效的声道数: {channels}");
                return null;
            }

            // 按预计时长预分配，读取过程中不足时自动扩容
            long estimatedFrames = (long)Math.Ceiling(reader.TotalTime.TotalSeconds * sampleRate) + 1;
            var result = new float[Math.Max(2L, estimatedFrames * 2)];
            var buffer = new float[4096 * channels];
            int frameCount = 0;

            int read;
            while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                int frames = read / channels;
                for (int i = 0; i < frames; i++)
                {
                    int writeIndex = frameCount * 2;
                    if (writeIndex + 1 >= result.Length)
                    {
                        Array.Resize(ref result, result.Length * 2);
                    }

                    int sourceIndex = i * channels;
                    if (channels == 1)
                    {
                        // 单声道同时写入左右声道
                        float value = buffer[sourceIndex];
                        result[writeIndex] = value;
                        result[writeIndex + 1] = value;
                    }
                    else
                    {
                        // 双声道直接拷贝；多声道只取前两个声道
                        result[writeIndex] = buffer[sourceIndex];
                        result[writeIndex + 1] = buffer[sourceIndex + 1];
                    }

                    frameCount++;
                }
            }

            // 长度可能因重采样估算略有偏差，按实际帧数截断
            int length = frameCount * 2;
            if (length != result.Length)
            {
                Array.Resize(ref result, length);
            }

            cacheExportData = result;
            cacheExportSampleRate = sampleRate;

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NBAudioClip({Name})] 导出立体声 PCM 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Play the loaded audio bytes asynchronously. Caller can await.
    /// </summary>
    public Task PlayAsync(CancellationToken ct = default)
    {
        if (_error == true) return Task.CompletedTask;

        var player = new AudioPlayer();
        // Player disposes itself when playback stops via OnPlaybackStopped,
        // but here we want to await completion and then dispose explicitly.
        return Task.Run(async () =>
        {
            try
            {
                await player.PlayAndWaitAsync(_audioFilePath, ct).ConfigureAwait(false);
            }
            finally
            {
                player.Dispose();
            }
        }, ct);
    }

    /// <summary>
    /// Fire-and-forget play on thread pool (does not block).
    /// </summary>
    public void Play()
    {
        if (_error == true) return;

        // fire-and-forget: play in background and let AudioPlayer clean up
        Task.Run(() =>
        {
            using var player = new AudioPlayer();
            player.PlayFromFile(_audioFilePath);
            // wait until finished
            while (player.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        });
    }

    public override void Prepare(NBStage stage)
    {
        // 未配置音频文件时，保留构造函数传入的默认时长
        if (string.IsNullOrWhiteSpace(_audioFilePath)) return;

        var path = Path.GetFullPath(_audioFilePath);
        if (File.Exists(path) == false)
        {
            _error = true;
            Console.WriteLine($"[NBAudioClip({Name})] 音频文件不存在: {path}");
            return;
        }

        // 当前仅支持 mp3 / wav
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension != ".mp3" && extension != ".wav")
        {
            _error = true;
            Console.WriteLine($"[NBAudioClip({Name})] 不支持的音频格式: {extension}（仅支持 .mp3 / .wav）");
            return;
        }

        double fps = stage.FrameRate;
        if (fps <= 0)
        {
            _error = true;
            Console.WriteLine($"[NBAudioClip({Name})] 无效的帧率: {fps}");
            return;
        }

        try
        {
            // AudioFileReader 同时支持 mp3 与 wav，可直接读取总时长
            using var reader = new AudioFileReader(path);
            var duration = reader.TotalTime;

            // 向上取整，保证最后一个不完整的帧也被覆盖；至少保留 1 帧
            DurationFrames = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds * fps));

            Console.WriteLine($"[NBAudioClip({Name})] 音频时长 {duration.TotalSeconds:F2} 秒，对应 {DurationFrames} 帧 @ {fps}fps");

            var pcm = this.ExportStereoPcm(stage.AudioFrameRate);
            if (pcm != null)
            {
                this.MixedAudioData = new StereoPcmData() { Frame = 0, Pcm = pcm };
            }
        }
        catch (Exception ex)
        {
            // 读取失败时保留默认时长，避免中断整个导出流程
            _error = true;
            Console.WriteLine($"[NBAudioClip({Name})] 读取音频时长失败: {ex.Message}");
        }
    }
}
