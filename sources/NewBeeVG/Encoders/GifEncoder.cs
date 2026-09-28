// 本文件由 Kimi K3 生成

using SkiaSharp;

namespace NewBeeVG.Encoders;

// -----------------------------------------------------------------------------
// GifEncoder.cs
//
// 基于 SkiaSharp 的 GIF89a 动画编码器：
//   - 将多张尺寸相同的 SKBitmap 逐帧编码为一个 GIF 动画
//   - 支持缩放压缩（Scale / MaxWidth / MaxHeight），可显著减小体积
//   - 内置 NeuQuant 神经网络调色板量化（自动裁剪未用颜色）+ GIF LZW 压缩
//   - 支持透明像素（Alpha 阈值二值化）、循环次数、逐帧延迟
//
// 依赖：NuGet 包 SkiaSharp（代码按 2.88.x API 编写，3.x 适配方式见
//       ScaleBitmap() 内注释）
//
// 用法示例：
//   int delayHundredths = (int)Math.Round(100.0 / stage.FrameRate);
//   using var gifEncoder = GifEncoder.Create(filePath, delayHundredths);
//   for (int currentFrame = 0; currentFrame < frames; currentFrame++)
//   {
//       using var bmp = Playable.RenderBitmap(stage, currentFrame, true);
//       if (bmp == null) break;
//       gifEncoder.AddFrame(bmp);
//   }
//
// 缩放压缩示例（宽高减半，文件体积约为原来的 1/4）：
//   using var gifEncoder = GifEncoder.Create(filePath, delayHundredths,
//       new GifEncoderOptions { Scale = 0.5 });
// 或限制最长边：
//   new GifEncoderOptions { MaxWidth = 480 }
// -----------------------------------------------------------------------------


/// <summary>GIF 编码选项。</summary>
public sealed class GifEncoderOptions
{
    /// <summary>
    /// 等比缩放系数，范围 (0, 1]，默认 1.0（不缩放）。
    /// 0.5 表示宽高各减半（像素数为 1/4，体积显著下降）。
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>输出最大宽度（像素）。超过时按比例缩小，null 表示不限制。</summary>
    public int? MaxWidth { get; set; }

    /// <summary>输出最大高度（像素）。超过时按比例缩小，null 表示不限制。</summary>
    public int? MaxHeight { get; set; }

    /// <summary>循环次数：0 = 无限循环（默认）；N &gt; 0 = 循环 N 次。</summary>
    public int LoopCount { get; set; } = 0;

    /// <summary>
    /// NeuQuant 采样精度：1 = 最高质量（最慢），30 = 最快（质量最低）。
    /// 默认 10，速度与质量均衡。
    /// </summary>
    public int SampleQuality { get; set; } = 10;

    /// <summary>
    /// 透明阈值：Alpha 小于该值的像素输出为透明（GIF 只支持二值透明）。
    /// 默认 128。
    /// </summary>
    public byte AlphaThreshold { get; set; } = 128;

    /// <summary>
    /// 半透明像素的衬底色。设置后 Alpha ≥ <see cref="AlphaThreshold"/> 且 &lt; 255
    /// 的像素会合成到该颜色上；null 表示直接使用原始 RGB。
    /// </summary>
    public SKColor? MatteColor { get; set; }

    /// <summary>
    /// GIF 处置方式（Disposal Method）：0/1 = 保留上一帧（默认 1）；
    /// 2 = 恢复背景；3 = 恢复上一帧之前。透明帧叠加出现问题时可尝试 2。
    /// </summary>
    public int DisposalMethod { get; set; } = 1;
}

