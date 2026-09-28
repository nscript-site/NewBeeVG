using Avalonia.Interactivity;
using Avalonia.Threading;
using NewBeeMedia;
using NewBeeMedia.Encoders.Gif;
using SkiaSharp;

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
                ExportGif(filePath, stage, frames,1024);
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

    private void ExportGif(string filePath, NBStage stage, int frames, int maxSize = int.MaxValue)
    {
        // 帧延迟：单位是 1/100 秒
        int delayHundredths = (int)Math.Round(100.0 / stage.FrameRate);

        var opt = new GifEncoderOptions();
        opt.MaxWidth = 1200;
        opt.MaxHeight = 1200;

        using var gifEncoder2 = GifEncoder.Create(filePath, delayHundredths, opt);

        for (int CurrentFrame = 0; CurrentFrame < frames; CurrentFrame++)
        {
            using var bmp = Playable.RenderBitmap(stage, CurrentFrame, true);
            if (bmp == null) break;

            gifEncoder2.AddFrame(bmp);

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

    public static SKBitmap ResizeProportionalMaxSize(SKBitmap src, int maxSize)
    {
        if (src.Width <= maxSize && src.Height <= maxSize)
        {
            // 已经小于等于maxSize，直接克隆返回，不做缩放
            return src.Copy();
        }

        // 计算缩放系数，取宽、高两者中缩放更大的比例
        float scaleW = (float)maxSize / src.Width;
        float scaleH = (float)maxSize / src.Height;
        float scale = Math.Min(scaleW, scaleH);

        int targetW = (int)Math.Round(src.Width * scale);
        int targetH = (int)Math.Round(src.Height * scale);

        // 修正为偶数，适配编码器要求
        targetW = targetW - (targetW % 2);
        targetH = targetH - (targetH % 2);

        var destInfo = new SKImageInfo(targetW, targetH, src.ColorType, src.AlphaType);
        return src.Resize(destInfo, SKFilterQuality.High);
    }
}
