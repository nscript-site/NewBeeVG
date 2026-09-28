// 本文件由豆包生成

using SkiaSharp;

namespace NewBeeVG.Encoders;

/// <summary>
/// GIF 编码选项。
/// </summary>
public sealed class GifEncoderOptions
{
    /// <summary>调色板颜色数（全局调色板），2~256，越小文件越小但色偏越明显。</summary>
    public int MaxColors { get; set; } = 256;

    /// <summary>每帧延迟（1/100 秒为单位）。例如 60fps 传 1，10fps 传 10。</summary>
    public int DelayHundredths { get; set; } = 10;

    /// <summary>循环次数，0 = 无限循环。</summary>
    public int RepeatCount { get; set; } = 0;

    /// <summary>是否启用帧差优化：只编码与上一帧不同的矩形区域，可显著减小体积。</summary>
    public bool UseFrameDiff { get; set; } = true;

    /// <summary>是否启用 Floyd–Steinberg 抖动（颜色少时更平滑，但会略增数据量）。</summary>
    public bool UseDithering { get; set; } = true;

    /// <summary>
    /// 目标缩放比例（1.0 = 原尺寸，0.5 = 长宽各减半）。
    /// 在编码前对每帧做高质量缩放，是"压缩尺寸"最直接的手段。
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>缩放质量。</summary>
    public SKFilterQuality ScaleQuality { get; set; } = SKFilterQuality.Medium;
}

/// <summary>
/// 流式 GIF89a 编码器：基于 SkiaSharp，逐帧 AddFrame，Dispose 时收尾。
///
/// 用法：
/// <code>
/// using var gif = GifEncoder.Create("out.gif", delayHundredths: 5);
/// for (...) {
///     using var bmp = RenderNextFrame();
///     gif.AddFrame(bmp);
/// }
/// </code>
/// </summary>
public sealed class GifEncoder : IDisposable
{
    private readonly Stream _out;
    private readonly bool _leaveOpen;
    private readonly GifEncoderOptions _opt;
    private readonly BitWriter _bw;

    private bool _headerWritten;
    private bool _disposed;

    // 第一帧到达后才确定
    private int _width, _height;
    private byte[] _palette = Array.Empty<byte>();
    private int _paletteEntries;
    private int _gctSize;
    private byte _minCodeSize;

    // 只保留上一帧的索引图，做帧差用
    private byte[]? _prevIndices;

    // ---- 构造 / 工厂 -------------------------------------------------

    private GifEncoder(Stream output, GifEncoderOptions? options, bool leaveOpen)
    {
        _out = output ?? throw new ArgumentNullException(nameof(output));
        _opt = options ?? new GifEncoderOptions();
        _leaveOpen = leaveOpen;
        _bw = new BitWriter(_out);
    }

    /// <summary>创建一个编码器，写入指定文件。</summary>
    /// <param name="filePath">输出 .gif 路径。</param>
    /// <param name="delayHundredths">每帧延迟（1/100 秒为单位）。</param>
    /// <param name="options">可选编码参数（Scale/MaxColors/帧差/抖动等）。</param>
    public static GifEncoder Create(string filePath, int delayHundredths, GifEncoderOptions? options = null)
    {
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("filePath 不能为空", nameof(filePath));
        var fs = File.Create(filePath);
        var opt = options == null ? new GifEncoderOptions() : Clone(options);
        opt.DelayHundredths = delayHundredths;
        return new GifEncoder(fs, opt, leaveOpen: false);
    }

    /// <summary>创建一个编码器，写入给定流。</summary>
    /// <param name="output">输出流。</param>
    /// <param name="options">编码参数。</param>
    /// <param name="leaveOpen">Dispose 时是否保留底层流不关闭。</param>
    public static GifEncoder Create(Stream output, GifEncoderOptions? options, bool leaveOpen = false)
        => new GifEncoder(output, options, leaveOpen);

