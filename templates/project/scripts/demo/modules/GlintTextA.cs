// ============================================================
//  GlintTextA · 单行文字层（NewBeeVG / SkiaSharp）
//  一行字，别的什么都没有（没有图、没有副标题、没有金线）。
//  主角是「掠金」——一道斜光扫过字面，扫完不留痕。
//
//  文字可以套一层装饰，样式由 TextStyle 选，颜色一律取主题主色：
//    Plain      纯文字
//    Button     描边按钮：淡填充 + 细边 + 圆角
//    Underline  下划线：字下一道细线，从中间向两边长出
//    Mark       高亮底：字后一块色块，像马克笔划过（左起展开）
//    Bar        左竖条：引用块的那种竖线，从下长上来
//    Bracket    方括号：两侧各一组直角括号
//
//  为什么样式要在文字落定之后才出：装饰先于字出现，会读成「一个空按钮」；
//  字先落定、装饰再压上去，才像「有人把这句话标了出来」。
//
//  时间线按「总帧数的系数」写，不按秒写：
//    AnimRatio × frames 走完入场，HoldRatio × frames 停在最后一帧（真静帧）。
// ============================================================

using NewBeeVG;
using SkiaSharp;


// ============================================================
//  渲染器
// ============================================================
public sealed class GlintTextA : SKCustomAnimation, IDisposable
{
    /// <summary>文字的装饰样式。颜色一律取主题主色，不单独配。</summary>
    public enum TextStyle
    {
        /// <summary>纯文字。</summary>
        Plain,
        /// <summary>描边按钮：淡填充 + 细边 + 圆角。</summary>
        Button,
        /// <summary>下划线：字下一道细线，从中间向两边长出。</summary>
        Underline,
        /// <summary>高亮底：字后一块色块，像马克笔划过。</summary>
        Mark,
        /// <summary>左竖条：引用块的那种竖线。</summary>
        Bar,
        /// <summary>方括号：两侧各一组直角括号。</summary>
        Bracket,
    }

    // ========================================================
    //  几何：一次解好，帧内只读
    // ========================================================
    sealed class Geo
    {
        public float Fs;
        public float TextW;
        public float Left, Right;      // 文字左右边界
        public float Top, Bottom;      // 字身上下（不含降部）
        public float Baseline;
        public float MidY;             // 视觉中心（画背光用）
    }

    readonly ContentSetting _c;
    readonly ContentTheme _theme;
    readonly Geo _geo;

    readonly SKBitmap _grainBmp;
    readonly SKShader _grainShader, _bgShader, _vignetteShader, _glowShader;
    readonly SKPaint _bgPaint;

    readonly Mote[] _motes;
    readonly SKFont _fText;
    readonly SKTypeface _tfText;

    int _fps = 25;
    bool _disposed;

    // ---------- 时间线派生 ----------
    float AnimEnd => _c.AnimEnd;
    /// <summary>动画段帧数（含最后一帧）</summary>
    public int AnimFrames => Math.Clamp((int)MathF.Round(Frames * AnimEnd), 1, Frames);
    /// <summary>尾帧停留帧数</summary>
    public int HoldFrames => Math.Max(0, Frames - AnimFrames);
    /// <summary>实际时长（秒）= 帧数 / 帧率。帧率在出片时给，所以是算出来的。</summary>
    public float Duration => Frames / (float)_fps;

    // ========================================================
    //  构造：归一化 → 量字宽 → 解几何 → 备资源
    // ========================================================
    // Margin 内边距与内容区尺寸（本地坐标 0,0 – CW,CH）
    readonly float PadX, PadY, CW, CH;

