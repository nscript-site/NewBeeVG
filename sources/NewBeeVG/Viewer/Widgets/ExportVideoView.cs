using Avalonia.Interactivity;
using Avalonia.Threading;
using NewBeeMedia;

namespace NewBeeVG.Viewer.Widgets;

public class ExportVideoView : BaseView
{
    public IPlayable Playable { get; init; } = default!;
    public NBWork Work { get; init; } = default!;

    private string Message { get; set; } = string.Empty;

    protected override void Build(out Control content)
    {
        TextBlock(() => Message)
            .Align(0,0)
            .Margin(10)
            .Return(out content);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Task.Run(Export);
    }

    private void Export()
    {
        var frames = Playable.Measure();
        var stage = Work.Stage;

        NBGlobal.CheckOrLoadFFmpeg();

        var writer = new MediaWriter(DateTime.Now.ToFileTimeUtc() + ".mp4", stage.Width, stage.Height,stage.FrameRate, true);

        try
        {
            for (int CurrentFrame = 0; CurrentFrame < frames; CurrentFrame++)
            {
                using var bmp = Playable.Render(stage, CurrentFrame, true);
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
        finally
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.RemoveFromOverlay();
            });
        }
    }
}
