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
        VGrid("30,*", [
            HGrid("*,Auto", [
                HStack([
                    TextBlock(()=>Playable?.FullName??String.Empty),
                    TextBlock(()=>$"{Math.Min(Frames,CurrentFrame + 1)}/{Frames}"),
                ]),
                IconButton(VideoCheckOutlineIcon.Instance,"导出视频", scale: 1, iconSize:20).Align(1,1)
                    .OnClick(_ => { Export(); }),
            ]),
            Border(FrameImage).Background(Brushes.Gray)
                .BorderBrush(Brushes.Gray).BorderThickness(1)
        ])
        .Return(out content);
    }

    public void Load(IPlayable? playable, NBWork? work)
    {
        Playing = false;
        Playable = playable;
        Work = work;
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
        // Run the playback loop on a background thread.
        Task.Run(() =>
        {
            const int targetMs = 40; // target frame interval in ms (40ms => 25 FPS)
            var sw = Stopwatch.StartNew();
            Playing = true;

            try
            {
                while (Playing)
                {
                    if (Playable == null || Work == null) break;

                    var frameStart = sw.ElapsedMilliseconds;

                    // Perform creation + rendering on UI thread and wait until it's done.
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Create bitmap and render on UI thread to avoid cross-thread issues.
                        var bitmap = Work!.CreateBitmap();

                        if (Playable!.Render(bitmap, Work.Stage, CurrentFrame))
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
            }
            catch (Exception ex)
            {
                // avoid crashing background thread — log if desired
                Console.WriteLine($"PlayerView.PlaySimple exception: {ex.Message}");
            }
            finally
            {
                Playing = false;
            }
        });
    }

    private async void Export()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存 MP4",
            SuggestedFileName = $"out_{Playable!.FullName}.mp4",
            DefaultExtension = "mp4",
            FileTypeChoices =
            [
                new FilePickerFileType("MP4")
                {
                    Patterns = ["*.mp4"]
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
                this.ShowToast($"视频已保存到: {filePath}");
            }
        };

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            exportView.ShowInOverlay(this, true);
        });
    }
}
