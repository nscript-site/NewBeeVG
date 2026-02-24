using Avalonia.Media.Imaging;

namespace NewBeeVG.Viewer.Widgets;

public class PlayerView : BaseView
{
    public IPlayable? Playable { get; set; } = default!;
    public NBWork? Work { get; set; } = default!;

    private int Frames = 0;
    private int CurrentFrame = 0;
    private RenderTargetBitmap? Bitmap = null;
    private Image Image = default!;

    protected bool Playing { get; set; }

    protected override void Build(out Control content)
    {
        Image = new Image();
        VGrid("30,*", [
            HStack([
                TextBlock(()=>Playable?.FullName??String.Empty),
                TextBlock(()=>$"{Math.Min(Frames,CurrentFrame + 1)}/{Frames}"),
            ]),
            Border(Image).Background(Brushes.Gray)
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
        Task.Run(() =>
        {
            Playing = true;
            while (Playing)
            {
                if (Playable == null || Work == null) break;

                var bitmap = Work.CreateBitmap();

                this.InvokeByUIThread(() => {

                    if (Playable.Render(bitmap, Work.Stage, CurrentFrame))
                    {
                        Bitmap = bitmap;
                        Image.Source = Bitmap;
                    }

                    this.UpdateState();
                });

                Thread.Sleep(1000/25);
                CurrentFrame++;
                if (CurrentFrame >= Frames)
                    break;
            }
            Playing = false;
        });
    }
}
