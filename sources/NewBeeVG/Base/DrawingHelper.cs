using Avalonia.Media.Imaging;

namespace NewBeeVG;

internal class DrawingHelper
{
    public static void DrawBitmap(RenderTargetBitmap bitmap, Control? content, int width, int height)
    {
        if (content == null) return;

        Layout(content, width, height);

        bitmap.Render(content);
    }

    public static void Layout(Control? content, int width, int height)
    {
        if (content == null) return;

        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
    }
}