/// <summary>
/// GIF89a 动画编码器。实现 <see cref="IDisposable"/>，释放时自动写入
/// 文件尾并关闭输出。所有帧缩放后的尺寸必须一致，否则抛异常。
/// </summary>
public sealed class GifEncoder : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _ownsStream;
    private readonly int _defaultDelayHundredths;
    private readonly GifEncoderOptions _options;

    private int _width = -1;   // 输出（缩放后）宽度
    private int _height = -1;  // 输出（缩放后）高度
    private int _frameCount;
    private bool _headerWritten;
    private bool _finished;
    private bool _disposed;

    /// <summary>已写入的帧数。</summary>
    public int FrameCount => _frameCount;

    /// <summary>输出 GIF 的宽度（写入首帧后确定，之前为 -1）。</summary>
    public int OutputWidth => _width;

    /// <summary>输出 GIF 的高度（写入首帧后确定，之前为 -1）。</summary>
    public int OutputHeight => _height;

    // ---------------------------------------------------------------------
    // 创建
    // ---------------------------------------------------------------------

    /// <summary>
    /// 创建编码器并输出到文件。目录不存在时自动创建。
    /// </summary>
    /// <param name="filePath">输出 .gif 文件路径。</param>
    /// <param name="delayHundredths">每帧延迟，单位 1/100 秒（如 25fps → 4）。</param>
    /// <param name="options">编码选项，null 使用默认。</param>
    public static GifEncoder Create(string filePath, int delayHundredths,
        GifEncoderOptions options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        string dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        return new GifEncoder(
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None),
            ownsStream: true, delayHundredths, options);
    }

    /// <summary>
    /// 创建编码器并输出到指定流（不拥有该流，Dispose 时不会关闭它）。
    /// </summary>
    public static GifEncoder Create(Stream stream, int delayHundredths,
        GifEncoderOptions options = null)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite) throw new ArgumentException("流不可写", nameof(stream));
        return new GifEncoder(stream, ownsStream: false, delayHundredths, options);
    }

    private GifEncoder(Stream stream, bool ownsStream, int delayHundredths,
        GifEncoderOptions options)
    {
        if (delayHundredths < 0 || delayHundredths > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delayHundredths),
                "延迟必须在 0 ~ 65535（1/100 秒）之间");

        _stream = stream;
        _ownsStream = ownsStream;
        _defaultDelayHundredths = delayHundredths;
        _options = options ?? new GifEncoderOptions();
    }

    // ---------------------------------------------------------------------
    // 写帧
    // ---------------------------------------------------------------------

    /// <summary>添加一帧（使用创建时指定的延迟）。</summary>
    /// <param name="bitmap">帧位图，缩放后尺寸必须与前面各帧一致。</param>
    public void AddFrame(SKBitmap bitmap) => AddFrame(bitmap, null);

    /// <summary>添加一帧，可单独覆盖该帧的延迟。</summary>
    /// <param name="bitmap">帧位图，缩放后尺寸必须与前面各帧一致。</param>
    /// <param name="delayHundredths">该帧延迟（1/100 秒），null 使用默认值。</param>
    public void AddFrame(SKBitmap bitmap, int? delayHundredths)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GifEncoder));
        if (_finished) throw new InvalidOperationException("编码已结束，不能再添加帧");
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
            throw new ArgumentException("位图尺寸无效", nameof(bitmap));

        int delay = delayHundredths ?? _defaultDelayHundredths;
        if (delay < 0 || delay > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delayHundredths));

        // 1. 按需缩放
        SKBitmap frame = null;
        try
        {
            (int tw, int th) = ComputeTargetSize(bitmap.Width, bitmap.Height);
            if (_width < 0)
            {
                _width = tw;
                _height = th;
            }
            else if (tw != _width || th != _height)
            {
                throw new InvalidOperationException(
                    $"帧尺寸不一致：期望 {_width}x{_height}，实际 {tw}x{th}。" +
                    "所有帧（缩放后）尺寸必须相同。");
            }

            frame = (tw == bitmap.Width && th == bitmap.Height)
                ? bitmap
                : ScaleBitmap(bitmap, tw, th);

            // 2. 量化 + 写帧
            WriteFrame(frame, delay);
            _frameCount++;
        }
        finally
        {
            // 仅释放缩放产生的临时位图
            if (frame != null && !ReferenceEquals(frame, bitmap))
                frame.Dispose();
        }
    }

    /// <summary>结束编码并写入 GIF 文件尾。Dispose 时会自动调用。</summary>
    public void Finish()
    {
        if (_finished) return;
        if (_headerWritten)
            _stream.WriteByte(0x3B); // GIF Trailer
        _stream.Flush();
        _finished = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            Finish();
        }
        finally
        {
            if (_ownsStream)
                _stream.Dispose();
        }
    }

    // ---------------------------------------------------------------------
    // 缩放
    // ---------------------------------------------------------------------

    private (int w, int h) ComputeTargetSize(int srcW, int srcH)
    {
        double scale = _options.Scale;
        if (scale <= 0 || scale > 1) scale = 1;

        int w = Math.Max(1, (int)Math.Round(srcW * scale));
        int h = Math.Max(1, (int)Math.Round(srcH * scale));

        if (_options.MaxWidth is int mw && mw > 0 && w > mw)
        {
            double r = (double)mw / w;
            w = mw;
            h = Math.Max(1, (int)Math.Round(h * r));
        }
        if (_options.MaxHeight is int mh && mh > 0 && h > mh)
        {
            double r = (double)mh / h;
            h = mh;
            w = Math.Max(1, (int)Math.Round(w * r));
        }
        return (w, h);
    }

    private static SKBitmap ScaleBitmap(SKBitmap source, int w, int h)
    {
        // SkiaSharp 2.88.x：
        var scaled = new SKBitmap(w, h, source.ColorType, source.AlphaType);
        using (var canvas = new SKCanvas(scaled))
        using (var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        })
        {
            canvas.DrawBitmap(source, new SKRect(0, 0, w, h), paint);
            canvas.Flush();
        }
        return scaled;

        // SkiaSharp 3.x（SKFilterQuality 已移除）改用：
        //   return source.Resize(new SKImageInfo(w, h),
        //       new SKSamplingOptions(SKCubicResampler.Mitchell));
    }

    // ---------------------------------------------------------------------
    // 帧编码：量化 + 调色板 + LZW
    // ---------------------------------------------------------------------

    private void WriteFrame(SKBitmap bmp, int delay)
    {
        int w = bmp.Width, h = bmp.Height;
        int pixelCount = w * h;
        byte alphaThreshold = _options.AlphaThreshold;
        SKColor? matte = _options.MatteColor;

        // --- 第一遍：拆分透明/不透明像素 --------------------------
        SKColor[] pixels = bmp.Pixels;
        var rgb = new byte[pixelCount * 3];       // 不透明像素的 RGB 序列
        var isTransparent = new bool[pixelCount];
        int opaqueCount = 0;
        bool hasTransparency = false;

        for (int i = 0; i < pixelCount; i++)
        {
            SKColor c = pixels[i];
            if (c.Alpha < alphaThreshold)
            {
                isTransparent[i] = true;
                hasTransparency = true;
                continue;
            }
            byte r = c.Red, g = c.Green, b = c.Blue;
            if (c.Alpha < 255 && matte.HasValue)
            {
                // 半透明像素合成到衬底色
                float a = c.Alpha / 255f;
                SKColor m = matte.Value;
                r = (byte)(r * a + m.Red * (1 - a) + 0.5f);
                g = (byte)(g * a + m.Green * (1 - a) + 0.5f);
                b = (byte)(b * a + m.Blue * (1 - a) + 0.5f);
            }
            int o = opaqueCount * 3;
            rgb[o] = r; rgb[o + 1] = g; rgb[o + 2] = b;
            opaqueCount++;
        }

        // --- 量化 ---------------------------------------------------
        // NeuQuant 固定输出 256 项调色板，之后裁剪掉未使用的颜色。
        int quantColors;
        byte[] quantPalette;   // RGB，长度 = quantColors * 3
        NeuQuant nq = null;

        if (opaqueCount > 0)
        {
            byte[] sample = new byte[opaqueCount * 3];
            Array.Copy(rgb, sample, sample.Length);
            int quality = _options.SampleQuality;
            if (quality < 1) quality = 1;
            if (quality > 30) quality = 30;
            nq = new NeuQuant(sample, sample.Length, quality);
            quantPalette = nq.Process();
            quantColors = 256;
        }
        else
        {
            // 全透明帧：只需一个占位颜色
            quantPalette = new byte[] { 0, 0, 0 };
            quantColors = 1;
        }

        // --- 建立索引位图 + 裁剪未用颜色 -----------------------------
        var indexed = new byte[pixelCount];
        var used = new bool[quantColors];

        if (nq != null)
        {
            for (int i = 0; i < pixelCount; i++)
            {
                if (isTransparent[i]) continue; // 稍后统一填透明索引
                SKColor c = pixels[i];
                byte r = c.Red, g = c.Green, b = c.Blue;
                if (c.Alpha < 255 && matte.HasValue)
                {
                    float a = c.Alpha / 255f;
                    SKColor m = matte.Value;
                    r = (byte)(r * a + m.Red * (1 - a) + 0.5f);
                    g = (byte)(g * a + m.Green * (1 - a) + 0.5f);
                    b = (byte)(b * a + m.Blue * (1 - a) + 0.5f);
                }
                int idx = nq.Inxsearch(r, g, b); // 与量化输入同为 RGB 顺序
                indexed[i] = (byte)idx;
                used[idx] = true;
            }
        }

        // 裁剪调色板：旧索引 -> 新索引
        var remap = new int[quantColors];
        int usedCount = 0;
        for (int i = 0; i < quantColors; i++)
        {
            remap[i] = used[i] ? usedCount++ : -1;
        }
        for (int i = 0; i < pixelCount; i++)
        {
            if (!isTransparent[i])
                indexed[i] = (byte)remap[indexed[i]];
        }

        // 透明索引排在已用颜色之后
        int transparentIndex = -1;
        if (hasTransparency)
        {
            transparentIndex = usedCount;
            for (int i = 0; i < pixelCount; i++)
                if (isTransparent[i])
                    indexed[i] = (byte)transparentIndex;
            usedCount++;
        }
        if (usedCount < 1) usedCount = 1;

        // 调色板尺寸必须是 2 的幂（2 ~ 256）
        int tableSize = 2;
        int sizeBits = 1;
        while (tableSize < usedCount)
        {
            tableSize <<= 1;
            sizeBits++;
        }

        // 组装最终调色板（RGB，按 tableSize 补齐）
        var palette = new byte[tableSize * 3];
        for (int old = 0; old < quantColors; old++)
        {
            int nw = remap[old];
            if (nw < 0) continue;
            palette[nw * 3] = quantPalette[old * 3];
            palette[nw * 3 + 1] = quantPalette[old * 3 + 1];
            palette[nw * 3 + 2] = quantPalette[old * 3 + 2];
        }
        // 透明索引位置颜色值无所谓（不会被显示），保持 (0,0,0)

        // --- 写文件头（首帧时）--------------------------------------
        if (!_headerWritten)
        {
            WriteHeader(_width, _height, palette, sizeBits);
            _headerWritten = true;
        }

        // --- Graphic Control Extension -------------------------------
        WriteByte(0x21);          // Extension Introducer
        WriteByte(0xF9);          // Graphic Control Label
        WriteByte(4);             // Block Size
        int packed = ((_options.DisposalMethod & 0x07) << 2)
                   | (hasTransparency ? 1 : 0);
        WriteByte(packed);
        WriteShort(delay);        // 延迟（1/100 秒）
        WriteByte(hasTransparency ? transparentIndex : 0);
        WriteByte(0x00);          // Block Terminator

        // --- Image Descriptor + 局部调色板 ----------------------------
        WriteByte(0x2C);          // Image Separator
        WriteShort(0);            // Left
        WriteShort(0);            // Top
        WriteShort(_width);
        WriteShort(_height);
        WriteByte(0x80 | (sizeBits - 1)); // 使用局部调色板
        _stream.Write(palette, 0, palette.Length);

        // --- LZW 压缩像素索引 ----------------------------------------
        int minCodeSize = Math.Max(2, sizeBits);
        new LzwEncoder(_width, _height, indexed, minCodeSize).Encode(_stream);
    }

    // ---------------------------------------------------------------------
    // GIF 头
    // ---------------------------------------------------------------------

    private void WriteHeader(int w, int h, byte[] firstPalette, int sizeBits)
    {
        WriteString("GIF89a");

        // Logical Screen Descriptor：以首帧调色板作为全局调色板
        WriteShort(w);
        WriteShort(h);
        WriteByte(0x80 | 0x70 | (sizeBits - 1)); // GCT 标志 + 颜色分辨率 + GCT 大小
        WriteByte(0); // 背景色索引
        WriteByte(0); // 像素纵横比
        _stream.Write(firstPalette, 0, firstPalette.Length);

        // NETSCAPE2.0 循环扩展
        WriteByte(0x21);          // Extension Introducer
        WriteByte(0xFF);          // Application Extension Label
        WriteByte(11);            // Block Size
        WriteString("NETSCAPE2.0");
        WriteByte(3);             // Sub-block Size
        WriteByte(1);             // Loop Sub-block ID
        WriteShort(_options.LoopCount); // 0 = 无限循环
        WriteByte(0);             // Block Terminator
    }

    // ---------------------------------------------------------------------
    // 基础写字节工具
    // ---------------------------------------------------------------------

    private void WriteByte(int v) => _stream.WriteByte((byte)(v & 0xFF));

    private void WriteShort(int v)
    {
        _stream.WriteByte((byte)(v & 0xFF));
        _stream.WriteByte((byte)((v >> 8) & 0xFF));
    }

    private void WriteString(string s)
    {
        foreach (char c in s)
            _stream.WriteByte((byte)c);
    }
}

