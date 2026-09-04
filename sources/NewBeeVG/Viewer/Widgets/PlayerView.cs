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
            HGrid("*,Auto,Auto", [
                HStack([
                    TextBlock(()=>Playable?.FullName??String.Empty),
                    TextBlock(()=>$"{Math.Min(Frames,CurrentFrame + 1)}/{Frames}"),
                ]),
                IconButton(VideoCheckOutlineIcon.Instance,"导出视频", scale: 1, iconSize:16).Align(1,1)
                    .OnClick(_ => { Export(true); }),
                IconButton( AnimationPlayOutlineIcon.Instance,"导出动画", scale: 1, iconSize:16).Align(1,1)
                    .OnClick(_ => { Export(false); }),
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
        if (Work == null) return;

        // Run the playback loop on a background thread.
        Task.Run(() =>
        {
            int targetMs = Math.Max(10,(int)(1000.0/Work.Fps));
            var sw = Stopwatch.StartNew();
            Playing = true;

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
            SuggestedFileName = $"out_{Playable!.FullName}.{fileType}",
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