    public GlintTextA(int width, int height, int frames, ContentSetting content, ContentTheme? contentTheme = null)
        : base(Math.Max(1, width), Math.Max(1, height), Math.Max(1, frames),
               contentTheme ?? ContentTheme.CreateDefault())
    {
        _c = content ?? throw new ArgumentNullException(nameof(content));
        _theme = (ContentTheme)Theme;      // 基类保证非空
        _c.Normalize();

        // ---- 字体 ----
        _tfText = Pick(_c.FontFamily, SKFontStyle.Bold, "text");
        _fText = new SKFont(_tfText, 100f);

        // ---- 几何：一行字，问题只剩「字号多大」与「样式留多少边」 ----
        var mg = _c.GetMargin();
        PadX = Math.Max(0f, mg[0]);
        PadY = Math.Max(0f, mg[1]);
        CW = Width - PadX - Math.Max(0f, mg[2]);
        CH = Height - PadY - Math.Max(0f, mg[3]);
        if (CW <= 1f || CH <= 1f) { PadX = PadY = 0f; CW = Width; CH = Height; }

        _geo = Solve(_c, (int)CW, (int)CH, MeasureOne(_fText, _c.Text) / 100f);

        _fText.Size = _geo.Fs;

        // ---- 颗粒 ----
        _grainBmp = MakeGrain(_c.Seed);
        _grainShader = SKShader.CreateBitmap(_grainBmp, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

        // ---- 底色：中心微亮、边缘压暗 ----
        float bgLum = Lum(_theme.Background);
        bool lightBg = bgLum > 0.5f;
        var bgGlow = Lighten(_theme.Background, lightBg ? 0.22f : 0.13f);
        var bgEdge = Darken(_theme.Background, lightBg ? 0.16f : 0.55f);
        _bgShader = SKShader.CreateRadialGradient(
            new SKPoint(CW * 0.5f, CH * 0.44f), MathF.Max(CW, CH) * 0.80f,
            new[] { bgGlow, _theme.Background, bgEdge }, new[] { 0f, 0.50f, 1f }, SKShaderTileMode.Clamp);
        _bgPaint = new SKPaint { IsAntialias = true, Shader = _bgShader };

        // ---- 暗角。亮底收着力，否则纸会发脏 ----
        _vignetteShader = SKShader.CreateRadialGradient(
            new SKPoint(CW * 0.5f, CH * 0.5f), MathF.Max(CW, CH) * 0.80f,
            new[] { SKColors.Transparent, SKColors.Transparent,
                    _theme.Shadow.WithAlpha((byte)(132 * (lightBg ? 0.40f : 1f))) },
            new[] { 0f, 0.64f, 1f }, SKShaderTileMode.Clamp);

        // ---- 主题背光：一行字背后的一团灯 ----
        float radius = MathF.Max(CW * 0.34f, _geo.Fs * 4.2f);
        _glowShader = SKShader.CreateRadialGradient(
            new SKPoint(CW * 0.5f, _geo.MidY), radius,
            new[] { _theme.Primary.WithAlpha(62), _theme.Primary.WithAlpha(24), SKColors.Transparent },
            new[] { 0f, 0.42f, 1f }, SKShaderTileMode.Clamp);

        // ---- 浮尘 ----
        var rnd = new Random(_c.Seed + 77);
        _motes = new Mote[Math.Max(0, _c.Dust)];
        for (int i = 0; i < _motes.Length; i++)
            _motes[i] = new Mote
            {
                X = (float)rnd.NextDouble(),
                Y = (float)rnd.NextDouble(),
                R = 0.9f + (float)rnd.NextDouble() * 1.6f,
                Speed = 0.35f + (float)rnd.NextDouble() * 0.85f,
                Phase = (float)(rnd.NextDouble() * Math.PI * 2),
            };

        Log($"[layout] 单行 · 画幅 {CW}×{CH} · {_c.Text.Length} 字 · 字号 {_geo.Fs:F0}pt" +
            $" · 行宽 {_geo.TextW:F0}px · 样式 {_c.Style}");
        Log($"[theme] {_theme.Name} · 主色 {Hex(_theme.Primary)} · 底 {Hex(_theme.Background)}");
    }

    // ========================================================
    //  逐帧绘制
    // ========================================================
        protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        // 主题底色透明时不清屏，交给下层
        if (_theme.Background.Alpha > 0) canvas.Clear(_theme.Background);
        if (CW <= 1f || CH <= 1f) return;

        canvas.Save();
        canvas.ClipRect(new SKRect(PadX, PadY, PadX + CW, PadY + CH));
        canvas.Translate(PadX, PadY);
        // 动画段结束就冻住帧号：停留段必须是最后一帧的原样重放
        DrawContent(canvas, Math.Min(frame, Math.Max(0, AnimFrames - 1)));
        canvas.Restore();
    }

    void DrawContent(SKCanvas canvas, int frame)
    {
        var size = new SKSize(CW, CH);
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);
        var tl = new Timeline(Duration, AnimEnd, _c.TextStart, _c.Stagger);

        // 动画段一结束就把时钟停住：停留段是最后一帧的原样重放
        float tc = MathF.Min(t * Duration, tl.AnimSeconds);
        var full = new SKRect(0, 0, size.Width, size.Height);

        // 主题底色是透明色时，氛围层（底色、辉光、颗粒、暗角、开场压暗）一律不画
        bool bgOn = _theme.Background.Alpha > 0;

        if (bgOn) canvas.DrawRect(full, _bgPaint);

        if (bgOn) DrawGlow(canvas, full, tl, tc);

        // 先铺样式里「在字后面」的部分（填充、高亮块），再字号，最后画「在字前面」的线
        float sp = StyleP(tl, tc);
        DrawStyleBack(canvas, sp);
        DrawText(canvas, tl, tc);
        DrawStyleFront(canvas, sp);

        DrawGlint(canvas, size, tl, tc);

        if (bgOn && _c.Grain > 0.001f)
        {
            using var gp = new SKPaint
            {
                Shader = _grainShader,
                Color = new SKColor(255, 255, 255, (byte)(150 * _c.Grain))
            };
            canvas.DrawRect(full, gp);
        }

        DrawDust(canvas, size, tl, tc);

        if (bgOn && _c.Vignette > 0.001f)
        {
            using var vp = new SKPaint
            {
                IsAntialias = true,
                Shader = _vignetteShader,
                Color = new SKColor(255, 255, 255, (byte)(255 * _c.Vignette))
            };
            canvas.DrawRect(full, vp);
        }