// =========================================================================
// NeuQuant 神经网络调色板量化器
// NeuQuant Neural-Net Quantization Algorithm
// Copyright (c) 1994 Anthony Dekker
// NEUQUANT Neural-Net quantization algorithm by Anthony Dekker, 1994.
// See "Kohonen neural networks for optimal colour quantization"
// in "Network: Computation in Neural Systems" Vol. 5 (1994) pp 351-367.
// for a discussion of the algorithm.
// =========================================================================
internal sealed class NeuQuant
{
    private const int Netsize = 256;              // 颜色数
    private const int Prime1 = 499;
    private const int Prime2 = 491;
    private const int Prime3 = 487;
    private const int Prime4 = 503;
    private const int MinPictureBytes = 3 * Prime4;

    private const int MaxNetPos = Netsize - 1;
    private const int NetBiasShift = 4;           // 颜色值偏移
    private const int NCycles = 100;              // 学习循环数

    private const int IntBiasShift = 16;          // 小数偏移
    private const int IntBias = 1 << IntBiasShift;
    private const int GammaShift = 10;
    private const int BetaShift = 10;
    private const int Beta = IntBias >> BetaShift;
    private const int BetaGamma = IntBias << (GammaShift - BetaShift);

    private const int InitRad = Netsize >> 3;
    private const int RadiusBiasShift = 6;
    private const int RadiusBias = 1 << RadiusBiasShift;
    private const int InitRadius = InitRad * RadiusBias;
    private const int RadiusDec = 30;

