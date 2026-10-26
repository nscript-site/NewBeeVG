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

    private WaveOutEvent? _audioOutput;
    private WaveStream? _audioStream;
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
            int targetMs = Math.Max(10,(int)(1000.0/Work.Fps));
            var sw = Stopwatch.StartNew();
            Playing = true;
            _isPlaying = true;
            bool breakByPlayingToogle = false;
            // 每次播放使用新的会话号，避免旧任务退出时误停新任务的音频
            int audioSession = ++_audioSession;
            try
            {
                _audioStartAttempted = false;

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

                    var frameStart = sw.ElapsedMilliseconds;

                    // Perform creation + rendering on UI thread and wait until it's done.
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Create bitmap and render on UI thread to avoid cross-thread issues.
                        var bitmap = Work!.CreateBitmap();

                        if (Playable!.Render(bitmap, Work.Stage, CurrentFrame, true))
                        {
                            Bitmap = bitmap;
                            FrameImage.Bitmap = bitmap;
                            FrameImage.InvalidateVisual();
                        }

                        // TODO: 这里 bitmap 和 FrameImage.Source 的生命周期管理可能有问题，可能会导致内存泄漏。

                        this.UpdateState();
                    }).GetAwaiter().GetResult(); // BLOCK until UI work finishes

                    CurrentFrame++;

                    if (CurrentFrame >= Frames)
                        break;

                    var elapsed = (int)(sw.ElapsedMilliseconds - frameStart);
                    var remaining = targetMs - elapsed;
                    if (remaining > 0)
                    {
                        Thread.Sleep(remaining);
                    }
                    // else: no sleep, we are running behind — skip waiting to catch up
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
    /// 从指定视频帧对应的采样点开始播放混音结果。
    /// </summary>
    private void StartAudio(int session, StereoPcmData? data, int startFrame)
    {
        // 旧会话直接忽略，避免覆盖新会话的音频设备
        if (session != _audioSession) return;

        DisposeAudio();

        if (data?.Pcm == null || data.Pcm.Length < 2) return;
        if (Work == null) return;

        int sampleRate = Work.Stage.AudioFrameRate;
        double fps = Work.Fps;
        if (sampleRate <= 0 || fps <= 0) return;

        int sampleFrames = data.Pcm.Length / 2;
        if (sampleFrames <= 0) return;

        // 视频帧号 -> 音频采样点
        long startSample = (long)Math.Round(startFrame * (double)sampleRate / fps);
        if (startSample < 0) startSample = 0;
        if (startSample >= sampleFrames) return;

        try
        {
            _audioStream = new StereoPcm16WaveStream(data.Pcm, sampleRate, startSample);
            _audioOutput = new WaveOutEvent();
            _audioOutput.Init(_audioStream);
            _audioOutput.Play();
        }
        catch (Exception ex)
        {
            // 声卡不可用或格式不支持时不阻断视频预览
            DisposeAudio();
            Console.WriteLine($"PlayerView.StartAudio exception: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止当前会话的音频；已被新会话接管时不做处理。
    /// </summary>
    private void StopAudio(int session)
    {
        if (session != _audioSession) return;
        DisposeAudio();
    }

    /// <summary>
    /// 停止并释放音频输出。
    /// </summary>
    private void DisposeAudio()
    {
        try { _audioOutput?.Stop(); } catch { }
        try { _audioOutput?.Dispose(); } catch { }
        _audioOutput = null;

        try { _audioStream?.Dispose(); } catch { }
        _audioStream = null;
    }

    /// <summary>
    /// 把交错立体声 float PCM（[-1, 1]）按需转换成 16 位 PCM 读取，
    /// 避免为整段音频额外分配一份字节数组。
    /// </summary>
    private sealed class StereoPcm16WaveStream : WaveStream
    {
        private readonly float[] _pcm;
        private readonly WaveFormat _format;
        private readonly long _length;
        private long _position;

        public StereoPcm16WaveStream(float[] pcm, int sampleRate, long startSample)
        {
            _pcm = pcm;
            _format = new WaveFormat(sampleRate, 16, 2);
            _length = (long)pcm.Length * sizeof(short);

            // 每帧立体声占 2 个样本、每个样本 2 字节
            _position = Math.Clamp(startSample * 2 * sizeof(short), 0, _length);
        }

        public override WaveFormat WaveFormat => _format;

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => _position = Math.Clamp(value, 0, _length);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length) return 0;

            const int bytesPerSample = sizeof(short);
            long remainingSamples = (_length - _position) / bytesPerSample;

            int samples = (int)Math.Min(count / bytesPerSample, remainingSamples);
            if (samples <= 0) return 0;

            long sampleIndex = _position / bytesPerSample;

            for (int i = 0; i < samples; i++)
            {
                float value = Math.Clamp(_pcm[sampleIndex + i], -1f, 1f);
                short pcm16 = (short)MathF.Round(value * short.MaxValue);

                buffer[offset + i * 2] = (byte)(pcm16 & 0xFF);
                buffer[offset + i * 2 + 1] = (byte)((pcm16 >> 8) & 0xFF);
            }

            int written = samples * bytesPerSample;
            _position += written;
            return written;
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
