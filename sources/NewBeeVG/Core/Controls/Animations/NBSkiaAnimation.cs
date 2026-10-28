using NewBeeVG.Core.Controls.Layout;
using SkiaSharp;

namespace NewBeeVG;

public class NBSkiaAnimation : NBBaseLoopAnimation
{
    public ISKAnimation? Animation {
        get; 
        set
        {
            field = value; 
            TotalFrames = value?.Frames ?? 0;
        } 
    }

    private SKSize? AnimSize
    {
        get
        {
            if (Animation == null) return null;
            return new SKSize(Animation.Width, Animation.Height);
        }
    }

    protected override SKSize? GetImageSize()
    {
        var size = AnimSize;
        if (size != null)
        {
            return new SKSize(size.Value.Width, size.Value.Height);
        }
        return null;
    }

    protected override void Draw(SKCanvas context, SKRect src, SKRect dest, SKPaint paint)
    {
        if (Animation == null || AnimSize == null || TotalFrames <= 0) return;

        var size = AnimSize.Value;
        int width = (int)size.Width;
        int height = (int)size.Height;
        if (width <= 0 || height <= 0) return;

        var frame = NextFrame(TotalFrames, LoopMode, Speed);

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