    private const int AlphaBiasShift = 10;
    private const int InitAlpha = 1 << AlphaBiasShift;

    private const int RadBiasShift = 8;
    private const int RadBias = 1 << RadBiasShift;
    private const int AlphaRadBShift = AlphaBiasShift + RadBiasShift;
    private const int AlphaRadBias = 1 << AlphaRadBShift;

    private readonly byte[] _thePicture;          // 输入图像（RGB 序列）
    private readonly int _lengthCount;            // = 像素数 * 3
    private int _sampleFac;                       // 采样因子 1..30

    private readonly int[][] _network;            // 网络 [netsize][4]
    private readonly int[] _netIndex = new int[256];
    private readonly int[] _bias = new int[Netsize];
    private readonly int[] _freq = new int[Netsize];
    private readonly int[] _radPower = new int[InitRad];

    public NeuQuant(byte[] thePic, int len, int sample)
    {
        _thePicture = thePic;
        _lengthCount = len;
        _sampleFac = sample;

        _network = new int[Netsize][];
        for (int i = 0; i < Netsize; i++)
        {
            _network[i] = new int[4];
            int[] p = _network[i];
            p[0] = p[1] = p[2] = (i << (NetBiasShift + 8)) / Netsize;
            _freq[i] = IntBias / Netsize;
            _bias[i] = 0;
        }
    }

