using Avalonia.Media.Imaging;
using SkiaSharp;

namespace NewBeeVG;

public class DrawingHelper
{
    public static void DrawBitmap(RenderTargetBitmap bitmap, Control? content, int width, int height)
    {
        if (content == null) return;

        Layout(content, width, height);

        bitmap.Render(content);
    }

    public static unsafe WriteableBitmap? ToWriteableBitmap(SKBitmap? bitmap)
    {
        if (bitmap == null || bitmap.IsEmpty) return null;

        var pixelSize = new PixelSize(bitmap.Width, bitmap.Height);
        var dpi = new Vector(96, 96);

        // 先把 SKBitmap 的像素直接拷贝到 WriteableBitmap
        var writeableBitmap = new WriteableBitmap(
            pixelSize,
            dpi,
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);

        var srcPixels = bitmap.GetPixels();
        if (srcPixels == IntPtr.Zero)
            return null;

        using (var locked = writeableBitmap.Lock())
        {
            var srcStride = bitmap.RowBytes;
            var dstStride = locked.RowBytes;
            var copyStride = Math.Min(srcStride, dstStride);

            byte* srcBase = (byte*)srcPixels.ToPointer();
            byte* dstBase = (byte*)locked.Address.ToPointer();

            for (int y = 0; y < bitmap.Height; y++)
            {
                Buffer.MemoryCopy(
                    srcBase + y * srcStride,
                    dstBase + y * dstStride,
                    dstStride,
                    copyStride);
            }
        }

        return writeableBitmap;
    }

    public static SKBitmap ToSKBitmap(RenderTargetBitmap rtBmp, NBStage? stage = null, bool drawStageBackground = false)
    {
        var width = rtBmp.PixelSize.Width;
        var height = rtBmp.PixelSize.Height;

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var skBmp = new SKBitmap(info);

        if(stage != null && drawStageBackground == true)
        {
            FillStageBackgroundIfSet(skBmp, stage, drawStageBackground);
        }

        var pixels = skBmp.GetPixels();
        if (pixels == IntPtr.Zero)
        {
            skBmp.Dispose();
            throw new InvalidOperationException("Failed to get pixels from SKBitmap.");
        }

        var pixelRect = new PixelRect(0, 0, width, height);
        rtBmp.CopyPixels(pixelRect, pixels, skBmp.RowBytes * height, skBmp.RowBytes);

        return skBmp;
    }

