using NewBeeVG.Core.Controls.Layout;
using SkiaSharp;

namespace NewBeeVG;

public class NBSkiaAnimation : NBBaseImage
{
    public ISKAnimation? Animation { get; set; }

    private SKSize? AnimSize
    {
        get
        {
            if (Animation == null) return null;
            return new SKSize(Animation.Width, Animation.Height);
        }
    }

    public int TotalFrames => Animation?.Frames ?? 0;

    public NBAnimateLoopMode LoopMode { get; set; } = NBAnimateLoopMode.Loop;

    protected override void Draw(SKCanvas context, SKRect src, SKRect dest, SKPaint paint)
    {
        if (Animation == null || AnimSize == null || TotalFrames <= 0) return;

        var size = AnimSize.Value;
        int width = (int)size.Width;
        int height = (int)size.Height;
        if (width <= 0 || height <= 0) return;

        var frame = NBDrawContext.Current?.frame ?? 0;
        frame = (int)(frame);

        if (LoopMode == NBAnimateLoopMode.Loop)
        {
            frame = frame % TotalFrames;
        }
        else if (LoopMode == NBAnimateLoopMode.AutoReverse)
        {
            var cycleFrames = TotalFrames * 2;
            frame = frame % cycleFrames;
            if (frame >= TotalFrames)
            {
                frame = cycleFrames - frame - 1;
            }
        }

        src = new SKRect(0, 0, width, height);
        SKMatrix? m = src == dest ? null : NBLayoutHelper.CreateRectTransform(src, dest);
        Draw(Animation, context, frame, m);
    }

    protected void Draw(ISKAnimation anim, SKCanvas canvas, int frameIndex, SKMatrix? m = null)
    {
        if (frameIndex < 0 || frameIndex >= TotalFrames) return;
        canvas.Save();
        if (m != null) canvas.Concat(m.Value);
        anim.Draw(canvas, frameIndex);
        canvas.Restore();
    }
}