    /// <summary>返回 RGB 调色板（3 * Netsize 字节，按亮度排序）。</summary>
    public byte[] ColorMap()
    {
        var map = new byte[3 * Netsize];
        var index = new int[Netsize];
        for (int i = 0; i < Netsize; i++)
            index[_network[i][3]] = i;
        int k = 0;
        for (int i = 0; i < Netsize; i++)
        {
            int j = index[i];
            map[k++] = (byte)_network[j][0];
            map[k++] = (byte)_network[j][1];
            map[k++] = (byte)_network[j][2];
        }
        return map;
    }

    /// <summary>对网络做插入排序并建立 netindex 查找表。</summary>
    public void Inxbuild()
    {
        int previousCol = 0;
        int startPos = 0;
        for (int i = 0; i < Netsize; i++)
        {
            int[] p = _network[i];
            int smallPos = i;
            int smallVal = p[1]; // 以 g 为索引
            for (int j = i + 1; j < Netsize; j++)
            {
                int[] q = _network[j];
                if (q[1] < smallVal)
                {
                    smallPos = j;
                    smallVal = q[1];
                }
            }
            int[] sp = _network[smallPos];
            if (i != smallPos)
            {
                Swap(sp, p, 0); Swap(sp, p, 1); Swap(sp, p, 2); Swap(sp, p, 3);
            }
            if (smallVal != previousCol)
            {
                _netIndex[previousCol] = (startPos + i) >> 1;
                for (int j = previousCol + 1; j < smallVal; j++)
                    _netIndex[j] = i;
                previousCol = smallVal;
                startPos = i;
            }
        }
        _netIndex[previousCol] = (startPos + MaxNetPos) >> 1;
        for (int j = previousCol + 1; j < 256; j++)
            _netIndex[j] = MaxNetPos;
    }

