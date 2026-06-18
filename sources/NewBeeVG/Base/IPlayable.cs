using Avalonia.Media.Imaging;
using SkiaSharp;

namespace NewBeeVG;

public interface IPlayable
{
    public bool Render(SKBitmap bitmap, NBStage stage, int frame)
    {
        var content = Render(stage, frame, true);
        if (content == null) return false;

        // 使用 skia 将 content 渲染到 SKBitmap 上
        using(SKCanvas canvas = new SKCanvas(bitmap))
        {
            canvas.DrawBitmap(content, new SKPoint());
        }
        return true;
    }

    SKBitmap? Render(NBStage stage, int frame, bool includeStageBackground);

    /// <summary>
    /// 准备。有些 playable 需要准备工作。
    /// </summary>
    void Prepare();

    int Measure();

    string FullName { get; }
}
