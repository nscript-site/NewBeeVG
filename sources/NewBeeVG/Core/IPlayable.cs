using Avalonia.Media.Imaging;

namespace NewBeeVG;

public interface IPlayable
{
    bool Render(RenderTargetBitmap bitmap, int frame);

    int Measure();

    string FullName { get; }
}