    private static void Swap(int[] a, int[] b, int idx)
    {
        int t = a[idx]; a[idx] = b[idx]; b[idx] = t;
    }

    /// <summary>主学习循环。</summary>
    public void Learn()
    {
        if (_lengthCount < MinPictureBytes)
            _sampleFac = 1;
        int alphadec = 30 + (_sampleFac - 1) / 3;
        byte[] p = _thePicture;
        int pix = 0;
        int lim = _lengthCount;
        int samplePixels = _lengthCount / (3 * _sampleFac);
        int delta = samplePixels / NCycles;
        if (delta == 0) delta = 1;
        int alpha = InitAlpha;
        int radius = InitRadius;

        int rad = radius >> RadiusBiasShift;
        if (rad <= 1) rad = 0;
        for (int k = 0; k < rad; k++)
            _radPower[k] = alpha * (((rad * rad - k * k) * RadBias) / (rad * rad));

        int step;
        if (_lengthCount < MinPictureBytes)
            step = 3;
        else if (_lengthCount % Prime1 != 0)
            step = 3 * Prime1;
        else if (_lengthCount % Prime2 != 0)
            step = 3 * Prime2;
        else if (_lengthCount % Prime3 != 0)
            step = 3 * Prime3;
        else
            step = 3 * Prime4;

        int i = 0;
        while (i < samplePixels)
        {
            int c0 = (p[pix] & 0xFF) << NetBiasShift;
            int c1 = (p[pix + 1] & 0xFF) << NetBiasShift;
            int c2 = (p[pix + 2] & 0xFF) << NetBiasShift;
            int j = Contest(c0, c1, c2);

            Altersingle(alpha, j, c0, c1, c2);
            if (rad != 0)
                Alterneigh(rad, j, c0, c1, c2);

            pix += step;
            if (pix >= lim) pix -= _lengthCount;

            i++;
            if (i % delta == 0)
            {
                alpha -= alpha / alphadec;
                radius -= radius / RadiusDec;
                rad = radius >> RadiusBiasShift;
                if (rad <= 1) rad = 0;
                for (int k = 0; k < rad; k++)
                    _radPower[k] = alpha * (((rad * rad - k * k) * RadBias) / (rad * rad));
            }
        }
    }

    /// <summary>搜索 RGB 值最接近的调色板索引（网络需已 Unbias + Inxbuild）。</summary>
    public int Inxsearch(int c0, int c1, int c2)
    {
        int bestd = 1000;
        int best = -1;
        int i = _netIndex[c1];
        int j = i - 1;

        while (i < Netsize || j >= 0)
        {
            if (i < Netsize)
            {
                int[] p = _network[i];
                int dist = p[1] - c1;
                if (dist >= bestd)
                    i = Netsize;
                else
                {
                    i++;
                    if (dist < 0) dist = -dist;
                    int a = p[0] - c0;
                    if (a < 0) a = -a;
                    dist += a;
                    if (dist < bestd)
                    {
                        a = p[2] - c2;
                        if (a < 0) a = -a;
                        dist += a;
                        if (dist < bestd)
                        {
                            bestd = dist;
                            best = p[3];
                        }
                    }
                }
            }
            if (j >= 0)
            {
                int[] p = _network[j];
                int dist = c1 - p[1];
                if (dist >= bestd)
                    j = -1;
                else
                {
                    j--;
                    if (dist < 0) dist = -dist;
                    int a = p[0] - c0;
                    if (a < 0) a = -a;
                    dist += a;
                    if (dist < bestd)
                    {
                        a = p[2] - c2;
                        if (a < 0) a = -a;
                        dist += a;
                        if (dist < bestd)
                        {
                            bestd = dist;
                            best = p[3];
                        }
                    }
                }
            }
        }
        return best < 0 ? 0 : best;
    }

