using NAudio.Wave;
using NewBeeVG.Internal;
using SkiaSharp;
using System.Diagnostics;

namespace NewBeeVG;

public class NBTTSClip : NBClip
{
    private string _audioFilePath;
    private readonly string _text;
    private readonly string _voice;
    private readonly string _language;
    private readonly string _instructions;
    private readonly string _model;

    public string Text { get => _text; }

    public string Voice { get => _voice; }

    public string Instructions { get => _instructions; }

    public string Language { get => _language; }

    private byte[]? _data;

    public NBTTSClip(string text, string voice = "Cherry", string language = "Chinese", string instructions = "", string model = "qwen3-tts-instruct-flash", string name = "clip", int? start = null)
    {
        Name = name;
        ControlBuilder = null;

        _text = text ?? string.Empty;
        _voice = voice ?? "Cherry";
        _language = language ?? "Chinese";
        _instructions = instructions ?? string.Empty;
        _model = model ?? "qwen3-tts-instruct-flash";
        _instructions = _instructions.Trim();

        var key = $"{model}##{voice}##{language}##{text}##{_instructions}";
        _audioFilePath = ComputeMd5(key) + ".wav";
        DurationFrames = -1;
        StartFrame = start;
    }

    private bool _playing = false;

    public override Control? Build(NBStage stage, int frame, bool includeStageBackground)
    {
        if(frame == 0)
            Play();   
        return null;
    }

    public override SKBitmap? Render(NBStage stage, int frame, bool includeStageBackground)
    {
        return null;
    }

    public override void Prepare()
    {
        var cacheDir = GetOrCreateCacheDir();
        var audioFile = Path.Combine(cacheDir.FullName, _audioFilePath);
        if (File.Exists(audioFile) == false)
        {
            if(_model != "mlx-tts")
            {
                GenerateByQwenTTS(audioFile);
            }
            else
            {
                GenerateByMlxTTS(audioFile);
            }
        }

        if (File.Exists(audioFile))
        {
            var duration = AudioHelpers.GetWavDuration(audioFile);

            // Convert duration to frames. Adjust defaultFps to your project's frame rate.
            const double defaultFps = 25.0;
            DurationFrames = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds * defaultFps));
            _data = File.ReadAllBytes(audioFile);
            Console.WriteLine($"[NBTTSClip({Name})] load successful: {audioFile}");
        }
    }

    protected void GenerateByQwenTTS(string outputAudioFile)
    {
        // 调用 QWenTTS 类来合成，下载到 audioFile 
        try
        {
            // Use QWenTTS client to generate and download audio synchronously (blocking).
            var tts = new QWenTTS();
            var resp = tts.GenerateAsync(_text, _voice, _language, _instructions, _model).GetAwaiter().GetResult();

            if (resp?.Output?.Audio != null)
            {
                // Prefer direct binary data if provided
                if (!string.IsNullOrWhiteSpace(resp.Output.Audio.Data))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(resp.Output.Audio.Data);
                        File.WriteAllBytes(outputAudioFile, bytes);
                    }
                    catch (FormatException fe)
                    {
                        Debug.WriteLine($"NBTTSClip: audio data base64 decode failed: {fe.Message}");
                    }
                }
                // Otherwise download from audio.url
                else if (!string.IsNullOrWhiteSpace(resp.Output.Audio.Url))
                {
                    try
                    {
                        tts.DownloadAudioAsync(resp, outputAudioFile).GetAwaiter().GetResult();
                        Console.WriteLine($"[NBTTSClip({Name})] Generate audio successful: {outputAudioFile}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NBTTSClip({Name})]: audio download failed: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NBTTSClip({Name})]: TTS generation failed: {ex.Message}");
        }
    }

    protected void GenerateByMlxTTS(string outputAudioFile)
    {
        // 调用 QWenTTS 类来合成，下载到 audioFile 
        try
        {
            var server = "http://192.168.8.18:8000";
            var apiKey = "xxxxxxxx";
            var bytes = NBMlxTTS.SynthesizeFoo(server, apiKey, _text, _voice, _language, Instructions);
            if(bytes?.Length > 0)
            {
                File.WriteAllBytes(outputAudioFile, bytes);
                Console.WriteLine($"[NBTTSClip({Name})] Generate audio successful: {outputAudioFile}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NBTTSClip({Name})]: TTS generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Play the loaded audio bytes asynchronously. Caller can await.
    /// </summary>
    public Task PlayAsync(CancellationToken ct = default)
    {
        if (_data == null || _data.Length == 0)
            return Task.CompletedTask;

        var player = new AudioPlayer();
        // Player disposes itself when playback stops via OnPlaybackStopped,
        // but here we want to await completion and then dispose explicitly.
        return Task.Run(async () =>
        {
            try
            {
                await player.PlayAndWaitAsync(_data, ct).ConfigureAwait(false);
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
        if (_data == null || _data.Length == 0) return;

        // fire-and-forget: play in background and let AudioPlayer clean up
        Task.Run(() =>
        {
            using var player = new AudioPlayer();
            player.PlayFromBytes(_data);
            // wait until finished
            while (player.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        });
    }
}
