using NAudio.Wave;
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
        if (frame == 0)
            Play();
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
        }
        catch (Exception ex)
        {
            // 读取失败时保留默认时长，避免中断整个导出流程
            _error = true;
            Console.WriteLine($"[NBAudioClip({Name})] 读取音频时长失败: {ex.Message}");
        }
    }
}
