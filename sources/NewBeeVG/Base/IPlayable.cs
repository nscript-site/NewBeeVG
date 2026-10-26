using SkiaSharp;

namespace NewBeeVG;

public interface IPlayable
{
    public void Reset();

    public bool Render(SKBitmap bitmap, NBStage stage, int frame, bool preview)
    {
        using(SKCanvas canvas = new SKCanvas(bitmap))
        {
            this.Render(canvas, stage, frame, true, preview);
            return true;
        }
    }

    public SKBitmap RenderBitmap(NBStage stage, int frame, bool includeStageBackground, bool preview)
    {
        var bitmap = new SKBitmap(stage.Width, stage.Height);
        using (SKCanvas canvas = new SKCanvas(bitmap))
        {
            this.Render(canvas, stage, frame, includeStageBackground, preview);
            return bitmap;
        }
    }

    void Render(SKCanvas canvas, NBStage stage, int frame, bool includeStageBackground, bool preview);

    /// <summary>
    /// 准备。有些 playable 需要准备工作。
    /// </summary>
    void Prepare(NBStage stage);

    int Measure();

    string FullName { get; }

    string Name { get; }
} 