    private static GifEncoderOptions Clone(GifEncoderOptions o) => new()
    {
        MaxColors = o.MaxColors,
        DelayHundredths = o.DelayHundredths,
        RepeatCount = o.RepeatCount,
        UseFrameDiff = o.UseFrameDiff,
        UseDithering = o.UseDithering,
        Scale = o.Scale,
        ScaleQuality = o.ScaleQuality,
    };

    // ---- 主接口 ------------------------------------------------------

    /// <summary>添加一帧。所有帧尺寸必须一致。</summary>
    public void AddFrame(SKBitmap frame)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GifEncoder));
        if (frame == null) throw new ArgumentNullException(nameof(frame));

        // 1) 规范化：缩放 + 统一 Bgra8888/Unpremul
        using var prepared = Prepare(frame);

        if (!_headerWritten)
        {
            _width = prepared.Width;
            _height = prepared.Height;

            // 2) 用第一帧建全局调色板
            int colorCount = Math.Clamp(_opt.MaxColors, 2, 256);
            _palette = BuildPaletteFromFrame(prepared, colorCount, out colorCount);
            _gctSize = 1;
            while ((1 << _gctSize) < colorCount) _gctSize++;
            _paletteEntries = 1 << _gctSize;
            _minCodeSize = (byte)Math.Max(2, Math.Min(8, _gctSize));

            WriteHeader();
            _headerWritten = true;
        }
        else
        {
            if (prepared.Width != _width || prepared.Height != _height)
                throw new ArgumentException($"帧尺寸不一致：期望 {_width}x{_height}，实际 {prepared.Width}x{prepared.Height}。");
        }

        // 3) 量化成索引图
        var indices = Quantize(prepared, _palette, _paletteEntries, _opt.UseDithering);

        // 4) 写这一帧
        WriteFrame(indices);

        // 5) 保留本帧索引图供下一帧做差
        _prevIndices = indices;
    }

    /// <summary>结束编码。必须调用（或用 using）。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_headerWritten)
        {
            _bw.WriteByte(0x3B); // Trailer
            _bw.FlushBits();
        }
        if (!_leaveOpen) _out.Dispose();
    }

    // ---- 内部：准备帧 ------------------------------------------------

    private SKBitmap Prepare(SKBitmap src)
    {
        if (Math.Abs(_opt.Scale - 1.0f) > 1e-3f)
        {
            int nw = Math.Max(1, (int)(src.Width * _opt.Scale));
            int nh = Math.Max(1, (int)(src.Height * _opt.Scale));
            var info = new SKImageInfo(nw, nh, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            var scaled = src.Resize(info, _opt.ScaleQuality);
            if (scaled == null) throw new InvalidOperationException("缩放失败。");
            return scaled;
        }

        var info0 = new SKImageInfo(src.Width, src.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var conv = new SKBitmap(info0);
        using (var canvas = new SKCanvas(conv))
        {
            canvas.DrawBitmap(src, 0, 0);
        }
        return conv;
    }

    // ---- 内部：写头 --------------------------------------------------

    private void WriteHeader()
    {
        _bw.WriteAscii("GIF89a");
        _bw.WriteU16((ushort)_width);
        _bw.WriteU16((ushort)_height);
        byte packed = (byte)(0x80 | ((byte)(_gctSize - 1) & 0x07) | 0x70);
        _bw.WriteByte(packed);
        _bw.WriteByte(0); // 背景色索引
        _bw.WriteByte(0); // 宽高比

        for (int i = 0; i < _paletteEntries; i++)
        {
            int o = i * 3;
            _bw.WriteByte(o < _palette.Length ? _palette[o] : (byte)0);
            _bw.WriteByte(o + 1 < _palette.Length ? _palette[o + 1] : (byte)0);
            _bw.WriteByte(o + 2 < _palette.Length ? _palette[o + 2] : (byte)0);
        }

        if (_opt.RepeatCount >= 0)
        {
            _bw.WriteByte(0x21);
            _bw.WriteByte(0xFF);
            _bw.WriteByte(11);
            _bw.WriteAscii("NETSCAPE2.0");
            _bw.WriteByte(3);
            _bw.WriteByte(1);
            _bw.WriteU16((ushort)_opt.RepeatCount);
            _bw.WriteByte(0);
        }
    }

    // ---- 内部：写一帧 ------------------------------------------------

    private void WriteFrame(byte[] indices)
    {
        int left = 0, top = 0, w = _width, h = _height;

        if (_opt.UseFrameDiff && _prevIndices != null)
        {
            FindChangedRect(indices, _prevIndices, _width, _height, out left, out top, out w, out h);
            if (w <= 0 || h <= 0)
            {
                // 与上一帧完全相同：写 1x1 占位，像素值取 (0,0) 实际值，不改变画面
                left = top = 0;
                w = h = 1;
            }
        }

        // Graphic Control Extension
        _bw.WriteByte(0x21);
        _bw.WriteByte(0xF9);
        _bw.WriteByte(4);
        _bw.WriteByte(0x04); // disposal = 1 (do not dispose)
        _bw.WriteU16((ushort)Math.Max(0, _opt.DelayHundredths));
        _bw.WriteByte(0);    // 透明色索引（未使用）
        _bw.WriteByte(0);    // block terminator

        // Image Descriptor
        _bw.WriteByte(0x2C);
        _bw.WriteU16((ushort)left);
        _bw.WriteU16((ushort)top);
        _bw.WriteU16((ushort)w);
        _bw.WriteU16((ushort)h);
        _bw.WriteByte(0);
        _bw.WriteByte(_minCodeSize);

        var row = new byte[w];
        var lzw = new LzwWriter(_bw, _minCodeSize);
        lzw.Begin();
        for (int y = 0; y < h; y++)
        {
            int srcRow = (top + y) * _width + left;
            Buffer.BlockCopy(indices, srcRow, row, 0, w);
            for (int x = 0; x < w; x++) lzw.WritePixel(row[x]);
        }
        lzw.Finish();
    }

    // ---- 调色板（中值切割，基于单帧采样） ----------------------------

    private static unsafe byte[] BuildPaletteFromFrame(SKBitmap bmp, int maxColors, out int actualCount)
    {
        int w = bmp.Width, h = bmp.Height;
        long total = (long)w * h;
        int step = total > 200_000 ? 2 : 1;

        var pixels = new List<(byte r, byte g, byte b)>(Math.Min((int)(total / step), 100_000));
        IntPtr ptr = bmp.GetPixels();
        int rowBytes = bmp.RowBytes;
        for (int y = 0; y < h; y += step)
        {
            byte* row = (byte*)ptr + y * rowBytes;
            for (int x = 0; x < w; x += step)
            {
                pixels.Add((row[x * 4 + 2], row[x * 4 + 1], row[x * 4]));
            }
        }

        var buckets = new List<List<(byte r, byte g, byte b)>> { pixels };
        int target = Math.Max(2, maxColors);
        while (buckets.Count < target)
        {
            int best = -1, bestExtent = -1;
            for (int i = 0; i < buckets.Count; i++)
            {
                var b = buckets[i];
                if (b.Count < 2) continue;
                ComputeExtent(b, out int rMin, out int rMax, out int gMin, out int gMax, out int bMin, out int bMax);
                int ext = Math.Max(rMax - rMin, Math.Max(gMax - gMin, bMax - bMin));
                if (ext > bestExtent) { bestExtent = ext; best = i; }
            }
            if (best < 0 || bestExtent <= 0) break;

            {
                var split = buckets[best];
                ComputeExtent(split, out int srMin, out int srMax, out int sgMin, out int sgMax, out int sbMin, out int sbMax);
                string axis = "r";
                int ext = srMax - srMin;
                if (sgMax - sgMin > ext) { axis = "g"; ext = sgMax - sgMin; }
                if (sbMax - sbMin > ext) { axis = "b"; }

                if (axis == "r") split.Sort((a, c) => a.r.CompareTo(c.r));
                else if (axis == "g") split.Sort((a, c) => a.g.CompareTo(c.g));
                else split.Sort((a, c) => a.b.CompareTo(c.b));

                int mid = split.Count / 2;
                var b1 = split.GetRange(0, mid);
                var b2 = split.GetRange(mid, split.Count - mid);
                buckets.RemoveAt(best);
                buckets.Add(b1);
                buckets.Add(b2);
            }
        }

        var palette = new byte[buckets.Count * 3];
        for (int i = 0; i < buckets.Count; i++)
        {
            long rs = 0, gs = 0, bs = 0;
            foreach (var p in buckets[i]) { rs += p.r; gs += p.g; bs += p.b; }
            int n = Math.Max(1, buckets[i].Count);
            palette[i * 3] = (byte)(rs / n);
            palette[i * 3 + 1] = (byte)(gs / n);
            palette[i * 3 + 2] = (byte)(bs / n);
        }
        actualCount = buckets.Count;
        return palette;
    }

    private static void ComputeExtent(List<(byte r, byte g, byte b)> bucket,
        out int rMin, out int rMax, out int gMin, out int gMax, out int bMin, out int bMax)
    {
        rMin = 255; rMax = 0; gMin = 255; gMax = 0; bMin = 255; bMax = 0;
        foreach (var p in bucket)
        {
            if (p.r < rMin) rMin = p.r; if (p.r > rMax) rMax = p.r;
            if (p.g < gMin) gMin = p.g; if (p.g > gMax) gMax = p.g;
            if (p.b < bMin) bMin = p.b; if (p.b > bMax) bMax = p.b;
        }
    }

    // ---- 量化 --------------------------------------------------------

    private static unsafe byte[] Quantize(SKBitmap bmp, byte[] palette, int paletteEntries, bool dither)
    {
        int w = bmp.Width, h = bmp.Height;
        var idx = new byte[w * h];
        IntPtr ptr = bmp.GetPixels();
        int rowBytes = bmp.RowBytes;

        if (!dither)
        {
            for (int y = 0; y < h; y++)
            {
                byte* row = (byte*)ptr + y * rowBytes;
                for (int x = 0; x < w; x++)
                {
                    byte r = row[x * 4 + 2], g = row[x * 4 + 1], b = row[x * 4];
                    idx[y * w + x] = (byte)NearestColor(palette, paletteEntries, r, g, b);
                }
            }
            return idx;
        }

        // Floyd–Steinberg，误差定点 *16
        var errR = new int[w + 1];
        var errG = new int[w + 1];
        var errB = new int[w + 1];
        for (int y = 0; y < h; y++)
        {
            byte* row = (byte*)ptr + y * rowBytes;
            var nextR = new int[w + 1];
            var nextG = new int[w + 1];
            var nextB = new int[w + 1];
            for (int x = 0; x < w; x++)
            {
                byte r = row[x * 4 + 2], g = row[x * 4 + 1], b = row[x * 4];
                int cr = ClampByte(r + (errR[x] >> 4));
                int cg = ClampByte(g + (errG[x] >> 4));
                int cb = ClampByte(b + (errB[x] >> 4));

                int best = NearestColor(palette, paletteEntries, (byte)cr, (byte)cg, (byte)cb);
                idx[y * w + x] = (byte)best;

                int er = cr - palette[best * 3];
                int eg = cg - palette[best * 3 + 1];
                int eb = cb - palette[best * 3 + 2];

                if (x + 1 < w) { errR[x + 1] += er * 7; errG[x + 1] += eg * 7; errB[x + 1] += eb * 7; }
                nextR[x] += er * 3; nextG[x] += eg * 3; nextB[x] += eb * 3;
                nextR[x] += er * 5; nextG[x] += eg * 5; nextB[x] += eb * 5;
                if (x + 1 < w) { nextR[x + 1] += er; nextG[x + 1] += eg; nextB[x + 1] += eb; }
            }
            errR = nextR; errG = nextG; errB = nextB;
        }
        return idx;
    }

    private static int NearestColor(byte[] palette, int count, byte r, byte g, byte b)
    {
        int best = 0;
        long bestDist = long.MaxValue;
        for (int i = 0; i < count; i++)
        {
            long dr = r - palette[i * 3];
            long dg = g - palette[i * 3 + 1];
            long db = b - palette[i * 3 + 2];
            long d = dr * dr + dg * dg + db * db;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private static byte ClampByte(int v) => v < 0 ? (byte)0 : v > 255 ? (byte)255 : (byte)v;

    private static void FindChangedRect(byte[] cur, byte[] prev, int w, int h,
        out int left, out int top, out int cw, out int ch)
    {
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            int rowOff = y * w;
            for (int x = 0; x < w; x++)
            {
                if (cur[rowOff + x] != prev[rowOff + x])
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
        left = minX; top = minY;
        cw = maxX < 0 ? 0 : maxX - minX + 1;
        ch = maxY < 0 ? 0 : maxY - minY + 1;
    }

    // ---- 位写入器 ----------------------------------------------------

    internal sealed class BitWriter
    {
        private readonly Stream _s;
        private int _cur;
        private int _nbits;
        private readonly List<byte> _block = new List<byte>(256);

        public BitWriter(Stream s) { _s = s; }

        public void WriteByte(byte b) { FlushBits(); _s.WriteByte(b); }
        public void WriteU16(ushort v) { WriteByte((byte)(v & 0xFF)); WriteByte((byte)(v >> 8)); }
        public void WriteAscii(string s) { foreach (var c in s) WriteByte((byte)c); }

        public void WriteBits(int value, int count)
        {
            _cur |= (value << _nbits);
            _nbits += count;
            while (_nbits >= 8)
            {
                byte b = (byte)(_cur & 0xFF);
                _cur >>= 8;
                _nbits -= 8;
                _block.Add(b);
                if (_block.Count == 255) FlushBlock();
            }
        }

        public void FlushBlock()
        {
            if (_block.Count == 0) return;
            _s.WriteByte((byte)_block.Count);
            foreach (var b in _block) _s.WriteByte(b);
            _block.Clear();
        }

        public void FlushBits()
        {
            if (_nbits > 0)
            {
                _block.Add((byte)(_cur & 0xFF));
                _cur = 0; _nbits = 0;
                FlushBlock();
            }
        }
    }

    // ---- LZW ---------------------------------------------------------

    internal sealed class LzwWriter
    {
        private readonly BitWriter _bw;
        private readonly int _minCodeSize;
        private readonly int _clearCode;
        private readonly int _endCode;
        private int _codeSize;
        private int _next;
        private Dictionary<int, int> _dict = new();
        private int _prev = -1;

        public LzwWriter(BitWriter bw, int minCodeSize)
        {
            _bw = bw;
            _minCodeSize = minCodeSize;
            _clearCode = 1 << minCodeSize;
            _endCode = _clearCode + 1;
        }

        public void Begin()
        {
            _codeSize = _minCodeSize + 1;
            _next = _endCode + 1;
            _dict.Clear();
            _prev = -1;
            _bw.WriteBits(_clearCode, _codeSize);
        }

        public void WritePixel(byte pixel)
        {
            if (_prev < 0) { _prev = pixel; return; }
            int key = (_prev << 8) | pixel;
            if (_dict.TryGetValue(key, out int code))
            {
                _prev = code;
            }
            else
            {
                _bw.WriteBits(_prev, _codeSize);
                _dict[key] = _next;
                _next++;
                if (_next > (1 << _codeSize) && _codeSize < 12) _codeSize++;
                if (_next == 4096)
                {
                    _bw.WriteBits(_clearCode, _codeSize);
                    _codeSize = _minCodeSize + 1;
                    _next = _endCode + 1;
                    _dict.Clear();
                }
                _prev = pixel;
            }
        }

        public void Finish()
        {
            if (_prev >= 0) _bw.WriteBits(_prev, _codeSize);
            _bw.WriteBits(_endCode, _codeSize);
            _bw.FlushBits();
            _bw.WriteByte(0);
        }
    }
}