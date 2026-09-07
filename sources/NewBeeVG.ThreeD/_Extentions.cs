using FFmpeg.AutoGen.Abstractions;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Maths;
using SkiaSharp;

namespace NewBeeVG.ThreeD;

public static class _Extentions
{
    public static void Submit(this IContext ctx, ICommandBuffer commandBuffer, bool wait = false)
    {
        var handle = ctx.Submit(commandBuffer, HelixToolkit.Nex.Handle<HelixToolkit.Nex.Graphics.Texture>.Null);
        if(wait == true) ctx.Wait(handle);
    }

    public static unsafe void Save(this TextureResource? _renderTexture, IContext ctx, Size size, string bmpFilePath)
    {
        _renderTexture.SaveSKBitmap(ctx, size.Width, size.Height, bmpFilePath);
    }

    public static unsafe void SaveSKBitmap(this TextureResource? _renderTexture, IContext ctx, int width, int height, string bmpFilePath)
    {
        var data = _renderTexture.SnapshotData(ctx, width, height);
        if (data == null)
            return;

        DrawingHelper.SaveBgraToBmp(data, width, height, bmpFilePath);
    }

    public static SKBitmap? SnapshotSKBitmap(this TextureResource? _renderTexture, IContext ctx, int width, int height)
    {
        var data = _renderTexture.SnapshotData(ctx, width, height);
        if (data == null)
            return null;

        return DrawingHelper.BgraToSKBitmap(data, width, height);
    }

    public static unsafe byte[]? SnapshotData(this TextureResource? _renderTexture, IContext ctx, int width, int height)
    {
        if (_renderTexture == null)
            return null;

        var desc = new TextureRangeDesc() { Dimensions = new Dimensions((uint)width, (uint)height) };
        var buff = new byte[width * height * 4];
        TextureHandle h = _renderTexture.Handle;
        fixed (byte* p = buff)
        {
            ctx.Download(h, desc, (nint)p, (uint)buff.Length);
        }

        return buff;
    }
}