        // 开场压暗：用底色而不是纯黑，亮底主题才不会从黑屏里跳出来
        float dark = (1f - EaseOutCubic(Seg(tc, 0f, tl.At(0.14f)))) * _c.Reveal;
        if (bgOn && dark > 0.002f)
        {
            using var dp = new SKPaint { Color = _theme.Background.WithAlpha((byte)(255 * dark)) };
            canvas.DrawRect(full, dp);
        }
    }

    void DrawGlow(SKCanvas canvas, SKRect full, in Timeline tl, float tc)
    {
        if (_c.Glow <= 0.001f) return;
        float p = EaseOutCubic(Seg(tc, 0f, tl.At(0.42f)));
        if (p <= 0.002f) return;
        using var gp = new SKPaint
        {
            IsAntialias = true,
            Shader = _glowShader,
            BlendMode = SKBlendMode.Screen,
            Color = new SKColor(255, 255, 255, (byte)(255 * _c.Glow * p))
        };
        canvas.DrawRect(full, gp);
    }

    // ========================================================
    //  文字入场：逐字上浮淡入。单行不需要行幕 —— 幕本来就是给多行用的。
    //
    //  字数一多，逐字错峰会把动画段吃光，装饰（下划线、高亮块）就展不开了：
    //  实测 7 个字的卡，装饰到动画结束只展开了三分之一。
    //  所以字块也要有时间预算：把「(n−1)·错峰 + 单字时长 + 装饰」一起压进剩余拍宽。
    // ========================================================
    float CharFit
    {
        get
        {
            int n = Math.Max(1, _c.Text.Length);
            float style = _c.Style == TextStyle.Plain || _c.StyleStrength <= 0.004f
                ? 0f : _c.StyleAt + _c.StyleSpan + 0.02f;
            float room = (0.96f - _c.TextStart) - style;
            float need = (n - 1) * 0.055f * _c.CharStagger + 0.26f;
            float limit = MathF.Max(0.10f, room * 0.96f);
            return need > limit ? limit / need : 1f;
        }
    }

    float CharGapBeats => 0.055f * _c.CharStagger * CharFit;
    float CharSpanBeats => 0.26f * CharFit;

    float CharP(int i, in Timeline tl, float tc)
    {
        float st = tl.TextAt + i * tl.Gap(CharGapBeats);
        return EaseOutCubic(Seg(tc, st, st + tl.Span(CharSpanBeats)));
    }

    void DrawText(SKCanvas canvas, in Timeline tl, float tc)
    {
        var g = _geo;
        if (string.IsNullOrEmpty(_c.Text)) return;

        using var tp = new SKPaint { IsAntialias = true, Color = _theme.TextPrimary };
        float acc = 0f;
        for (int i = 0; i < _c.Text.Length; i++)
        {
            string ch = _c.Text.Substring(i, 1);
            float p = CharP(i, tl, tc);
            if (p > 0.001f)
            {
                tp.Color = _theme.TextPrimary.WithAlpha((byte)(255 * p));
                float dy = g.Fs * 0.34f * (1f - p);
                canvas.DrawText(ch, g.Left + acc, g.Baseline + dy, SKTextAlign.Left, _fText, tp);
            }
            acc += _fText.GetGlyphWidths(ch, null)[0];
        }
    }

    // ========================================================
    //  样式的时序与几何
    //  装饰等文字落定后再出：先出装饰会读成「一个空按钮」，
    //  字先落定、装饰再压上去，才像「有人把这句话标了出来」。
    // ========================================================
    float LastCharEnd(in Timeline tl)
    {
        int n = Math.Max(1, _c.Text.Length);
        return tl.TextAt + (n - 1) * tl.Gap(CharGapBeats) + tl.Span(CharSpanBeats);
    }

    float StyleP(in Timeline tl, float tc)
    {
        if (_c.Style == TextStyle.Plain || _c.StyleStrength <= 0.004f) return 0f;
        float t0 = LastCharEnd(tl) + tl.Span(_c.StyleAt);
        return EaseOutCubic(Seg(tc, t0, t0 + tl.Span(_c.StyleSpan)));
    }

    /// <summary>样式在字后面的部分：填充与高亮块。</summary>
    void DrawStyleBack(SKCanvas canvas, float sp)
    {
        if (sp <= 0.002f) return;
        var g = _geo;
        float fs = g.Fs;
        float k = _c.StyleStrength;
        var col = _c.StyleColor ?? _theme.Primary;

        switch (_c.Style)
        {
            case TextStyle.Mark:
            {
                // 高亮块：左起展开。边界略不齐（±0.5% 字高），读成笔刷而不是色块
                float padX = fs * 0.16f, padT = fs * 0.10f, padB = fs * 0.08f;
                float x0 = g.Left - padX, x1 = g.Right + padX;
                float y0 = g.Top - padT, y1 = g.Bottom + padB;
                float tip = x0 + (x1 - x0) * sp;
                using var mp = new SKPaint
                {
                    IsAntialias = true,
                    Color = Mix(col, _theme.TextPrimary, 0.10f).WithAlpha((byte)(52 * k)),
                };
                canvas.DrawRect(new SKRect(x0, y0, tip, y1), mp);
                // 压一层更实的芯：只有一层 22% 的色块会像「字被改了底色」，加芯才有笔画感
                using var core = new SKPaint
                {
                    IsAntialias = true,
                    Color = Mix(col, _theme.TextPrimary, 0.04f).WithAlpha((byte)(34 * k)),
                };
                canvas.DrawRect(new SKRect(x0, y0 + fs * 0.10f, tip, y1 - fs * 0.06f), core);
                break;
            }
            case TextStyle.Button:
            {
                var r = ButtonRect();
                float s = 0.965f + 0.035f * sp;
                canvas.Save();
                canvas.Scale(s, s, r.MidX, r.MidY);
                using var bp = new SKPaint
                {
                    IsAntialias = true,
                    Color = col.WithAlpha((byte)(34 * k * sp)),
                };
                canvas.DrawRoundRect(r, _c.StyleRadius * fs, _c.StyleRadius * fs, bp);
                canvas.Restore();
                break;
            }
        }
    }

    /// <summary>样式在字前面的部分：线、竖条、括号、按钮边框。</summary>
    void DrawStyleFront(SKCanvas canvas, float sp)
    {
        if (sp <= 0.002f) return;
        var g = _geo;
        float fs = g.Fs;
        float k = _c.StyleStrength;
        var col = _c.StyleColor ?? _theme.Primary;
        float w = MathF.Max(1.4f, fs * 0.030f) * (0.55f + 0.45f * _c.StyleWeight);

        switch (_c.Style)
        {
            case TextStyle.Underline:
            {
                // 从中间向两边长出：从一头长会读成「进度条」
                float len = g.TextW * 0.985f * sp;
                float y = g.Bottom + fs * 0.16f;
                using var p = new SKPaint
                {
                    IsAntialias = true, StrokeCap = SKStrokeCap.Round,
                    Style = SKPaintStyle.Stroke, StrokeWidth = MathF.Max(2f, fs * 0.045f) * _c.StyleWeight,
                    Color = col.WithAlpha((byte)(235 * k)),
                };
                canvas.DrawLine(g.Left + (g.TextW - len) * 0.5f, y,
                                g.Left + (g.TextW + len) * 0.5f, y, p);
                break;
            }
            case TextStyle.Bar:
            {
                // 从下往上长
                float h = (g.Bottom - g.Top) * sp;
                float x = g.Left - fs * 0.26f;
                using var p = new SKPaint
                {
                    IsAntialias = true, StrokeCap = SKStrokeCap.Round,
                    Style = SKPaintStyle.Stroke, StrokeWidth = MathF.Max(2f, fs * 0.055f) * _c.StyleWeight,
                    Color = col.WithAlpha((byte)(240 * k)),
                };
                canvas.DrawLine(x, g.Bottom, x, g.Bottom - h, p);
                break;
            }
            case TextStyle.Bracket:
            {
                // 两侧直角括号，从外往内收
                float gap = fs * (0.34f - 0.10f * sp);
                float arm = fs * 0.30f;
                float over = fs * 0.06f;
                using var p = new SKPaint
                {
                    IsAntialias = true, StrokeCap = SKStrokeCap.Square,
                    Style = SKPaintStyle.Stroke, StrokeWidth = w,
                    Color = col.WithAlpha((byte)(235 * k * sp)),
                };
                float top = g.Top - over, bot = g.Bottom + over;
                // 左括号
                float xl = g.Left - gap;
                canvas.DrawLine(xl, top, xl, bot, p);
                canvas.DrawLine(xl, top, xl + arm, top, p);
                canvas.DrawLine(xl, bot, xl + arm, bot, p);
                // 右括号
                float xr = g.Right + gap;
                canvas.DrawLine(xr, top, xr, bot, p);
                canvas.DrawLine(xr, top, xr - arm, top, p);
                canvas.DrawLine(xr, bot, xr - arm, bot, p);
                break;
            }
            case TextStyle.Button:
            {
                var r = ButtonRect();
                float s = 0.965f + 0.035f * sp;
                canvas.Save();
                canvas.Scale(s, s, r.MidX, r.MidY);
                using var p = new SKPaint
                {
                    IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = w,
                    Color = col.WithAlpha((byte)(205 * k * sp)),
                };
                canvas.DrawRoundRect(r, _c.StyleRadius * fs, _c.StyleRadius * fs, p);
                canvas.Restore();
                break;
            }
        }
    }

    SKRect ButtonRect()
    {
        var g = _geo;
        float fs = g.Fs;
        return new SKRect(
            g.Left - _c.StylePad * fs,
            g.Top - _c.StylePad * fs * 0.72f,
            g.Right + _c.StylePad * fs,
            g.Bottom + _c.StylePad * fs * 0.72f);
    }

    // ========================================================
    //  文字掠金：一道斜光扫过字面，扫完不留痕
    //  字形 alpha 当蒙版 → SrcIn 裁出斜光带 → 叠回画面。
    //  光只落在字上，空白处不会凭空多出一条光；装饰也不参与（光只打墨，不打框）。
    // ========================================================
    void DrawGlint(SKCanvas canvas, SKSize size, in Timeline tl, float tc)
    {
        if (_c.Glint <= 0.005f || string.IsNullOrEmpty(_c.Text)) return;

        float s = EaseInOut(Seg(tc, tl.At(_c.GlintAt), tl.At(_c.GlintAt + _c.GlintSpan)));
        if (s <= 0.0005f || s >= 0.9995f) return;

        var g = _geo;
        float fs = g.Fs;

        float top = g.Top - fs * 0.10f;
        float bot = g.Bottom + fs * 0.10f;
        float left = g.Left - fs * 0.34f;
        float textW = g.TextW + fs * 0.68f;

        // 光带从字块左外侧走到右外侧，走到边界时正好整条在字外
        float bw = MathF.Max(fs * 0.70f, textW * _c.GlintWidth);
        float cx = left - bw + (textW + 2f * bw) * s;
        float cy = (top + bot) * 0.5f;

        float ang = _c.GlintTilt * MathF.PI / 180f;
        float dx = MathF.Cos(ang), dy = MathF.Sin(ang);
        float reach = (textW * MathF.Abs(dx) + (bot - top) * MathF.Abs(dy)) * 0.5f + bw * 2.4f;
        var p0 = new SKPoint(cx - dx * reach, cy - dy * reach);
        var p1 = new SKPoint(cx + dx * reach, cy + dy * reach);
        float k = Math.Clamp(bw * 0.5f / (2f * reach), 0.004f, 0.40f);

        // 进出手都有淡入淡出，否则光带会在字上凭空亮起、凭空消失
        float env = MathF.Pow(MathF.Sin(MathF.PI * s), 0.40f) * _c.Glint;

        // 判断看「字比底色亮还是暗」，而不是看主题名：
        //   暗底浅字 —— 字已接近白，加光余量小，靠字外那圈晕读出「光」
        //   亮底深字 —— 墨字余量大，直接把光带镀成亮金
        bool onDark = Lum(_theme.TextPrimary) > 0.5f;
        var core = onDark ? Mix(_theme.Primary, SKColors.White, 0.86f)
                          : Mix(_theme.Sweep, _theme.Primary, 0.26f);
        var rim = onDark ? Mix(_theme.Primary, SKColors.White, 0.50f)
                         : Mix(_theme.Sweep, _theme.Primary, 0.50f);
        byte aHi = (byte)(255 * env), aMd = (byte)(150 * env), aSk = (byte)(82 * env);

        // 剖面：两侧三倍多宽的软裙（光散开的地方）→ 贴边的金 → 中间一条白芯
        var colors = new[]
        {
            core.WithAlpha((byte)0), core.WithAlpha((byte)0),
            rim.WithAlpha(aSk), rim.WithAlpha(aMd), core.WithAlpha(aHi), rim.WithAlpha(aMd), rim.WithAlpha(aSk),
            core.WithAlpha((byte)0), core.WithAlpha((byte)0),
        };
        var stops = new[]
        {
            0f, MathF.Max(0f, 0.5f - k * 3.4f), MathF.Max(0f, 0.5f - k * 1.5f), 0.5f - k * 0.42f, 0.5f,
            MathF.Min(1f, 0.5f + k * 0.42f), MathF.Min(1f, 0.5f + k * 1.5f), MathF.Min(1f, 0.5f + k * 3.4f), 1f,
        };

        using var comp = new SKPaint { BlendMode = SKBlendMode.Plus };
        canvas.SaveLayer(comp);

        using (var bandShader = SKShader.CreateLinearGradient(p0, p1, colors, stops, SKShaderTileMode.Clamp))
        {
            // 亮底上不要那层模糊晕：它落在纸上就是一片白雾。
            // 暗底上必须有晕：浅字本身的加光量太小，光只存在于字外。
            // 暗底上光只存在于字外：浅字本身的加光量只有十几级灰阶，看不出。
            // 所以晕要「紧而实」——sigma 放大而 alpha 不变等于把 alpha 摊薄，只会更弱。
            var halos = onDark
                ? new[] { (a: 232, sg: 0.22f), (a: 255, sg: 0f) }
                : new[] { (a: 255, sg: 0f) };
            foreach (var (a, sg) in halos)
            {
                using var mp = new SKPaint { IsAntialias = true, Color = SKColors.White };
                if (sg > 0f) mp.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, fs * sg);
                DrawGlyphMask(canvas, tl, tc, mp, (byte)a);
            }

            using var cut = new SKPaint { IsAntialias = false, BlendMode = SKBlendMode.SrcIn, Shader = bandShader };
            canvas.DrawRect(new SKRect(0, 0, size.Width, size.Height), cut);
        }

        canvas.Restore();
    }

    /// <summary>把字形的 alpha 画进当前图层（颜色一律白，只取 alpha）。</summary>
    void DrawGlyphMask(SKCanvas canvas, in Timeline tl, float tc, SKPaint paint, byte alpha)
    {
        var g = _geo;
        float acc = 0f;
        for (int i = 0; i < _c.Text.Length; i++)
        {
            string ch = _c.Text.Substring(i, 1);
            float p = CharP(i, tl, tc);
            if (p > 0.002f)
            {
                paint.Color = SKColors.White.WithAlpha((byte)(alpha * Clamp01(p)));
                float dy = g.Fs * 0.34f * (1f - p);
                canvas.DrawText(ch, g.Left + acc, g.Baseline + dy, SKTextAlign.Left, _fText, paint);
            }
            acc += _fText.GetGlyphWidths(ch, null)[0];
        }
    }

    // ========================================================
    //  浮尘
    // ========================================================
    void DrawDust(SKCanvas canvas, SKSize size, in Timeline tl, float tc)
    {
        if (_motes.Length == 0) return;
        float sc = size.Width / 1920f;
        float p = EaseOutCubic(Seg(tc, tl.At(0.10f), tl.At(0.55f)));
        if (p <= 0.002f) return;

        using var dp = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        foreach (var m in _motes)
        {
            float yy = m.Y - tc * m.Speed * 0.035f;
            yy -= MathF.Floor(yy);
            float xx = m.X + MathF.Sin(tc * 1.2f + m.Phase) * 0.010f;
            dp.Color = _theme.Primary.WithAlpha((byte)((16 + 30 * MathF.Abs(MathF.Sin(tc * 0.9f + m.Phase))) * p));
            canvas.DrawCircle(xx * size.Width, (1f - yy) * size.Height, m.R * sc, dp);
        }
    }

    // ========================================================
    //  几何求解：一行字，只有「字号多大」要算。
    //  横向按版心，竖向按 0.17H 封顶；装饰的内边距从版心里扣掉，
    //  所以套上按钮不会比纯文字更容易碰到边。
    // ========================================================
    static Geo Solve(ContentSetting c, int W, int H, float em)
    {
        var g = new Geo();

        // 装饰要占的地方（以字号为单位）
        float padL = 0f, padR = 0f;
        switch (c.Style)
        {
            case TextStyle.Button: padL = padR = c.StylePad; break;
            case TextStyle.Mark: padL = padR = 0.16f; break;
            case TextStyle.Bar: padL = 0.42f; break;
            case TextStyle.Bracket: padL = padR = 0.52f; break;
            case TextStyle.Underline: padL = padR = 0.02f; break;
        }
        if (c.StyleStrength <= 0.004f) { padL = padR = 0f; }

        float availW = W * 0.86f;
        float emAll = MathF.Max(0.2f, em + padL + padR);
        float fs = MathF.Min(H * 0.17f, availW / emAll);
        g.Fs = fs;
        g.TextW = em * fs;

        // 装饰左右不对称时（左竖条），把整组居中，而不是只让字居中
        float unitW = (em + padL + padR) * fs;
        float unitLeft = W * 0.5f - unitW * 0.5f;
        g.Left = unitLeft + padL * fs;
        g.Right = g.Left + g.TextW;

        g.Top = H * 0.5f - fs * 0.50f;
        g.Bottom = g.Top + fs * 1.00f;
        g.Baseline = g.Top + fs * 0.88f;
        g.MidY = (g.Top + g.Bottom) * 0.5f;

        return g;
    }

    // ========================================================
    //  资源工具
    // ========================================================
    static float MeasureOne(SKFont f, string s)
    {
        var ws = f.GetGlyphWidths(s, null);
        float w = 0f;
        foreach (var v in ws) w += v;
        return w;
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

    /// <summary>字体名：空则用官方默认字体；系统缺字体时退回 SKTypeface.Default。</summary>
    static SKTypeface Pick(string? family, SKFontStyle style, string tag = "")
    {
        var name = string.IsNullOrWhiteSpace(family) ? SKCustomAnimation.GetDefaultFontName() : family.Trim();
        var tf = SKTypeface.FromFamilyName(name, style) ?? SKTypeface.Default;
        Log($"[font/{tag}] {name} → {tf.FamilyName}");
        return tf;
    }

    static void Log(string msg) => Console.WriteLine(msg);

    static string Hex(SKColor c) => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}";

    static SKColor Mix(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            a.Alpha);
    }

    static SKColor Lighten(SKColor c, float t) => Mix(c, SKColors.White, t);
    static SKColor Darken(SKColor c, float t) => Mix(c, SKColors.Black, t);
    /// <summary>相对亮度（0–1）。判断「字比底色亮还是暗」，掠金据此换叠法。</summary>
    static float Lum(SKColor c) => (0.213f * c.Red + 0.715f * c.Green + 0.072f * c.Blue) / 255f;

    static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    static float Seg(float t, float a, float b) => Clamp01((t - a) / MathF.Max(1e-6f, b - a));
    static float EaseOutCubic(float x) { x = Clamp01(x); float u = 1f - x; return 1f - u * u * u; }
    static float EaseInOut(float x) { x = Clamp01(x); return x < 0.5f ? 2f * x * x : 1f - 2f * (1f - x) * (1f - x); }

    // ========================================================
    //  出片
    // ========================================================
    public void Render(string outputPath, int fps = 25, int previewFrames = 0, bool exportGif = false)
    {
        if (fps > 0) _fps = fps;
        string dir = Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".";
        string stem = Path.GetFileNameWithoutExtension(outputPath);
        Directory.CreateDirectory(dir);   // SaveMp4 / SaveGif 不建目录，这里补上

        for (int i = 0; i < previewFrames; i++)
        {
            int fr = previewFrames == 1
                ? Frames - 1
                : (int)MathF.Round(i * (Frames - 1) / (float)(previewFrames - 1));
            if (!SavePNG(fr, Path.Combine(dir, "preview", $"{stem}_f_{fr:000}.png")))
                Console.WriteLine($"[warn] 预览帧 {fr} 写出失败");
        }

        Log($"[time] {Frames} 帧 → 动画 {AnimFrames} 帧（{AnimFrames / (float)_fps:F2}s，{AnimEnd * 100f:F0}%）" +
            $" + 停留 {HoldFrames} 帧（{HoldFrames / (float)_fps:F2}s）；文字起于动画段 {_c.TextStart * 100f:F0}%");

        SaveMp4(_fps, outputPath);

        if (exportGif)
        {
            string gp = Path.ChangeExtension(outputPath, ".gif");
            SaveGif(_fps, gp);
            Log($"GIF → {gp}");
        }

        Log($"完成：{Frames} 帧 / {_fps}fps / {CW}×{CH} / {Duration:F2}s → {outputPath}");
    }

    /// <summary>只渲染某一帧为 PNG，不出视频。</summary>
    public void RenderStill(int frame, string outputPath, int fps = 25)
    {
        if (fps > 0) _fps = fps;
        int fr = Math.Clamp(frame, 0, Frames - 1);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");
        Log(SavePNG(fr, outputPath) ? $"静帧 → {outputPath}" : $"静帧写出失败：{outputPath}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _bgPaint.Dispose();
        _bgShader.Dispose(); _vignetteShader.Dispose(); _grainShader.Dispose(); _glowShader.Dispose();
        _grainBmp.Dispose();
        _fText.Dispose();
        _tfText.Dispose();
    }

    struct Mote
    {
        public float X, Y, R, Speed, Phase;
    }

    // ========================================================
    //  时间线：设计拍点 → 真实秒。与 PaperTitleCardA / CurtainTextA 同一套词汇。
    // ========================================================
    readonly struct Timeline
    {
        const float DText = 0.32f;
        const float DEnd = 0.96f;

        public readonly float AnimEnd;
        public readonly float AnimSeconds;

        readonly float _textStart;
        readonly float _k1;
        readonly float _k2;
        readonly float _stagger;

        public Timeline(float duration, float animEnd, float textStart, float stagger)
        {
            AnimEnd = Math.Clamp(animEnd, 0.02f, 1f);
            AnimSeconds = AnimEnd * MathF.Max(1e-3f, duration);
            _textStart = Math.Clamp(textStart, 0.05f, 0.95f);
            _k1 = _textStart / DText;
            _k2 = (1f - _textStart) / (DEnd - DText);
            _stagger = MathF.Max(0f, stagger);
        }

        /// <summary>文字开始升起的时刻（秒）</summary>
        public float TextAt => _textStart * AnimSeconds;

        /// <summary>拍点 → 秒</summary>
        public float At(float beat) => (beat <= DText
            ? beat * _k1
            : _textStart + (beat - DText) * _k2) * AnimSeconds;

        /// <summary>拍点跨度 → 秒</summary>
        public float Span(float beats) => beats * _k2 * AnimSeconds;

        /// <summary>拍点错峰 → 秒</summary>
        public float Gap(float beats) => beats * _k2 * AnimSeconds * _stagger;

        /// <summary>动画段内的归一化进度</summary>
        public float Progress(float tc) => Math.Clamp(tc / AnimSeconds, 0f, 1f);
    }

    // ========================================================
    //  内容配置
    // ========================================================
    /// <summary>
    /// 单行文字卡的内容与特效。只有一句字，没有副标题、没有起首横线。
    /// 所有 0–1 的特效参数，0 就是关掉那一个。
    /// </summary>
    public sealed class ContentSetting : SKAnimSetting
    {
        // ---------- 内容 ----------
        /// <summary>唯一的一行字。空串 = 空白卡（只有底与光）。</summary>
        public string Text { get; set; } = "";

        // ---------- 装饰样式 ----------
        /// <summary>文字装饰。</summary>
        public TextStyle Style { get; set; } = TextStyle.Plain;
        /// <summary>装饰强度（0 = 不画，1 = 满）。颜色取自主题主色。</summary>
        public float StyleStrength { get; set; } = 1.00f;
        /// <summary>覆盖装饰色。null = 用主题 Primary。</summary>
        public SKColor? StyleColor { get; set; } = null;
        /// <summary>按钮 / 高亮的内边距（字号的倍数）。按钮建议 0.5–0.7</summary>
        public float StylePad { get; set; } = 0.52f;
        /// <summary>按钮圆角（字号的倍数）。0 = 直角，越大越圆</summary>
        public float StyleRadius { get; set; } = 0.30f;
        /// <summary>线宽 / 边框粗细的倍率</summary>
        public float StyleWeight { get; set; } = 1.00f;
        /// <summary>装饰在文字落定后多久开始（拍点）</summary>
        public float StyleAt { get; set; } = 0.04f;
        /// <summary>装饰展开用几拍</summary>
        public float StyleSpan { get; set; } = 0.14f;

        // ---------- 画面 ----------
        /// <summary>主题背光：主色在字背后托一层</summary>
        public float Glow { get; set; } = 0.55f;
        /// <summary>全画面颗粒</summary>
        public float Grain { get; set; } = 0.24f;
        /// <summary>浮尘颗粒数量（0 = 关）</summary>
        public int Dust { get; set; } = 26;
        /// <summary>四角压暗</summary>
        public float Vignette { get; set; } = 0.48f;
        /// <summary>开场压暗强度：0 = 一上来就是定稿</summary>
        public float Reveal { get; set; } = 1.00f;

        // ---------- 掠金 ----------
        /// <summary>掠金：一道斜光扫过字面。0 = 关。单行是它的主场，默认给足</summary>
        public float Glint { get; set; } = 0.90f;
        /// <summary>光带宽度（字块宽度的比例）。窄了像一道冷光，宽了像整块在亮</summary>
        public float GlintWidth { get; set; } = 0.16f;
        /// <summary>光带倾角（度，正值 = 光从左上斜下来）。0 = 正着扫，像扫描线</summary>
        public float GlintTilt { get; set; } = 18f;
        /// <summary>掠过时刻（编排拍点）</summary>
        public float GlintAt { get; set; } = 0.64f;
        /// <summary>掠过的时长（拍点）。越大越慢、越像反光，越小越像闪一下</summary>
        public float GlintSpan { get; set; } = 0.30f;

        // ---------- 时间线：全部是「总帧数的系数」----------
        /// <summary>动画段占全部帧的比例</summary>
        /// <summary>尾帧停留占全部帧的比例；由 AnimRatio 推出，恒为 1 - AnimRatio</summary>
        public float HoldRatio => 1f - AnimRatio;
        /// <summary>文字在动画段的哪个位置开始升起</summary>
        public float TextStart { get; set; } = 0.16f;
        /// <summary>字与字错峰倍率（0 = 一起出现）。单行时它就是「打出来」的节奏</summary>
        public float Stagger { get; set; } = 1.00f;
        /// <summary>逐字错峰的基准间距（拍点）</summary>
        public float CharStagger { get; set; } = 0.80f;

        // ---------- 其他 ----------
        public int Seed { get; set; } = 20261029;
        /// <summary>字体名；为空时使用 SKCustomAnimation.GetDefaultFontName()。</summary>
        public string FontFamily { get; set; } = "";

        /// <summary>动画段结束位置（占全片 0–1）</summary>
        public float AnimEnd => Math.Clamp(AnimRatio, 0.02f, 1f);

        internal void Normalize()
        {
            Text ??= "";
            Glow = Clamp01(Glow); Grain = Clamp01(Grain);
            Vignette = Clamp01(Vignette); Reveal = Clamp01(Reveal);
            StyleStrength = Clamp01(StyleStrength);
            StylePad = Math.Clamp(StylePad, 0f, 1.5f);
            StyleRadius = Math.Clamp(StyleRadius, 0f, 1.0f);
            StyleWeight = Math.Clamp(StyleWeight, 0.3f, 2.5f);
            StyleAt = Math.Clamp(StyleAt, 0f, 0.60f);
            StyleSpan = Math.Clamp(StyleSpan, 0.02f, 0.60f);
            Glint = Clamp01(Glint);
            GlintWidth = Math.Clamp(GlintWidth, 0.05f, 0.60f);
            GlintTilt = Math.Clamp(GlintTilt, -45f, 45f);
            GlintAt = Math.Clamp(GlintAt, 0f, 0.92f);
            GlintSpan = Math.Clamp(GlintSpan, 0.02f, 0.60f);
            TextStart = Math.Clamp(TextStart, 0.05f, 0.95f);
            Stagger = MathF.Max(0f, Stagger);
            CharStagger = MathF.Max(0f, CharStagger);
            Dust = Math.Clamp(Dust, 0, 400);

        }
    }

    // ========================================================
    //  主题：7 个基础色 + 本渲染器专用的 3 个色
    // ========================================================
    /// <summary>
    /// 单行文字卡主题。基础 7 色沿用 NBVideoTheme；
    /// 另补 Glow（背光）、Sweep（亮底掠金的高光）、Shadow（暗角）。
    /// </summary>
    public sealed class ContentTheme : NBVideoTheme
    {
        /// <summary>亮底掠金的高光参考色。</summary>
        public SKColor Sweep { get; set; } = new SKColor(0xFF, 0xF5, 0xE0);

        /// <summary>文字背后的主色背光（Screen）。</summary>
        public SKColor Glow { get; set; } = new SKColor(0x8A, 0x5F, 0x30);

        /// <summary>暗角色；亮底主题可换成暖棕，免得像脏。</summary>
        public SKColor Shadow { get; set; } = new SKColor(0x00, 0x00, 0x00);

        /// <summary>未传入主题时的兜底。</summary>
        public static ContentTheme CreateDefault() => From(NBVideoThemes.Nature);

        /// <summary>从任一主题（含 20 套内置主题）复制出可扩展的 ContentTheme。</summary>
        public static ContentTheme From(NBVideoTheme theme)
        {
            ArgumentNullException.ThrowIfNull(theme);

            return new ContentTheme
            {
                Name = theme.Name,
                Primary = theme.Primary,
                Background = theme.Background,
                TextPrimary = theme.TextPrimary,
                TextSecondary = theme.TextSecondary,
                Divider = theme.Divider,
                Positive = theme.Positive,
                Negative = theme.Negative,
                Glow = theme.Primary,
                Sweep = Mix(SKColors.White, theme.Primary, 0.12f),
            };
        }

        /// <summary>
        /// 夜墨·铜金 —— 与 PaperTitleCardA / CurtainTextA 逐值一致，
        /// 几张卡剪在同一条时间线上时不会跳色。
        /// </summary>
        public static ContentTheme NightCopper()
        {
            var t = From(NBVideoThemes.Luxury);
            t.Name = "夜墨铜金";
            t.Primary = new SKColor(0xC2, 0x91, 0x4E);
            t.Background = new SKColor(0x0A, 0x14, 0x20);
            t.TextPrimary = new SKColor(0xEF, 0xE6, 0xD2);
            t.TextSecondary = new SKColor(0x4E, 0x8F, 0xA0);
            t.Divider = new SKColor(0x4E, 0x47, 0x3E);
            t.Positive = new SKColor(0xCE, 0x4A, 0x32);
            t.Negative = new SKColor(0x3F, 0x7D, 0x5C);
            t.Sweep = new SKColor(0xFF, 0xF5, 0xE0);
            t.Glow = new SKColor(0x8A, 0x5F, 0x30);
            t.Shadow = new SKColor(0x00, 0x00, 0x00);
            return t;
        }

        /// <summary>旧纸·深墨 —— 亮底对照组，验证深字上的掠金走「镀亮金」而不是加光晕。</summary>
        public static ContentTheme PaperDay()
        {
            var t = From(NBVideoThemes.Nature);
            t.Name = "旧纸深墨";
            t.Primary = new SKColor(0x8C, 0x6A, 0x3A);
            t.Background = new SKColor(0xEF, 0xE4, 0xCF);
            t.TextPrimary = new SKColor(0x2A, 0x21, 0x18);
            t.TextSecondary = new SKColor(0x6B, 0x5B, 0x45);
            t.Divider = new SKColor(0xC9, 0xB8, 0x96);
            t.Positive = new SKColor(0xA8, 0x3A, 0x28);
            t.Negative = new SKColor(0x3F, 0x6B, 0x50);
            t.Sweep = new SKColor(0xFF, 0xF3, 0xD8);
            t.Glow = new SKColor(0xFF, 0xE9, 0xC0);
            t.Shadow = new SKColor(0x3A, 0x33, 0x28);
            return t;
        }
    }
}