    /// <summary>学习 + 去偏 + 建索引，返回调色板。</summary>
    public byte[] Process()
    {
        Learn();
        Unbiasnet();
        Inxbuild();
        return ColorMap();
    }

    /// <summary>去偏：把网络值还原为 0..255，并记录原始序号。</summary>
    public void Unbiasnet()
    {
        for (int i = 0; i < Netsize; i++)
        {
            _network[i][0] >>= NetBiasShift;
            _network[i][1] >>= NetBiasShift;
            _network[i][2] >>= NetBiasShift;
            _network[i][3] = i;
        }
    }

    /// <summary>按 radpower 预计算值调整邻近神经元。</summary>
    private void Alterneigh(int rad, int i, int c0, int c1, int c2)
    {
        int lo = i - rad;
        if (lo < -1) lo = -1;
        int hi = i + rad;
        if (hi > Netsize) hi = Netsize;

        int j = i + 1;
        int k = i - 1;
        int m = 1;
        while (j < hi || k > lo)
        {
            int a = _radPower[m++];
            if (j < hi)
            {
                int[] p = _network[j++];
                p[0] -= a * (p[0] - c0) / AlphaRadBias;
                p[1] -= a * (p[1] - c1) / AlphaRadBias;
                p[2] -= a * (p[2] - c2) / AlphaRadBias;
            }
            if (k > lo)
            {
                int[] p = _network[k--];
                p[0] -= a * (p[0] - c0) / AlphaRadBias;
                p[1] -= a * (p[1] - c1) / AlphaRadBias;
                p[2] -= a * (p[2] - c2) / AlphaRadBias;
            }
        }
    }

    /// <summary>把命中的神经元向 (c0,c1,c2) 移动 alpha 因子。</summary>
    private void Altersingle(int alpha, int i, int c0, int c1, int c2)
    {
        int[] n = _network[i];
        n[0] -= alpha * (n[0] - c0) / InitAlpha;
        n[1] -= alpha * (n[1] - c1) / InitAlpha;
        n[2] -= alpha * (n[2] - c2) / InitAlpha;
    }

    /// <summary>寻找最近神经元（带频率偏差修正）。</summary>
    private int Contest(int c0, int c1, int c2)
    {
        int bestd = int.MaxValue;
        int bestBiasD = int.MaxValue;
        int bestPos = -1;
        int bestBiasPos = -1;

        for (int i = 0; i < Netsize; i++)
        {
            int[] n = _network[i];
            int dist = n[0] - c0;
            if (dist < 0) dist = -dist;
            int a = n[1] - c1;
            if (a < 0) a = -a;
            dist += a;
            a = n[2] - c2;
            if (a < 0) a = -a;
            dist += a;
            if (dist < bestd)
            {
                bestd = dist;
                bestPos = i;
            }
            int biasDist = dist - (_bias[i] >> (IntBiasShift - NetBiasShift));
            if (biasDist < bestBiasD)
            {
                bestBiasD = biasDist;
                bestBiasPos = i;
            }
            int betaFreq = _freq[i] >> BetaShift;
            _freq[i] -= betaFreq;
            _bias[i] += betaFreq << GammaShift;
        }
        _freq[bestPos] += Beta;
        _bias[bestPos] -= BetaGamma;
        return bestBiasPos;
    }
}

// =========================================================================
// GIF LZW 压缩器（经典实现移植）
// =========================================================================
internal sealed class LzwEncoder
{
    private const int Eof = -1;
    private const int Bits = 12;          // 最大码长
    private const int HSize = 5003;       // 哈希表大小（80% 占用率）
    private const int MaxMaxCode = 1 << Bits;

    private readonly int _imgW;
    private readonly int _imgH;
    private readonly byte[] _pixAry;
    private readonly int _initCodeSize;

    private int _remaining;
    private int _curPixel;

    private int _nBits;
    private int _maxCode;
    private readonly int[] _htab = new int[HSize];
    private readonly int[] _codeTab = new int[HSize];
    private int _freeEnt;
    private bool _clearFlg;
    private int _gInitBits;
    private int _clearCode;
    private int _eofCode;

    private int _curAccum;
    private int _curBits;

