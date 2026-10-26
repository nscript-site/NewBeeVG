using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NAudio.Wave;
using SkiaSharp;
using System.Diagnostics;

namespace NewBeeVG.Viewer.Widgets;

public class PlayerView : BaseView
{
    public IPlayable? Playable { get; set; } = default!;
    public NBWork? Work { get; set; } = default!;

    private int Frames = 0;
    private int CurrentFrame = 0;
    private SKBitmap? Bitmap = null;
    private NBSkiaBitmapView FrameImage = default!;

    protected bool Playing { get; set; }

    protected override void Build(out Control content)
    {
        FrameImage = new NBSkiaBitmapView();
        if (Work != null)
        {
            VGrid("30,*,5,40,5", [
                HGrid("*,Auto,Auto", [
                HStack([
                    TextBlock(()=>Playable?.FullName??String.Empty),
                    TextBlock(()=>$"{Math.Min(Frames,CurrentFrame + 1)}/{Frames}"),
                ]),
                IconButton(VideoCheckOutlineIcon.Instance,"导出视频", scale: 1, iconSize:16)
                    .Align(1,1)
                    .OnClick(_ => { Export(true); }),
                IconButton( AnimationPlayOutlineIcon.Instance,"导出动画", scale: 1, iconSize:16)
                    .Align(1,1)
                    .OnClick(_ => { Export(false); }),
            ]),
            Border(FrameImage)
                .Background(Brushes.Gray)
                .BorderBrush(Brushes.LightGray).BorderThickness(1),
            null,
            HGrid("Auto,20,*",[
                HStack([
                    IconButton(SkipPreviousIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(0)),
                    IconButton(ArrowLeftBoldBoxIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(CurrentFrame - 1)),
                    IconButton(()=> Playing ? PauseBoxIcon.Instance : PlayBoxIcon.Instance,"", scale: 1, iconSize:30).Size(36,36)
                        .OnClick(_=>{ TogglePlaying(); }),
                    IconButton(ArrowRightBoldBoxIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(CurrentFrame + 1)),
                    IconButton(SkipNextIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(Frames)),
                    ]).Spacing(0),
                null,
                new ProgressBar().Value(()=>CurrentFrame).Maximum(()=>Frames).Ref(out var progress)
                    .Align(null,0).Cursor(new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand))
                    .OnPointerPressed(e=>{
                        if(Frames <= 0) return;
                        var p = e.GetPosition(progress);
                        var w = progress.Bounds.Width;
                        var f = (int)Math.Round(p.X / w * Frames);
                        GotoFrame(f);   
                    })
                ]),
            ])
            .Return(out content);
        }
        else
        {
            Border(
                VStack([TextBlock("未选择任何项目 (No Item Selected)")]).Align(0,0)
                )
            .BorderBrush(Brushes.LightGray).BorderThickness(1)
            .Return(out content);
        }
    }

    protected bool _isPlaying = false;

    private readonly object _audioLock = new();
    private WaveOutEvent? _audioOutput;
    private BufferedWaveProvider? _audioBuffer;
    private float[]? _audioPcm;
    private int _audioSampleRate;
    private double _audioFps;
    private int _nextFeedFrame;
    private int _audioPrebufferFrames;
    private byte[]? _audioFeedBuffer;
    private int _audioSession;
    private bool _audioStartAttempted;

    public void GotoFrame(int frame)
    {
        if (Playable == null) return;
        var old = CurrentFrame;
        if (frame < 0) frame = 0;
        else if (frame > Frames) frame = Frames;

        CurrentFrame = frame;

        // 播放中跳帧：停止当前音频，下一轮循环会从新位置重新开始播放
        if (Playing)
        {
            _audioStartAttempted = false;
            DisposeAudio();
        }

        if(old != CurrentFrame && Playing == false)
        {
            Task.Run(() => {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var bitmap = Work!.CreateBitmap();

                    if (Playable!.Render(bitmap, Work.Stage, CurrentFrame, true))
                    {
                        Bitmap = bitmap;
                        FrameImage.Bitmap = bitmap;
                        FrameImage.InvalidateVisual();
                    }

                    this.UpdateState();
                }).GetAwaiter().GetResult(); // BLOCK until UI work finishes
            });
        }
    }

    public void TogglePlaying()
    {
        if(Playing)
        {
            Playing = false;
            this.UpdateStateByUIThread();
        }
        else
        {
            if (CurrentFrame >= Frames)
                CurrentFrame = 0;
            Play();
            this.UpdateStateByUIThread();
        }
    }

    public void Stop()
    {
        Playing = false;
        while(true)
        {
            if (_isPlaying == false)
                break;

            Thread.Sleep(100);
        }
        this.UpdateStateByUIThread();
    }

    public void Load(IPlayable? playable, NBWork? work)
    {
        bool needReload = (Work == null) && (work != null);
        Playing = false;
        Playable = playable;
        Work = work;
        if (needReload) this.Reload();
        Reset();
        Play();
    }

    private void Reset()
    {
        Frames = 0;
        CurrentFrame = 0;
        Bitmap = null;
        if (Playable != null)
            Frames = Playable.Measure();
        this.UpdateStateByUIThread();
    }

    private void Play()
    {
        if (Playable == null || Work == null) return;
        PlaySimple();
    }

    private void PlaySimple()
    {
        if (Work == null) return;

        // Run the playback loop on a background thread.
        Task.Run(() =>
        {
            double fps = Work.Fps > 0 ? Work.Fps : 25.0;
            double frameIntervalMs = 1000.0 / fps;

            var clock = Stopwatch.StartNew();
            Playing = true;
            _isPlaying = true;
            bool breakByPlayingToogle = false;
            // 每次播放使用新的会话号，避免旧任务退出时误停新任务的音频
            int audioSession = ++_audioSession;
            try
            {
                _audioStartAttempted = false;

                // 绝对时间基准：目标时间 = 起点 + n × 帧间隔。
                // 不能每帧 Sleep(帧间隔 - 渲染耗时)，因为 Thread.Sleep 在 Windows 上有 10~15ms 粒度，
                // 误差会逐帧累积，实际帧率低于设定值，音频持续被“少喂”而出现卡顿。
                double nextFrameMs = clock.Elapsed.TotalMilliseconds;

                while (Playing)
                {
                    if (Playable == null) break;

                    // 音频只在开始播放（或跳帧后）启动一次：从当前帧对应的采样点开始播放，
                    // 之后由音频设备自行推进，不再每帧重复启动
                    if (_audioStartAttempted == false)
                    {
                        _audioStartAttempted = true;
                        StartAudio(audioSession, Playable.MixedAudioData, CurrentFrame);
                    }

                    // 等到该帧的绝对时间点（末尾几毫秒自旋，避免 Sleep 粒度误差）
                    if (WaitUntil(nextFrameMs, clock) == false) break;

                    int renderedFrame = CurrentFrame;

                    // Perform creation + rendering on UI thread and wait until it's done.
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Create bitmap and render on UI thread to avoid cross-thread issues.
                        var bitmap = Work!.CreateBitmap();

                        if (Playable!.Render(bitmap, Work.Stage, renderedFrame, true))
                        {
                            var oldBitmap = Bitmap;
                            Bitmap = bitmap;
                            FrameImage.Bitmap = bitmap;
                            FrameImage.InvalidateVisual();
                        }

                        this.UpdateState();
                    }).GetAwaiter().GetResult(); // BLOCK until UI work finishes

                    CurrentFrame++;
                    nextFrameMs += frameIntervalMs;

                    // 画面推进后才继续喂音频：渲染卡住时缓冲耗尽，声音随之停住
                    FeedAudioUpTo(CurrentFrame + _audioPrebufferFrames);

                    if (CurrentFrame >= Frames)
                        break;

                    // 渲染严重落后时丢帧追赶，避免越拖越久（音频保持实时）
                    double behindMs = clock.Elapsed.TotalMilliseconds - nextFrameMs;
                    if (behindMs > frameIntervalMs * 1.5)
                    {
                        int skip = Math.Min((int)(behindMs / frameIntervalMs), Frames - CurrentFrame);
                        if (skip > 0)
                        {
                            CurrentFrame += skip;
                            nextFrameMs += skip * frameIntervalMs;
                            FeedAudioUpTo(CurrentFrame + _audioPrebufferFrames);
                        }
                    }
                }

                breakByPlayingToogle = !Playing;
            }
            catch (Exception ex)
            {
                // avoid crashing background thread — log if desired
                Console.WriteLine($"PlayerView.PlaySimple exception: {ex.Message}");
            }
            finally
            {
                // 暂停、停止或播放结束都要释放音频设备
                StopAudio(audioSession);

                if(breakByPlayingToogle == false)
                {
                    Playing = CurrentFrame < Frames;
                }

                _isPlaying = false;
                this.UpdateStateByUIThread();
            }
        });
    }

    /// <summary>
    /// 等到指定的绝对时间点（毫秒）。
    /// 前半段 Sleep，最后几毫秒自旋，抵消 Thread.Sleep 在 Windows 上 10~15ms 的粒度误差。
    /// </summary>
    /// <returns>到达时间点返回 true；播放被停止返回 false。</returns>
    private bool WaitUntil(double targetMs, Stopwatch clock)
    {
        while (Playing)
        {
            double remaining = targetMs - clock.Elapsed.TotalMilliseconds;
            if (remaining <= 0) return true;

            if (remaining > 4)
            {
                // 分段睡眠，保证暂停响应不会被长时间的 Sleep 拖住
                int sleep = (int)Math.Min(remaining - 2, 20);
                Thread.Sleep(Math.Max(1, sleep));
            }
            else
            {
                Thread.SpinWait(200);
            }
        }

        return false;
    }

    /// <summary>
    /// 从指定视频帧开始按「推送」方式播放混音结果。
    /// 音频只在画面推进时补充数据，因此渲染卡住时缓冲耗尽，声音会自然停住。
    /// </summary>
    private void StartAudio(int session, StereoPcmData? data, int startFrame)
    {
        lock (_audioLock)
        {
            // 旧会话直接忽略，避免覆盖新会话的音频设备
            if (session != _audioSession) return;

            DisposeAudioCore();

            if (data?.Pcm == null || data.Pcm.Length < 2) return;
            if (Work == null) return;

            int sampleRate = Work.Stage.AudioFrameRate;
            double fps = Work.Fps;
            if (sampleRate <= 0 || fps <= 0) return;

            try
            {
                _audioPcm = data.Pcm;
                _audioSampleRate = sampleRate;
                _audioFps = fps;

                // 预缓冲约 100ms：既避免启动欠载，又不至于让声音明显领先画面
                _audioPrebufferFrames = Math.Max(2, (int)Math.Ceiling(fps * 0.1));

                _audioBuffer = new BufferedWaveProvider(new WaveFormat(sampleRate, 16, 2))
                {
                    BufferDuration = TimeSpan.FromMilliseconds(400),
                    DiscardOnBufferOverflow = true,
                    // 缓冲耗尽时输出静音：画面卡住，声音也随之停住
                    ReadFully = true
                };

                _nextFeedFrame = startFrame;
                FeedAudioCore(startFrame + _audioPrebufferFrames);

                _audioOutput = new WaveOutEvent { DesiredLatency = 100 };
                _audioOutput.Init(_audioBuffer);
                _audioOutput.Play();
            }
            catch (Exception ex)
            {
                // 声卡不可用或格式不支持时不阻断视频预览
                DisposeAudioCore();
                Console.WriteLine($"PlayerView.StartAudio exception: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 停止当前会话的音频；已被新会话接管时不做处理。
    /// </summary>
    private void StopAudio(int session)
    {
        lock (_audioLock)
        {
            if (session != _audioSession) return;
            DisposeAudioCore();
        }
    }

    /// <summary>
    /// 停止并释放音频输出。
    /// </summary>
    private void DisposeAudio()
    {
        lock (_audioLock)
        {
            DisposeAudioCore();
        }
    }

    private void DisposeAudioCore()
    {
        try { _audioOutput?.Stop(); } catch { }
        try { _audioOutput?.Dispose(); } catch { }
        _audioOutput = null;

        _audioBuffer = null;
        _audioPcm = null;
        _audioFeedBuffer = null;
        _audioPrebufferFrames = 0;
        _nextFeedFrame = 0;
    }

    /// <summary>
    /// 把指定帧范围内的音频追加到缓冲。
    /// </summary>
    private void FeedAudioUpTo(int throughFrame)
    {
        lock (_audioLock)
        {
            FeedAudioCore(throughFrame);
        }
    }

    private void FeedAudioCore(int throughFrame)
    {
        if (_audioBuffer == null || _audioPcm == null) return;
        if (_audioSampleRate <= 0 || _audioFps <= 0) return;

        while (_nextFeedFrame <= throughFrame)
        {
            int start = SampleIndexAtFrame(_nextFeedFrame);
            int end = SampleIndexAtFrame(_nextFeedFrame + 1);
            _nextFeedFrame++;

            if (end <= start) continue;

            int sampleCount = end - start;
            int byteCount = sampleCount * sizeof(short);
            EnsureFeedBuffer(byteCount);

            var buffer = _audioFeedBuffer!;
            for (int i = 0; i < sampleCount; i++)
            {
                float value = Math.Clamp(_audioPcm[start + i], -1f, 1f);
                short pcm16 = (short)MathF.Round(value * short.MaxValue);

                buffer[i * 2] = (byte)(pcm16 & 0xFF);
                buffer[i * 2 + 1] = (byte)((pcm16 >> 8) & 0xFF);
            }

            _audioBuffer.AddSamples(buffer, 0, byteCount);
        }
    }

    /// <summary>
    /// 视频帧号 -> 交错立体声样本下标。
    /// </summary>
    private int SampleIndexAtFrame(int frame)
    {
        if (_audioPcm == null) return 0;

        double samplesPerFrame = (double)_audioSampleRate / _audioFps;
        long sample = (long)Math.Round(frame * samplesPerFrame);
        long index = sample * 2;

        return (int)Math.Clamp(index, 0L, _audioPcm.Length);
    }

    private void EnsureFeedBuffer(int byteCount)
    {
        if (_audioFeedBuffer == null || _audioFeedBuffer.Length < byteCount)
        {
            _audioFeedBuffer = new byte[byteCount];
        }
    }

    private async void Export(bool saveAsMp4 = true)
    {
        if (Playable == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var title = saveAsMp4 ? "保存 MP4" : "保存 GIF";
        var fileType = saveAsMp4 ? "mp4" : "gif";

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = $"out_{Playable!.Name}.{fileType}",
            DefaultExtension = fileType,
            FileTypeChoices =
            [
                new FilePickerFileType(fileType.ToUpper())
                {
                    Patterns = [$"*.{fileType}"]
                }
            ]
        });

        if (file is null)
            return;

        var exportView = new ExportVideoView() { Playable = this.Playable!, Work = this.Work! };
        exportView.ExportFilePath = file.Path.LocalPath;
        exportView.OnSave = filePath =>
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                this.ShowToast($"文件已保存到: {filePath}");
            }
        };

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            exportView.ShowInOverlay(this, true);
        });
    }
}
