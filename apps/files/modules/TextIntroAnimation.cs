// ---- 换一套长相，只改上面几行就够。例如竖屏 9:16 ----
//   Width = 1080, Height = 1920, Duration = 4.0f,
//   Title = new[] { "一张纸", "撑起了一个" }, Accent = "资本帝国",
//   Eyebrow = "AMSTERDAM · 1602", Caption = "全球股票市值 · 已超 100 万亿美元",
//   Background = new SKColor(0x16, 0x11, 0x0C),
//   Gold = new SKColor(0xD9, 0xA4, 0x41),
//   AccentColor = new SKColor(0xE8, 0x8B, 0x4A),
//   OutputPath = "output/titlecard_9x16.mp4",
//
// 版式不用管：宽高比一变，左右分栏会自动变成上图下文。

// ============================================================
//  规格：全部可定制项都在这里，带默认值
// ============================================================

// ============================================================
//  几何：所有位置由画幅与字号推导，行数与横竖屏都不写死
// ============================================================

// ============================================================
//  渲染器
// ============================================================
public sealed class TitleCard : SKCustomAnimation, IDisposable
{
    public sealed class Spec
    {
        // ---------- 画幅 ----------
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public int Fps { get; set; } = 25;
        public float Duration { get; set; } = 3.0f;
        /// <summary>直接指定帧数；0 表示由 Duration × Fps 推导</summary>
        public int Frames { get; set; } = 0;

        // ---------- 图像 ----------
        public string Image { get; set; } = "oldest_stock_02.png";
        /// <summary>取原图的上/下边界（0–1）。下半是空白纸时用 0.58 之类裁掉。</summary>
        public float CropTop { get; set; } = 0f;
        public float CropBottom { get; set; } = 1f;
        /// <summary>宽屏布局下卡片高度占画幅高度的比例</summary>
        public float CardScale { get; set; } = 0.537f;
        /// <summary>宽屏布局下卡片宽度上限（占画幅宽）</summary>
        public float CardWidthCap { get; set; } = 0.42f;
        public float CardRadius { get; set; } = 8f;
        public bool ShowBrackets { get; set; } = true;   // 四角档案框线
        public bool ShowShadow { get; set; } = true;     // 卡片投影
        /// <summary>纸面暖调（Multiply 压回羊皮纸色）；Alpha 为 0 表示不做</summary>
        public SKColor CardTint { get; set; } = new(0xFF, 0xEE, 0xCF, 0x80);
        public float CardTintAlpha { get; set; } = 0.50f;

        // ---------- 文字 ----------
        public string[] Title { get; set; } = { "世界上最古老的" };
        /// <summary>金色强调词，用更大字号渲染；空字符串则跳过</summary>
        public string Accent { get; set; } = "股票";
        public string Eyebrow { get; set; } = "1606 · ENKHUIZEN";
        public string Caption { get; set; } = "联合东印度公司 · 目前已知最古老的股票凭证";
        public bool CenterAlign { get; set; } = false;

        // ---------- 版式 ----------
        public Layout Mode { get; set; } = Layout.Auto;
        /// <summary>宽屏布局下图像放右边（文字在左）</summary>
        public bool ImageOnRight { get; set; } = false;
        public bool ShowDivider { get; set; } = true;    // 中缝竖线

        // ---------- 背景色 ----------
        /// <summary>背景主色。只改这一个，光晕与边缘会按它自动派生。</summary>
        public SKColor Background { get; set; } = new(0x0A, 0x14, 0x20);
        /// <summary>背景径向光晕（画面中心偏亮处）；null = 由主色提亮派生</summary>
        public SKColor? BackgroundGlow { get; set; }
        /// <summary>背景边缘（暗角外侧）；null = 由主色压暗派生</summary>
        public SKColor? BackgroundEdge { get; set; }

        // ---------- 装饰色：框线、边线、下划线、四角 bracket ----------
        public SKColor Gold { get; set; } = new(0xC2, 0x91, 0x4E);

        // ---------- 文字颜色 ----------
        /// <summary>标题主色。只改这一个，整行文字的色阶就会跟着重建。</summary>
        public SKColor TitleLight { get; set; } = new(0xEF, 0xE6, 0xD2);
        /// <summary>标题中部色（渐变中段）；null = 由 TitleLight 与强调色混合派生</summary>
        public SKColor? TitleMid { get; set; }
        /// <summary>标题下缘色（渐变末端）；null = 由中段色压暗派生</summary>
        public SKColor? TitleDark { get; set; }

        /// <summary>强调词主色；null = 跟随 Gold</summary>
        public SKColor? AccentColor { get; set; }
        /// <summary>强调词渐变亮部；null = 由主色提亮派生</summary>
        public SKColor? AccentLight { get; set; }
        /// <summary>强调词渐变暗部；null = 由主色压暗派生</summary>
        public SKColor? AccentDark { get; set; }

        /// <summary>eyebrow 小字颜色</summary>
        public SKColor EyebrowColor { get; set; } = new(0x4E, 0x8F, 0xA0);
        /// <summary>说明行颜色</summary>
        public SKColor CaptionColor { get; set; } = new(0xE8, 0xE4, 0xDB);
        /// <summary>扫过文字的高光颜色</summary>
        public SKColor SweepColor { get; set; } = new(0xFF, 0xF5, 0xE0);