    public static System.Drawing.Bitmap ToGdiBitmap(SKBitmap skBmp)
    {
        // SKBitmap必须是BGRA8888
        if (skBmp.ColorType != SKColorType.Bgra8888)
            throw new NotSupportedException("仅支持 Bgra8888");

        var width = (int)skBmp.Width;
        var height = (int)skBmp.Height;
        var gdiBmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        var bmpData = gdiBmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            // 直接内存拷贝
            var srcSpan = skBmp.GetPixelSpan();
            System.Runtime.InteropServices.Marshal.Copy(srcSpan.ToArray(), 0, bmpData.Scan0, srcSpan.Length);
        }
        finally
        {
            gdiBmp.UnlockBits(bmpData);
        }
        return gdiBmp;
    }

    public static void FillStageBackgroundIfSet(SKBitmap? bmp, NBStage stage, bool drawStageBackground)
    {
        if (bmp == null || drawStageBackground == false || stage.Background == null) return;
        var skColor = stage.Background.Value;
        using var canvas = new SKCanvas(bmp);
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = skColor,
            IsAntialias = true
        };
        canvas.DrawRect(new SKRect(0, 0, bmp.Width, bmp.Height), paint);
    }

    public static SKBitmap? Render(Control? control, NBStage stage, bool includeStageBackground)
    {
        if (control == null) return null;

        using var rtBmp = new RenderTargetBitmap(new PixelSize(stage.Width, stage.Height));
        DrawBitmap(rtBmp, control, stage.Width, stage.Height);
        return DrawingHelper.ToSKBitmap(rtBmp, stage, includeStageBackground);
    }

    public static void Layout(Control? content, int width, int height)
    {
        if (content == null) return;

        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
    }

    public static SKColor ToSKColor(ISolidColorBrush brush)
    {
        return new SKColor(
            brush.Color.R,
            brush.Color.G,
            brush.Color.B,
            (byte)(brush.Opacity * 255));
    }

    /// <summary>
    /// 将BGRA字节数组(B G R A)保存为32bit BMP，保留Alpha
    /// </summary>
    /// <param name="bgra">输入：B G R A 顺序，每像素4字节</param>
    /// <param name="width">图像宽</param>
    /// <param name="height">图像高</param>
    /// <param name="filePath">输出路径</param>
    public static void SaveBgraToBmp(byte[] bgra, int width, int height, string filePath)
    {
        int pixelDepth = 32;
        int stride = width * 4; // 32bit：每行字节数，天然4对齐
        int pixelDataSize = stride * height;
        int totalFileSize = 14 + 40 + pixelDataSize;

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // ========== BITMAPFILEHEADER 14字节 ==========
        bw.Write((ushort)0x4D42);       // 'BM' 标记
        bw.Write((uint)totalFileSize);  // 整个文件大小
        bw.Write((ushort)0);           // 保留1
        bw.Write((ushort)0);           // 保留2
        bw.Write((uint)14 + 40);       // 像素数据偏移：文件头+信息头

        // ========== BITMAPINFOHEADER 40字节 ==========
        bw.Write((uint)40);            // 本结构体大小
        bw.Write((int)width);          // 宽
        bw.Write((int)-height);        // ⭐负数height：行顺序=从上到下，不用翻转像素行！
        bw.Write((ushort)1);           // 色彩平面数
        bw.Write((ushort)pixelDepth);   // 32 bits per pixel
        bw.Write((uint)0);             // 压缩方式：0=无压缩 BI_RGB
        bw.Write((uint)pixelDataSize); // 像素数据大小
        bw.Write((int)0);              // 水平分辨率像素/m
        bw.Write((int)0);              // 垂直分辨率像素/m
        bw.Write((uint)0);             // 调色板颜色数
        bw.Write((uint)0);             // 重要颜色数

        // ========== 写入像素数据 ==========
        // 技巧：height传负数，BMP解释为“自上而下存储”，就不用手动颠倒行顺序
        bw.Write(bgra);
    }

    /// <summary>
    /// 将BGRA字节数组(B G R A)转换为SKBitmap并保存到文件，保留Alpha
    /// </summary>
    /// <param name="bgra">输入：B G R A 顺序，每像素4字节</param>
    /// <param name="width">图像宽</param>
    /// <param name="height">图像高</param>
    /// <returns>转换得到的SKBitmap，调用方负责释放</returns>
    public static unsafe SKBitmap BgraToSKBitmap(byte[] bgra, int width, int height)
    {
        if (bgra == null)
            throw new ArgumentNullException(nameof(bgra));
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "宽高必须大于0");
        if (bgra.Length < width * height * 4)
            throw new ArgumentException("字节数组长度小于所需的像素数据大小");

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        var bmp = new SKBitmap(info);
        var srcPixels = bmp.GetPixels();
        if (srcPixels == IntPtr.Zero)
            throw new InvalidOperationException("获取SKBitmap像素失败");

        var dstStride = bmp.RowBytes;
        var srcStride = width * 4;

        // 若源数据行是4字节对齐（BGRA天然满足），直接整体拷贝
        if (dstStride == srcStride && bgra.Length == dstStride * height)
        {
            fixed (byte* src = bgra)
            {
                Buffer.MemoryCopy(src, srcPixels.ToPointer(), (ulong)(dstStride * height), (ulong)(dstStride * height));
            }
        }
        else
        {
            // 处理行对齐不一致的情况，逐行拷贝
            fixed (byte* srcBase = bgra)
            {
                var dstBase = (byte*)srcPixels.ToPointer();
                var copyStride = Math.Min(srcStride, dstStride);
                for (int y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(
                        srcBase + (long)y * srcStride,
                        dstBase + (long)y * dstStride,
                        dstStride,
                        copyStride);
                }
            }
        }

        return bmp;
    }
}
