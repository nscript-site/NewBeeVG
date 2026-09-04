using Avalonia.Interactivity;
using Avalonia.Threading;
using NewBeeMedia;

namespace NewBeeVG.Viewer.Widgets;

public class ExportVideoView : BaseView
{
    public IPlayable Playable { get; init; } = default!;
    public NBWork Work { get; init; } = default!;

    private string Message { get; set; } = string.Empty;

    public Action<string>? OnSave { get; set; }

    internal string? ExportFilePath { get; set; }

    protected override void Build(out Control content)
    {
        VGrid("*", [
            TextBlock(() => Message)
            .Align(0,0)
            .Margin(10)
            ])
            .Size(200,100)
            .Background(Brushes.White)
            .Return(out content);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Task.Run(Export);
    }

    private void Export()
    {
        if (ExportFilePath == null) return;

        var frames = Playable.Measure();
        var stage = Work.Stage;
        var filePath = ExportFilePath;

        try
        {
            if(ExportFilePath.EndsWith(".mp4"))
                ExportMp4(filePath, stage, frames);
            else
                ExportGif(filePath, stage, frames);
        }
        finally
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.RemoveFromOverlay();
                OnSave?.Invoke(filePath);
            });
        }
    }

    private void ExportMp4(string filePath, NBStage stage, int frames)
    {
        NBGlobal.CheckOrLoadFFmpeg();

        var writer = new MediaWriter(filePath, stage.Width, stage.Height, stage.FrameRate, true);
        for (int CurrentFrame = 0; CurrentFrame < frames; CurrentFrame++)
        {
            using var bmp = Playable.RenderBitmap(stage, CurrentFrame, true);
            if (bmp == null) break;

            writer.WriteFrame(bmp);

            if (CurrentFrame % 10 == 0)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Message = $"正在导出视频... {CurrentFrame}/{frames}";
                    this.UpdateState();
                });
            }
        }

        writer.Close();
    }

    private void ExportGif(string filePath, NBStage stage, int frames)
    {
        // 帧延迟：单位是 1/100 秒
        int delayHundredths = (int)Math.Round(100.0 / stage.FrameRate);
        using var gifEncoder = AnimatedGif.AnimatedGif.Create(filePath, delayHundredths);

        for (int CurrentFrame = 0; CurrentFrame < frames; CurrentFrame++)
        {
            using var bmp = Playable.RenderBitmap(stage, CurrentFrame, true);
            if (bmp == null) break;

            using var gbmp = DrawingHelper.ToGdiBitmap(bmp);
            gifEncoder.AddFrame(gbmp);

            if (CurrentFrame % 10 == 0)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Message = $"正在导出动画... {CurrentFrame}/{frames}";
                    this.UpdateState();
                });
            }
        }
    }
}
