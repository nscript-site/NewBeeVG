using NewBeeVG.Internal;
using SkiaSharp;

namespace NewBeeVG;

public class NBPngAnimation : NBBaseLoopAnimation
{
    public Stream? PngStream { get; set; }

    public List<SKBitmap>? PngFrames { get; protected set; }

    protected bool IsResourceLoaded;

    public NBPngAnimation(string filePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            PngStream = new MemoryStream(bytes);
        }
        catch(Exception ex)
        {
            DecodeException = ex;
        }
    }

    protected override SKSize? GetImageSize()
    {
        TryLoadResource();
        if (PngFrames != null && PngFrames.Count > 0)
        {
            return new SKSize(PngFrames[0].Width, PngFrames[0].Height);
        }
        return null;
    }

    protected void TryLoadResource()
    {
        if (IsResourceLoaded == true) return;

        IsResourceLoaded = true;
        if (PngStream != null)
        {
            try
            {
                PngFrames = APngDecoder.Instance.Read(PngStream);
                TotalFrames = PngFrames?.Count ?? 0;
                IsResourceLoaded = true;
            }
            catch (Exception ex)
            {
                DecodeException = ex;
            }
        }
    }

    protected override void Draw(SKCanvas context, SKRect src, SKRect dest, SKPaint paint)
    {
        if (PngFrames == null) return;
        
        int frame = NextFrame(TotalFrames, LoopMode, Speed);
        if (frame < 0 || frame >= PngFrames.Count) return;
        var bmp = PngFrames[frame];
        if (bmp == null || bmp.IsEmpty) return;

        context.Save();

        // 计算缩放和平移
        float scaleX = dest.Width / src.Width;
        float scaleY = dest.Height / src.Height;
        // 平移到目标区域
        context.Translate(dest.Left, dest.Top);
        // 缩放到目标大小
        context.Scale(scaleX, scaleY);
        // 平移到原点（如果 cullRect 不从 0,0 开始）
        context.Translate(-src.Left, -src.Top);

        context.DrawBitmap(bmp, new SKPoint(), paint);

        context.Restore();
    }
}