        // 派生结果（Normalize 填充，供渲染内部使用）
        internal SKColor R_BgGlow, R_BgEdge;
        internal SKColor R_TitleMid, R_TitleDark;
        internal SKColor R_Accent, R_AccentLight, R_AccentDark;

        // ---------- 字体 ----------
        /// <summary>衬线候选，按顺序试；找不到会自动回退</summary>
        public string[] SerifFonts { get; set; } = { "Source Han Serif SC", "Noto Serif SC", "思源宋体", "SimSun" };
        public string[] SansFonts { get; set; } = { "Bahnschrift", "Segoe UI", "Consolas" };
        /// <summary>正文字号占画幅高的比例（88px @1080）；宽度不够时会自动缩小</summary>
        public float FontScale { get; set; } = 0.0815f;

        // ---------- 质感 ----------
        public float Grain { get; set; } = 0.57f;      // 0–1
        public float Vignette { get; set; } = 0.61f;   // 0–1
        public int Seed { get; set; } = 1606;

        // ---------- 动画 ----------
        /// <summary>入场节奏倍率；0 = 跟随 Duration 自动（3 秒 → 1.0）</summary>
        public float TimingScale { get; set; } = 0f;

        // ---------- 输出 ----------
        public string OutputPath { get; set; } = "output/titlecard.mp4";
        public bool ExportGif { get; set; } = false;
        /// <summary>导出几张 PNG 预览帧（0 = 不导）</summary>
        public int PreviewFrames { get; set; } = 5;
        /// <summary>只渲染指定帧为 PNG、不导出视频；-1 表示正常出片（调版式时最快）</summary>
        public int StillFrame { get; set; } = -1;
        public bool Verbose { get; set; } = true;

        internal void Normalize()
        {
            if (Width <= 0) Width = 1920;
            if (Height <= 0) Height = 1080;
            if (Fps <= 0) Fps = 25;
            if (Duration <= 0f) Duration = 3f;
            if (Frames <= 0) Frames = Math.Max(2, (int)MathF.Round(Duration * Fps));
            if (CropBottom <= 0f || CropBottom > 1f) CropBottom = 1f;
            if (CropTop < 0f || CropTop >= CropBottom) CropTop = 0f;
            if (Title is null || Title.Length == 0) Title = new[] { " " };
            Title = Title.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
            if (Title.Length == 0) Title = new[] { " " };
            Accent ??= "";
            Eyebrow ??= "";
            Caption ??= "";
            if (SerifFonts is null || SerifFonts.Length == 0) SerifFonts = new[] { "SimSun" };
            if (SansFonts is null || SansFonts.Length == 0) SansFonts = new[] { "Segoe UI" };

            // 文字色阶：只给主色，其余按色相关系自动派生，避免出现「标题换了、下划线没换」
            R_BgGlow = BackgroundGlow ?? Lighten(Background, 0.22f);
            R_BgEdge = BackgroundEdge ?? Darken(Background, 0.55f);
            R_Accent = AccentColor ?? Gold;
            R_AccentLight = AccentLight ?? Lighten(R_Accent, 0.30f);
            R_AccentDark = AccentDark ?? Darken(R_Accent, 0.28f);
            R_TitleMid = TitleMid ?? Mix(TitleLight, R_Accent, 0.16f);
            R_TitleDark = TitleDark ?? Darken(R_TitleMid, 0.16f);
        }

        // ---------- 颜色工具（配置时可直接用）----------
        public static SKColor Mix(SKColor a, SKColor b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new SKColor(
                (byte)(a.Red + (b.Red - a.Red) * t),
                (byte)(a.Green + (b.Green - a.Green) * t),
                (byte)(a.Blue + (b.Blue - a.Blue) * t),
                a.Alpha);
        }
        public static SKColor Lighten(SKColor c, float t) => Mix(c, SKColors.White, t);
        public static SKColor Darken(SKColor c, float t) => Mix(c, SKColors.Black, t);
    }

    public enum Layout { Auto, Side, Stack }

    private sealed class Geo
    {
        public bool Stack, Mirror;
        public SKRect Card;
        public float CardW;
        public float BlockX, AvailW, MaxLineW, BlockH;
        public float VX;
        public float Fs1, Fs2, FsEye, FsCap;
        public float[] TitleBase = Array.Empty<float>();
        public float AccentBase, CapBase, LeadY;
    }

    // 基准版式：以 1920×1080 为 1.0，按画幅比例推导
    const float F2_RATIO = 1.6818f;   // 强调字号 / 正文字号（148/88）
    const float EYE_RATIO = 0.2614f;  // eyebrow / 正文（23/88）
    const float CAP_RATIO = 0.3182f;  // caption / 正文（28/88）
    const float LINE_RATIO = 1.22f;   // 标题行距 / 字号