    private static readonly int[] Masks =
    {
            0x0000, 0x0001, 0x0003, 0x0007, 0x000F,
            0x001F, 0x003F, 0x007F, 0x00FF,
            0x01FF, 0x03FF, 0x07FF, 0x0FFF,
            0x1FFF, 0x3FFF, 0x7FFF, 0xFFFF
        };

    private int _aCount;
    private readonly byte[] _accum = new byte[256];

    public LzwEncoder(int width, int height, byte[] pixels, int colorDepth)
    {
        _imgW = width;
        _imgH = height;
        _pixAry = pixels;
        _initCodeSize = Math.Max(2, colorDepth);
    }

    public void Encode(Stream os)
    {
        os.WriteByte((byte)_initCodeSize); // 最小码长
        _remaining = _imgW * _imgH;
        _curPixel = 0;
        Compress(_initCodeSize + 1, os);
        os.WriteByte(0); // 块结束符
    }

    private void Compress(int initBits, Stream outs)
    {
        _gInitBits = initBits;
        _clearFlg = false;
        _nBits = _gInitBits;
        _maxCode = MaxCode(_nBits);

        _clearCode = 1 << (initBits - 1);
        _eofCode = _clearCode + 1;
        _freeEnt = _clearCode + 2;

        _aCount = 0;

        int ent = NextPixel();

        int hshift = 0;
        for (int fcode = HSize; fcode < 65536; fcode *= 2)
            ++hshift;
        hshift = 8 - hshift;

        ResetCodeTable(HSize);

        Output(_clearCode, outs);

        int c;
        while ((c = NextPixel()) != Eof)
        {
            int fcode = (c << Bits) + ent;
            int i = (c << hshift) ^ ent;

            if (_htab[i] == fcode)
            {
                ent = _codeTab[i];
                continue;
            }
            if (_htab[i] >= 0)
            {
                int disp = HSize - i;
                if (i == 0) disp = 1;
                do
                {
                    if ((i -= disp) < 0)
                        i += HSize;
                    if (_htab[i] == fcode)
                    {
                        ent = _codeTab[i];
                        goto next;
                    }
                } while (_htab[i] >= 0);
            }
            Output(ent, outs);
            ent = c;
            if (_freeEnt < MaxMaxCode)
            {
                _codeTab[i] = _freeEnt++;
                _htab[i] = fcode;
            }
            else
            {
                ClearTable(outs);
            }
        next:;
        }

        Output(ent, outs);
        Output(_eofCode, outs);
    }

    private void ClearTable(Stream outs)
    {
        ResetCodeTable(HSize);
        _freeEnt = _clearCode + 2;
        _clearFlg = true;
        Output(_clearCode, outs);
    }

    private void ResetCodeTable(int hsize)
    {
        for (int i = 0; i < hsize; ++i)
            _htab[i] = -1;
    }

    private int NextPixel()
    {
        if (_remaining == 0) return Eof;
        --_remaining;
        return _pixAry[_curPixel++] & 0xFF;
    }

    private void Add(byte b, Stream outs)
    {
        _accum[_aCount++] = b;
        if (_aCount >= 254)
            FlushPacket(outs);
    }

    private void FlushPacket(Stream outs)
    {
        if (_aCount > 0)
        {
            outs.WriteByte((byte)_aCount);
            outs.Write(_accum, 0, _aCount);
            _aCount = 0;
        }
    }

    private static int MaxCode(int nBits) => (1 << nBits) - 1;

    private void Output(int code, Stream outs)
    {
        _curAccum &= Masks[_curBits];

        if (_curBits > 0)
            _curAccum |= code << _curBits;
        else
            _curAccum = code;

        _curBits += _nBits;

        while (_curBits >= 8)
        {
            Add((byte)(_curAccum & 0xFF), outs);
            _curAccum >>= 8;
            _curBits -= 8;
        }

        if (_freeEnt > _maxCode || _clearFlg)
        {
            if (_clearFlg)
            {
                _maxCode = MaxCode(_nBits = _gInitBits);
                _clearFlg = false;
            }
            else
            {
                ++_nBits;
                _maxCode = _nBits == Bits ? MaxMaxCode : MaxCode(_nBits);
            }
        }

        if (code == _eofCode)
        {
            while (_curBits > 0)
            {
                Add((byte)(_curAccum & 0xFF), outs);
                _curAccum >>= 8;
                _curBits -= 8;
            }
            FlushPacket(outs);
        }
    }
}