using Avalonia.Media.Imaging;

namespace NewBeeVG;

public interface IPlayable
{
    public bool Render(RenderTargetBitmap bitmap, NBStage stage, int frame)
    {
        var content = Build(stage, frame, true);
        if (content == null) return false;

        bitmap.Render(content);

        return true;
    }

    Control? Build(NBStage stage, int frame, bool includeStageBackground);

    int Measure();

    string FullName { get; }
}
