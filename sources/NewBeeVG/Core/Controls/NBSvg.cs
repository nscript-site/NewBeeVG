using SkiaSharp;
using Svg.Skia;

namespace NewBeeVG;

public class NBSvg : NBBaseImage
{
    public Stream? SvgStream { get; set; }

    public SKPicture? Source { get; protected set; }

    protected SKSvg? Svg { get; set; }

    public Exception? SvgLoadException { get; protected set; }

    protected bool IsSvgLoaded;

    protected override SKSize? GetImageSize()
    {
        TryLoadSvg();
        if (Source != null)
        {
            return new SKSize(Source.CullRect.Width, Source.CullRect.Height);
        }
        return null;
    }

    protected void TryLoadSvg()
    {
        if (IsSvgLoaded == true) return;

        IsSvgLoaded = true;
        if (SvgStream != null)
        {
            try
            {
                Svg = new SKSvg();
                Source = Svg.Load(SvgStream);
                IsSvgLoaded = true;
            }
            catch(Exception ex)
            {
                SvgLoadException = ex;
            }
        }
    } 

    protected override void Draw(SKCanvas context, SKRect src, SKRect dest, SKPaint paint)
    {
        if (Source == null) return;

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

        context.DrawPicture(Source, paint);

        context.Restore();
    }
}

public static partial class NBExtentions
{
    public static T SvgStream<T>(this T widget, Stream? source) where T : NBSvg
    {
        widget.SvgStream = source;
        return widget;
    }

    public static T SvgContent<T>(this T widget, string content) where T : NBSvg
    {
        // 将字符串内容转换为流
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        widget.SvgStream = stream;
        return widget;
    }
}