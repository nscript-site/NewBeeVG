using Avalonia.Platform.Storage;
using Avalonia.Threading;
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

    public void GotoFrame(int frame)
    {
        if (Playable == null) return;
        var old = CurrentFrame;
        if (frame < 0) frame = 0;
        else if (frame > Frames) frame = Frames;

        CurrentFrame = frame;
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
            try
            {
                while (Playing)
                {
                    if (Playable == null) break;

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
                if(breakByPlayingToogle == false)
                {
                    Playing = CurrentFrame < Frames;
                }

                _isPlaying = false;
                this.UpdateStateByUIThread();
            }
        });
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
