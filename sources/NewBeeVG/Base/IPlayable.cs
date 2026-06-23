using SkiaSharp;

namespace NewBeeVG;

public interface IPlayable
{
    public bool Render(SKBitmap bitmap, NBStage stage, int frame)
    {
        using(SKCanvas canvas = new SKCanvas(bitmap))
        {
            this.Render(canvas, stage, frame, true);
            return true;
        }
    }

    public SKBitmap RenderBitmap(NBStage stage, int frame, bool includeStageBackground)
    {
        var bitmap = new SKBitmap(stage.Width, stage.Height);
        using (SKCanvas canvas = new SKCanvas(bitmap))
        {
            this.Render(canvas, stage, frame, includeStageBackground);
            return bitmap;
        }
    }

    void Render(SKCanvas canvas, NBStage stage, int frame, bool includeStageBackground);

    /// <summary>
    /// 准备。有些 playable 需要准备工作。
    /// </summary>
    void Prepare();

    int Measure();

    string FullName { get; }
}