    static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    static float Lerp(float a, float b, float t) => a + (b - a) * t;
    static float EaseInOut(float t) => t * t * (3f - 2f * t);
    static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);
    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
    }
    static float Seg(float t, float a, float b) => Clamp01((t - a) / MathF.Max(1e-6f, b - a));

    // ========================================================
    //  实例：继承 SKCustomAnimation，逐帧逻辑在 DrawFrame 里
    //    构造时把 spec 归一化、解好几何、建好字体与着色器（只做一次）
    //    基类负责 BuildFrame / SavePNG / SaveMp4 / SaveGif 的帧循环
    // ========================================================
    readonly Spec _spec;
    readonly Geo _geo;
    readonly bool _stack;
    readonly float _duration, _k;

    readonly SKBitmap _bmp;
    readonly SKImage _img;
    readonly SKRect _src;
    readonly SKTypeface _serif, _sans;
    readonly SKFont _f1, _f2, _fEye, _fCap;

    readonly SKBitmap _grainBmp;
    readonly SKShader _grainShader, _bgShader, _warmHalo, _textShader, _goldShader, _vignetteShader;
    readonly SKPaint _bgPaint, _vignettePaint, _tintPaint;

    bool _disposed;

    public TitleCard(Spec spec) : this(Prepare(spec), true) { }

    /// <summary>先归一化 spec，再把它算出的画幅交给基类；归一化只会跑一次。</summary>
    TitleCard(Spec spec, bool _) : base(spec.Width, spec.Height, spec.Frames)
    {
        _spec = spec;
        _duration = spec.Frames / (float)spec.Fps;
        _k = spec.TimingScale > 0f ? spec.TimingScale : Math.Clamp(_duration / 3f, 0.55f, 1.6f);

        _bmp = SKBitmap.Decode(spec.Image) ?? throw new FileNotFoundException($"读不到图像：{spec.Image}");
        _img = SKImage.FromBitmap(_bmp);
        _src = new SKRect(0, _bmp.Height * spec.CropTop, _bmp.Width, _bmp.Height * spec.CropBottom);

        float aspect = _src.Width / MathF.Max(1f, _src.Height);
        Log(spec, $"[img] {_bmp.Width}×{_bmp.Height} → 取高 {spec.CropTop:P0}–{spec.CropBottom:P0} = {_src.Width:F0}×{_src.Height:F0}（比例 {aspect:F3}）");

        _stack = spec.Mode == Layout.Stack || (spec.Mode == Layout.Auto && spec.Height > spec.Width * 1.15f);
        _geo = Solve(spec, aspect, _stack);

        _serif = Pick(spec.SerifFonts, SKFontStyle.Bold, "serif", spec.Verbose);
        _sans = Pick(spec.SansFonts, SKFontStyle.Normal, "sans", spec.Verbose);

        _f1 = new SKFont(_serif, _geo.Fs1);
        _f2 = new SKFont(_serif, _geo.Fs2);
        _fEye = new SKFont(_sans, _geo.FsEye);
        _fCap = new SKFont(_serif, _geo.FsCap);

        _grainBmp = MakeGrain(spec.Seed);
        _grainShader = SKShader.CreateBitmap(_grainBmp, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

        _bgShader = SKShader.CreateRadialGradient(
            new SKPoint(_geo.Card.MidX + spec.Width * 0.031f, spec.Height * 0.44f), spec.Width * 0.677f,
            new[] { spec.R_BgGlow, spec.Background, spec.R_BgEdge },
            new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp);
        _bgPaint = new SKPaint { Shader = _bgShader };

        _warmHalo = SKShader.CreateRadialGradient(
            new SKPoint(_geo.Card.MidX, _geo.Card.MidY), _geo.CardW * 1.16f,
            new[] { spec.Gold.WithAlpha(0x34), spec.Gold.WithAlpha(0x0A), SKColors.Transparent },
            new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);

        _textShader = SKShader.CreateLinearGradient(
            new SKPoint(0, _geo.TitleBase[0] - _geo.Fs1 * 1.09f),
            new SKPoint(0, _geo.TitleBase[0] + _geo.Fs1 * 0.16f),
            new[] { spec.TitleLight, spec.R_TitleMid, spec.R_TitleDark },
            new[] { 0f, 0.62f, 1f }, SKShaderTileMode.Clamp);

        _goldShader = SKShader.CreateLinearGradient(
            new SKPoint(_geo.BlockX, _geo.AccentBase - _geo.Fs2),
            new SKPoint(_geo.BlockX + _geo.MaxLineW, _geo.AccentBase - _geo.Fs2 * 0.08f),
            new[] { spec.R_AccentLight, spec.R_Accent, spec.R_AccentDark },
            new[] { 0f, 0.48f, 1f }, SKShaderTileMode.Clamp);

        float halfDiag = MathF.Sqrt(spec.Width * spec.Width + spec.Height * spec.Height) * 0.5f;
        _vignetteShader = SKShader.CreateRadialGradient(
            new SKPoint(spec.Width * 0.5f, spec.Height * 0.5f), halfDiag * 1.13f,
            new[] { SKColors.Transparent, SKColors.Transparent, new SKColor(0, 0, 0, (byte)(255 * spec.Vignette)) },
            new[] { 0f, 0.58f, 1f }, SKShaderTileMode.Clamp);
        _vignettePaint = new SKPaint { Shader = _vignetteShader };

        _tintPaint = new SKPaint
        {
            Color = spec.CardTint.WithAlpha((byte)(255 * spec.CardTintAlpha)),
            BlendMode = SKBlendMode.Multiply
        };
    }

    static Spec Prepare(Spec spec) { spec.Normalize(); return spec; }

    // ========================================================
    //  逐帧绘制：由基类的 BuildFrame / SavePNG / SaveMp4 / SaveGif 调用
    //  也可以 externalDraw：别的动画里 canvas.Draw 时手动调 Draw(canvas, frame)
    // ========================================================
    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        var s = _spec;
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);
        float tc = t * _duration;
        float T(float sec) => sec * _k;
        var full = new SKRect(0, 0, s.Width, s.Height);

        canvas.Clear(s.Background);
        canvas.DrawRect(full, _bgPaint);

        float intro = EaseOutCubic(Seg(tc, 0f, T(0.16f)));
        using (var p = new SKPaint { Shader = _warmHalo, Color = new SKColor(255, 255, 255, (byte)(165 * intro)) })
            canvas.DrawRect(full, p);

        if (s.Grain > 0.001f)
        {
            using var gp = new SKPaint { Shader = _grainShader, Color = new SKColor(255, 255, 255, (byte)(145 * s.Grain)) };
            canvas.DrawRect(full, gp);
        }

        DrawCard(canvas, s, _geo, _img, _src, _tintPaint, T, tc, t);

        if (!_stack && s.ShowDivider) DrawDivider(canvas, s, _geo, T, tc);

        DrawText(canvas, s, _geo, T, tc, _f1, _f2, _fEye, _fCap, _textShader, _goldShader);

        canvas.DrawRect(full, _vignettePaint);

        float black = 1f - EaseOutCubic(Seg(tc, 0f, T(0.12f)));
        if (black > 0.002f)
        {
            using var bp = new SKPaint { Color = new SKColor(0, 0, 0, (byte)(255 * black)) };
            canvas.DrawRect(full, bp);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _bgPaint.Dispose();
        _vignettePaint.Dispose();
        _tintPaint.Dispose();
        _grainShader.Dispose();
        _bgShader.Dispose();
        _warmHalo.Dispose();
        _textShader.Dispose();
        _goldShader.Dispose();
        _vignetteShader.Dispose();
        _grainBmp.Dispose();
        _f1.Dispose();
        _f2.Dispose();
        _fEye.Dispose();
        _fCap.Dispose();
        _serif.Dispose();
        _sans.Dispose();
        _img.Dispose();
        _bmp.Dispose();
    }

    // ========================================================
    //  出片：按 spec 导出预览帧 + MP4（+ 可选 GIF）
    //  想自定义编码 / 只出几帧，就直接用基类的方法：
    //    SaveMp4 / SaveGif / SavePNG / BuildFrame
    // ========================================================
    public void Render()
    {
        var s = _spec;
        string dir = Path.GetDirectoryName(Path.GetFullPath(s.OutputPath)) ?? ".";
        string stem = Path.GetFileNameWithoutExtension(s.OutputPath);
        Directory.CreateDirectory(dir);

        if (s.StillFrame >= 0)
        {
            int fr = Math.Clamp(s.StillFrame, 0, Frames - 1);
            string p = Path.Combine(dir, $"still_{stem}_{fr:000}.png");
            Log(s, SavePNG(fr, p) ? $"静帧 → {p}" : $"静帧写出失败：{p}");
            return;
        }

        for (int i = 0; i < s.PreviewFrames; i++)
        {
            int fr = s.PreviewFrames == 1
                ? Frames - 1
                : (int)MathF.Round(i * (Frames - 1) / (float)(s.PreviewFrames - 1));
            if (!SavePNG(fr, Path.Combine(dir, "preview", $"{stem}_f_{fr:000}.png")))
                Console.WriteLine($"[warn] 预览帧 {fr} 写出失败");
        }

        SaveMp4(s.Fps, s.OutputPath);

        if (s.ExportGif)
        {
            string gp = Path.ChangeExtension(s.OutputPath, ".gif");
            SaveGif(s.Fps, gp);
            Log(s, $"GIF → {gp}");
        }

        Log(s, $"完成：{Frames} 帧 / {s.Fps}fps / {Width}×{Height} / {_duration:F2}s → {s.OutputPath}");
    }

    // ========================================================
    //  几何求解
    // ========================================================
    static Geo Solve(Spec s, float aspect, bool stack)
    {
        var geo = new Geo { Stack = stack, Mirror = s.ImageOnRight };
        bool mirror = s.ImageOnRight && !stack;

        if (stack)
        {
            float cw = MathF.Min(s.Width * 0.86f, s.Height * 0.46f * aspect);
            float ch = cw / aspect;
            float top = s.Height * 0.10f;
            geo.Card = new SKRect(s.Width * 0.5f - cw * 0.5f, top, s.Width * 0.5f + cw * 0.5f, top + ch);
            geo.CardW = cw;
            geo.BlockX = s.Width * 0.09f;
            geo.AvailW = s.Width * 0.82f;
            geo.VX = 0f;
        }
        else
        {
            float ch = s.Height * s.CardScale;
            float cw = ch * aspect;
            float cap = s.Width * s.CardWidthCap;
            if (cw > cap) { cw = cap; ch = cw / aspect; }
            float cx = s.Width * (mirror ? 0.7573f : 0.2427f);
            geo.Card = new SKRect(cx - cw * 0.5f, s.Height * 0.5f - ch * 0.5f, cx + cw * 0.5f, s.Height * 0.5f + ch * 0.5f);
            geo.CardW = cw;
            geo.BlockX = s.Width * (mirror ? 0.045f : 0.526f);
            geo.AvailW = s.Width * (mirror ? 0.44f : 0.42f);
            geo.VX = s.Width * (mirror ? 0.53125f : 0.46875f);
        }

        int n = s.Title.Length;
        bool hasAccent = !string.IsNullOrWhiteSpace(s.Accent);
        // 块高系数（以正文字号为单位）：lead 线 → caption 基线
        float blockCoef = 1.77f + (n - 1) * LINE_RATIO + (hasAccent ? 3.259f : 1.55f) + 0.095f;

        float u = stack ? MathF.Min(s.FontScale * s.Height, s.Width * 0.115f) : s.FontScale * s.Height;

        if (stack)
        {
            float availH = s.Height * 0.90f - geo.Card.Bottom - s.Height * 0.045f;
            if (blockCoef > 0.01f) u = MathF.Min(u, availH / blockCoef);
        }

        // 宽度约束：最长行与强调词都必须落在可用宽度内
        for (int iter = 0; iter < 3; iter++)
        {
            float need = MeasureNeed(s, u);
            if (need <= geo.AvailW || need <= 0f) break;
            u *= geo.AvailW / need * 0.998f;
        }

        geo.Fs1 = u;
        geo.Fs2 = u * F2_RATIO;
        geo.FsEye = u * EYE_RATIO;
        geo.FsCap = u * CAP_RATIO;
        geo.BlockH = blockCoef * u;

        if (stack)
        {
            float top = geo.Card.Bottom + s.Height * 0.045f;
            float availH = s.Height * 0.90f - top;
            geo.LeadY = top + MathF.Max(0f, (availH - geo.BlockH) * 0.5f);
        }
        else
        {
            geo.LeadY = (s.Height - geo.BlockH) * 0.5f + s.Height * 0.022f;
        }

        geo.TitleBase = new float[n];
        for (int i = 0; i < n; i++)
            geo.TitleBase[i] = geo.LeadY + (1.77f + i * LINE_RATIO) * geo.Fs1;

        geo.AccentBase = geo.TitleBase[n - 1] + 1.28f * geo.Fs2;
        geo.CapBase = hasAccent
            ? geo.AccentBase + 0.66f * geo.Fs2
            : geo.TitleBase[n - 1] + 1.55f * geo.Fs1;

        geo.MaxLineW = MaxLineWidth(s, geo);
        Log(s, $"[layout] {(stack ? "上图下文" : "左图右文")}{(mirror ? "（镜像）" : "")} 字号 {geo.Fs1:F0}/{geo.Fs2:F0} 卡 {geo.Card.Width:F0}×{geo.Card.Height:F0} 文字宽 {geo.MaxLineW:F0}/{geo.AvailW:F0}");
        return geo;
    }

    static SKTypeface ProbeTypeface(Spec s) =>
        SKTypeface.FromFamilyName(s.SerifFonts[0], SKFontStyle.Bold) ?? SKTypeface.Default;

    static float MeasureNeed(Spec s, float u)
    {
        using var tf = ProbeTypeface(s);
        using var f1 = new SKFont(tf, u);
        using var fa = new SKFont(tf, u * F2_RATIO);
        float need = 0f;
        foreach (var line in s.Title) need = MathF.Max(need, Measure(f1, line));
        if (!string.IsNullOrWhiteSpace(s.Accent)) need = MathF.Max(need, Measure(fa, s.Accent) + u * 0.10f);
        return need;
    }

    static float MaxLineWidth(Spec s, Geo g)
    {
        using var tf = ProbeTypeface(s);
        using var f1 = new SKFont(tf, g.Fs1);
        using var fa = new SKFont(tf, g.Fs2);
        float max = 0f;
        foreach (var line in s.Title) max = MathF.Max(max, Measure(f1, line));
        if (!string.IsNullOrWhiteSpace(s.Accent)) max = MathF.Max(max, Measure(fa, s.Accent));
        return max;
    }

    static float Measure(SKFont f, string text)
    {
        if (string.IsNullOrEmpty(text)) return 0f;
        var w = f.GetGlyphWidths(text, null);
        float sum = 0f;
        foreach (var x in w) sum += x;
        return sum;
    }

    static float LineX(Spec s, Geo g, float lineW) =>
        s.CenterAlign ? g.BlockX + MathF.Max(0f, (g.AvailW - lineW) * 0.5f) : g.BlockX;

    static SKTypeface Pick(string[] names, SKFontStyle style, string tag, bool verbose)
    {
        foreach (var n in names)
        {
            var tf = SKTypeface.FromFamilyName(n, style);
            if (tf is null) continue;
            if (verbose) Console.WriteLine($"[font/{tag}] 试 {n} → 实际 {tf.FamilyName}");
            if (tf.FamilyName.Contains(n, StringComparison.OrdinalIgnoreCase)) return tf;
            // 中文字体被替换成西文字体时渲染中文会出方块，宁可继续找
            if ((tag == "serif" && (tf.FamilyName.Contains("Song") || tf.FamilyName.Contains("Serif") || tf.FamilyName.Contains("Hei"))) ||
                (tag == "sans" && (tf.FamilyName.Contains("Bahnschrift") || tf.FamilyName.Contains("YaHei"))))
                return tf;
            tf.Dispose();
        }
        var fb = SKTypeface.FromFamilyName(names[^1], style) ?? SKTypeface.Default;
        if (verbose) Console.WriteLine($"[font/{tag}] 兜底 → {fb.FamilyName}");
        return fb;
    }

    // ========================================================
    //  图像卡片：推入淡入 → 暖调 → 烛光扫过 → 四边压暗 → 描边
    // ========================================================
    static void DrawCard(SKCanvas canvas, Spec s, Geo g, SKImage img, SKRect src,
                         SKPaint tintPaint, Func<float, float> T, float tc, float t)
    {
        var card = g.Card;
        float reveal = EaseOutCubic(Seg(tc, T(0.04f), T(0.30f)));
        if (reveal <= 0.001f) return;

        float push = 1.015f + 0.070f * EaseInOut(t);
        float lift = -10f * EaseInOut(t);

        using var cardPath = new SKPath();
        cardPath.AddRoundRect(card, s.CardRadius, s.CardRadius);

        using (var lp = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(255 * reveal)) })
        {
            canvas.SaveLayer(lp);
            canvas.Translate(-s.Width * 0.05f * (1f - reveal), lift);

            if (s.ShowShadow)
            {
                using var sh = new SKPaint
                {
                    IsAntialias = true,
                    Color = new SKColor(0, 0, 0, 150),
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 26f)
                };
                canvas.Save();
                canvas.Translate(0f, 20f);
                canvas.DrawPath(cardPath, sh);
                canvas.Restore();
            }

            canvas.Save();
            canvas.ClipPath(cardPath, SKClipOperation.Intersect, true);

            canvas.Save();
            canvas.Translate(card.MidX, card.MidY);
            canvas.Scale(push, push);
            canvas.Translate(-card.MidX, -card.MidY);
            using (var ip = new SKPaint { IsAntialias = true })
                canvas.DrawImage(img, src, card,
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), ip);

            if (s.CardTintAlpha > 0.001f) canvas.DrawRect(card, tintPaint);

            // 烛光扫过：SoftLight 调暖，不用 Plus 提亮（会把纸曝成纯白）
            float sweep = EaseInOut(Seg(tc, T(0.08f), T(0.56f)));
            if (sweep > 0f && sweep < 1f)
            {
                float sx = Lerp(card.Left - g.CardW * 0.95f, card.Right + g.CardW * 0.30f, sweep);
                using var ws = SKShader.CreateLinearGradient(
                    new SKPoint(sx - g.CardW * 0.50f, card.Top - 90f),
                    new SKPoint(sx + g.CardW * 0.50f, card.Bottom + 90f),
                    new[]
                    {
                        new SKColor(0xFF, 0xD6, 0x92, 0),
                        new SKColor(0xFF, 0xDC, 0xA4, 66),
                        new SKColor(0xFF, 0xF0, 0xD6, 96),
                        new SKColor(0xFF, 0xD6, 0x92, 0)
                    },
                    new[] { 0f, 0.42f, 0.55f, 1f }, SKShaderTileMode.Clamp);
                using (var wp = new SKPaint
                {
                    IsAntialias = true, Shader = ws, BlendMode = SKBlendMode.SoftLight,
                    Color = new SKColor(255, 255, 255, (byte)(238 * MathF.Sin(MathF.PI * sweep)))
                })
                    canvas.DrawRect(card, wp);
                using (var wp2 = new SKPaint
                {
                    IsAntialias = true, Shader = ws, BlendMode = SKBlendMode.Plus,
                    Color = new SKColor(255, 255, 255, (byte)(30 * MathF.Sin(MathF.PI * sweep)))
                })
                    canvas.DrawRect(card, wp2);
            }

            using (var es = SKShader.CreateRadialGradient(
                new SKPoint(card.MidX, card.MidY), card.Height * 0.80f,
                new[] { SKColors.Transparent, SKColors.Transparent, s.Background.WithAlpha(172) },
                new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp))
            using (var ep = new SKPaint { Shader = es })
                canvas.DrawRect(card, ep);

            canvas.Restore();   // 推近
            canvas.Restore();   // 圆角裁切

            using (var glow = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 9f,
                Color = s.Gold.WithAlpha((byte)(24 * reveal)),
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 11f)
            })
                canvas.DrawPath(cardPath, glow);
            using (var edge = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.3f,
                Color = s.Gold.WithAlpha((byte)(135 * reveal))
            })
                canvas.DrawPath(cardPath, edge);

            if (s.ShowBrackets)
            {
                float br = EaseOutCubic(Seg(tc, T(0.26f), T(0.46f)));
                if (br > 0f)
                {
                    float in0 = 22f, len = 34f * br;
                    var pts = new[]
                    {
                        (card.Left + in0, card.Top + in0, 1f, 1f),
                        (card.Right - in0, card.Top + in0, -1f, 1f),
                        (card.Left + in0, card.Bottom - in0, 1f, -1f),
                        (card.Right - in0, card.Bottom - in0, -1f, -1f)
                    };
                    using var bp = new SKPaint
                    {
                        IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f,
                        StrokeCap = SKStrokeCap.Square,
                        Color = s.Gold.WithAlpha((byte)(160 * br))
                    };
                    foreach (var (bx, by, dx, dy) in pts)
                    {
                        canvas.DrawLine(bx, by, bx + dx * len, by, bp);
                        canvas.DrawLine(bx, by, bx, by + dy * len, bp);
                    }
                }
            }

            canvas.Restore();   // SaveLayer
        }
    }

    static void DrawDivider(SKCanvas canvas, Spec s, Geo g, Func<float, float> T, float tc)
    {
        float vl = EaseInOut(Seg(tc, T(0.26f), T(0.54f)));
        if (vl <= 0f) return;

        float half = s.Height * 0.2685f * vl;
        float cy = s.Height * 0.5f;
        using var vs = SKShader.CreateLinearGradient(
            new SKPoint(g.VX, cy - half), new SKPoint(g.VX, cy + half),
            new[] { s.Gold.WithAlpha(0), s.Gold.WithAlpha(62), s.Gold.WithAlpha(0) },
            new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
        using var vp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Shader = vs };
        canvas.DrawLine(g.VX, cy - half, g.VX, cy + half, vp);
    }

    // ========================================================
    //  文字层
    // ========================================================
    static void DrawText(SKCanvas canvas, Spec s, Geo g, Func<float, float> T, float tc,
                         SKFont f1, SKFont f2, SKFont fE, SKFont fC, SKShader textShader, SKShader goldShader)
    {
        canvas.SaveLayer();

        float maxW = g.MaxLineW;
        float lx0 = LineX(s, g, maxW);

        // 起首短金线
        float lp = EaseInOut(Seg(tc, T(0.24f), T(0.44f)));
        if (lp > 0f)
        {
            using var lsh = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.4f,
                StrokeCap = SKStrokeCap.Round,
                Color = s.Gold.WithAlpha((byte)(225 * lp))
            };
            canvas.DrawLine(lx0, g.LeadY, lx0 + g.Fs1 * 0.66f * lp, g.LeadY, lsh);
        }

        // eyebrow：逐字淡入 + 上浮
        if (!string.IsNullOrWhiteSpace(s.Eyebrow))
        {
            float acc = 0f;
            float tracking = g.FsEye * 0.115f;
            for (int i = 0; i < s.Eyebrow.Length; i++)
            {
                string ch = s.Eyebrow.Substring(i, 1);
                float p = EaseOutCubic(Seg(tc, T(0.26f) + i * T(0.011f), T(0.44f) + i * T(0.011f)));
                if (p > 0f)
                {
                    using var ep = new SKPaint { IsAntialias = true, Color = s.EyebrowColor.WithAlpha((byte)(225 * p)) };
                    canvas.DrawText(ch, lx0 + acc, g.LeadY + g.Fs1 * 0.50f + g.FsEye * 0.39f * (1f - p),
                                    SKTextAlign.Left, fE, ep);
                }
                var gw = fE.GetGlyphWidths(ch, null);
                acc += (gw.Length > 0 ? gw[0] : 0f) + tracking;
            }
        }

        // 标题各行：逐字上浮
        for (int li = 0; li < s.Title.Length; li++)
        {
            string line = s.Title[li];
            float lx = LineX(s, g, maxW);
            var ws = f1.GetGlyphWidths(line, null);
            float acc = 0f;
            for (int i = 0; i < line.Length; i++)
            {
                float st = T(0.32f) + li * T(0.10f) + i * T(0.030f);
                float p = EaseOutCubic(Seg(tc, st, st + T(0.30f)));
                if (p > 0f)
                {
                    string ch = line.Substring(i, 1);
                    float bx = lx + acc;
                    float by = g.TitleBase[li] + g.Fs1 * 0.25f * (1f - p);

                    using (var glow = new SKPaint
                    {
                        IsAntialias = true,
                        Color = s.R_TitleMid.WithAlpha((byte)(28 * p)),
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, g.Fs1 * 0.17f)
                    })
                        canvas.DrawText(ch, bx, by, SKTextAlign.Left, f1, glow);

                    using var tp = new SKPaint
                    {
                        IsAntialias = true, Shader = textShader,
                        Style = SKPaintStyle.StrokeAndFill,
                        StrokeWidth = MathF.Max(1.2f, g.Fs1 * 0.0205f),   // 可变字体偏细，靠描边补粗
                        StrokeJoin = SKStrokeJoin.Round,
                        Color = new SKColor(255, 255, 255, (byte)(255 * p))
                    };
                    canvas.DrawText(ch, bx, by, SKTextAlign.Left, f1, tp);
                }
                acc += ws.Length > i ? ws[i] : 0f;
            }
        }

        // 强调词：过冲放大 + 金色辉光 + 下划线
        if (!string.IsNullOrWhiteSpace(s.Accent))
        {
            float sp = Seg(tc, T(0.50f), T(0.78f));
            if (sp > 0f)
            {
                float aw = Measure(f2, s.Accent);
                float se = EaseOutBack(sp);
                float alpha = Clamp01(sp * 2.8f);
                float sc = 0.84f + 0.16f * se;
                float pivotY = g.AccentBase - g.Fs2 * 0.36f;

                canvas.Save();
                canvas.Translate(lx0, pivotY);
                canvas.Scale(sc, sc);
                canvas.Translate(-lx0, -pivotY);

                float pulse = MathF.Sin(MathF.PI * Clamp01(sp * 1.2f));
                using (var gg = new SKPaint
                {
                    IsAntialias = true,
                    Color = s.R_Accent.WithAlpha((byte)(62 * pulse)),
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 28f)
                })
                    canvas.DrawText(s.Accent, lx0, g.AccentBase, SKTextAlign.Left, f2, gg);

                using (var gp = new SKPaint
                {
                    IsAntialias = true, Shader = goldShader,
                    Style = SKPaintStyle.StrokeAndFill,
                    StrokeWidth = MathF.Max(1.6f, g.Fs2 * 0.015f),
                    StrokeJoin = SKStrokeJoin.Round,
                    Color = new SKColor(255, 255, 255, (byte)(255 * alpha))
                })
                    canvas.DrawText(s.Accent, lx0, g.AccentBase, SKTextAlign.Left, f2, gp);

                float up = EaseOutCubic(Seg(tc, T(0.62f), T(0.86f)));
                if (up > 0f)
                {
                    using var ul = new SKPaint
                    {
                        IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.6f,
                        Color = s.R_Accent.WithAlpha((byte)(165 * up))
                    };
                    canvas.DrawLine(lx0 + 3f, g.AccentBase + g.Fs2 * 0.176f,
                                    lx0 + 3f + (aw - 6f) * up, g.AccentBase + g.Fs2 * 0.176f, ul);
                }
                canvas.Restore();
            }
        }

        // 说明行
        if (!string.IsNullOrWhiteSpace(s.Caption))
        {
            float cp = EaseOutCubic(Seg(tc, T(0.58f), T(0.80f)));
            if (cp > 0f)
            {
                float lx = LineX(s, g, maxW);
                using var cpc = new SKPaint { IsAntialias = true, Color = s.CaptionColor.WithAlpha((byte)(180 * cp)) };
                canvas.DrawText(s.Caption, lx, g.CapBase + g.FsCap * 0.43f * (1f - cp), SKTextAlign.Left, fC, cpc);

                using var mk = new SKPaint { IsAntialias = true, Color = s.Gold.WithAlpha((byte)(230 * cp)) };
                canvas.DrawRect(new SKRect(lx - g.FsCap * 0.71f, g.CapBase - g.FsCap * 0.61f,
                                           lx - g.FsCap * 0.46f, g.CapBase - g.FsCap * 0.36f), mk);
            }
        }

        // 高光扫过：SrcATop 只吃文字像素
        float sw = Seg(tc, T(0.72f), T(0.95f));
        if (sw > 0f && sw < 1f)
        {
            float x = Lerp(g.BlockX - g.Fs1 * 3.2f, g.BlockX + maxW + g.Fs1 * 3.6f, EaseInOut(sw));
            using var ss = SKShader.CreateLinearGradient(
                new SKPoint(x - g.Fs1 * 1.6f, g.CapBase), new SKPoint(x + g.Fs1 * 1.6f, g.LeadY),
                new[]
                {
                    s.SweepColor.WithAlpha(0),
                    s.SweepColor.WithAlpha(150),
                    Spec.Lighten(s.SweepColor, 0.30f).WithAlpha(210),
                    s.SweepColor.WithAlpha(0)
                },
                new[] { 0f, 0.40f, 0.56f, 1f }, SKShaderTileMode.Clamp);
            using var spaint = new SKPaint
            {
                IsAntialias = true, Shader = ss, BlendMode = SKBlendMode.SrcATop,
                Color = new SKColor(255, 255, 255, (byte)(240 * MathF.Sin(MathF.PI * sw)))
            };
            canvas.DrawRect(new SKRect(g.BlockX - g.Fs1 * 0.5f, g.LeadY - g.Fs1 * 0.45f,
                                       g.BlockX + maxW + g.Fs1 * 0.7f, g.CapBase + g.FsCap * 0.9f), spaint);
        }

        canvas.Restore();
    }

    static SKBitmap MakeGrain(int seed)
    {
        var bmp = new SKBitmap(256, 256);
        var rnd = new Random(seed);
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                byte v = (byte)rnd.Next(0, 255);
                byte a = (byte)(rnd.Next(0, 100) < 40 ? 24 : 0);
                bmp.SetPixel(x, y, new SKColor(v, v, v, a));
            }
        return bmp;
    }

    static void Log(Spec s, string msg)
    {
        if (s.Verbose) Console.WriteLine(msg);
    }
}