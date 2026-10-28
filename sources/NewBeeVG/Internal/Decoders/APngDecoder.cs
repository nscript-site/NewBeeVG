using ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine;
using ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine.Chunks;
using SkiaSharp;

namespace NewBeeVG.Internal;

internal class APngDecoder
{
    public static readonly APngDecoder Instance = new APngDecoder();

    public string Name { get; } = nameof(APngDecoder);

    public bool Match(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return false;
        return extension == ".png" || extension == ".apng";
    }

    public Size? GetImageSize(Stream stream)
    {
        if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
        var image = new ApngImage(stream);
        return new Size(image.IhdrChunk.Width, image.IhdrChunk.Height);
    }

    public List<SKBitmap>? Read(Stream stream)
    {
        ApngImage image = new ApngImage(stream);

        if (image.IsSimplePng)
        {
            stream.Position = 0;
            var png = DecodePNG(stream);
            if(png != null) return new List<SKBitmap> { png };
        }
        else
        {
            return DecodeAnimatePNG(image.IhdrChunk.Width, image.IhdrChunk.Height, image.Frames);
        }
        return null;
    }

    internal SKBitmap? DecodePNG(Stream stream)
    {
        var bmp = SKBitmap.Decode(stream);
        return bmp;
    }

    internal List<SKBitmap>? DecodeAnimatePNG(int width, int height, Frame[]? frames)
    {
        if (frames == null || frames.Length == 0 || width < 1 || height < 1) return null;

        SKBitmap? prevFrame = null;
        SKBitmap? prevPrevFrame = null;
        DisposeOps? prevDisposeOps = null;

        var list = new List<SKBitmap>(frames.Length);
        for(int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            using var stream = frame.GetStream();
            using var bmp = SKBitmap.Decode(stream);
            if (bmp == null) continue;

            var blendOp = frame.FcTlChunk.BlendOp;
            var disposeOp = frame.FcTlChunk.DisposeOp;

            var frameBmp = new SKBitmap(width, height);

            var duration = frame.FcTlChunk.DelayNum / (float)frame.FcTlChunk.DelayDen;
            Console.WriteLine(duration);

            if (prevDisposeOps != null && prevFrame != null)
            {
                var fInfo = prevDisposeOps.Value;
                if (fInfo == DisposeOps.ApngDisposeOpNone /* 保持上一帧 */)
                {
                    using (var canvas = new SKCanvas(frameBmp))
                    {
                        canvas.DrawBitmap(prevFrame, new SKPoint());
                    }
                }
                else if (fInfo== DisposeOps.ApngDisposeOpPrevious /* 恢复上上帧 */ && prevPrevFrame != null)
                {
                    using (var canvas = new SKCanvas(frameBmp))
                    {
                        canvas.DrawBitmap(prevPrevFrame, new SKPoint());
                    }
                }
            }

            var frameP0 = new SKPoint(
                frame.FcTlChunk.XOffset,
                frame.FcTlChunk.YOffset
            );

            using (var canvas = new SKCanvas(frameBmp))
            {
                using var p = new SKPaint
                {
                    BlendMode = blendOp switch
                    {
                        BlendOps.ApngBlendOpSource => SKBlendMode.Src,
                        BlendOps.ApngBlendOpOver => SKBlendMode.SrcOver,
                        _ => SKBlendMode.Src
                    }
                };
                canvas.DrawBitmap(bmp, frameP0, p);
            }

            list.Add(frameBmp);

            prevPrevFrame = prevFrame;
            prevFrame = frameBmp;
            prevDisposeOps = disposeOp;
        }
        return list;
    }
}
