// ------------------------------------------------------------
//  动画本体：SKCustomAnimation 子类
//  file-based app 约定：顶层语句在前，类型声明在文件末尾
// ------------------------------------------------------------
public sealed class TextIntroAnimation : SKCustomAnimation
{
    // ---------- 文案约束 ----------
    public const string DefaultText = "Hello World";   // 未传文案时的示例
    public const int MinChars = 1;
    public const int MaxChars = 12;

    /// <summary>清洗后的实际文案（去空白、压空格）。</summary>
    public string Text => _text;

    /// <summary>进文件名用的安全片段。</summary>
    public string Slug { get; }

    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&*+=<>/\\|[]{}";

    // ---------- 时间轴 ----------
    private const float LockStart = 0.12f;    // 首个字符开始解码
    private const float LockSpan = 0.30f;     // 全部锁定完成的目标跨度（自适应摊分）
    private const float LockFlash = 0.13f;    // 锁定闪光时长
    private const float ScanStart = 0.48f;
    private const float ScanEnd = 0.86f;
    private const float SubStart = 0.58f;
    private const float SubChar = 0.016f;     // 每字打字间隔

    // ---------- 版式比例（按画布比例算，换分辨率不用改） ----------
    private const float TargetWidthRatio = 0.688f;   // 标题目标宽度 / 画布宽
    private const float InkHeightRatio = 0.400f;     // 字形墨迹最大高度 / 画布高
    private const float TrackingEm = 0.012f;         // 字距（相对字号）

    // ---------- 配色 ----------
    private static readonly SKColor Cyan = new(0xFF5FE3FF);
    private static readonly SKColor Magenta = new(0xFFFF5FD8);
    private static readonly SKColor Ice = new(0xFFDFF7FF);
    private static readonly SKColor Backdrop = new(0xFF04060C);

    // ---------- 文案 / 几何 / 资源 ----------
    private readonly string _text;
    private readonly string _subtitle;
    private readonly string[] _glyphs;        // 逐字预切，避免每帧 Substring
    private readonly int _glyphCount;         // 非空格字符数
    private readonly bool _transparent;

    private readonly int _w;
    private readonly int _h;
    private readonly SKTypeface _titleTypeface;
    private readonly SKTypeface _monoTypeface;
    private readonly SKFont _titleFont;
    private readonly SKFont _subFont;
    private readonly SKFont _microFont;

    private readonly float[] _charX;
    private readonly float[] _charW;
    private readonly float[] _lockT;
    private readonly float _lockStep;
    private readonly float _cx;
    private readonly float _textLeft;
    private readonly float _textRight;
    private readonly float _textW;
    private readonly float _baseline;
    private readonly float _inkTop;           // 墨迹上沿（相对 baseline，负值）
    private readonly float _inkBottom;        // 墨迹下沿（相对 baseline，含降部）
    private readonly float _inkH;             // 墨迹总高
    private readonly float _midY;
    private readonly float _lastLock;
    private readonly float _fontSize;

    private readonly SKShader _bgShader;
    private readonly SKShader _vignetteShader;
    private readonly SKShader _gridMaskShader;
    private readonly SKShader _textShader;
    private readonly SKShader _auraShader;
    private readonly SKShader _scanShader;

    // 故障闪烁窗口：(起始, 持续)
    private static readonly (float Start, float Dur)[] GlitchWindows =
    {
        (0.505f, 0.050f),
        (0.640f, 0.032f),
        (0.760f, 0.026f)
    };

    public TextIntroAnimation(int width, int height, int frames, string text,
                     string? subtitle = null, bool transparent = false)
        : base(width, height, frames)
    {
        // 文案在这里统一清洗与校验：非法文本直接抛 ArgumentException，不进入渲染
        _text = NormalizeText(text);
        Slug = ToSlug(_text);

        _subtitle = subtitle ?? "> SYSTEM ONLINE";
        _transparent = transparent;
        _w = width;
        _h = height;
        _cx = width / 2f;

        _glyphs = new string[_text.Length];
        for (int i = 0; i < _text.Length; i++) _glyphs[i] = _text.Substring(i, 1);

        _glyphCount = 0;
        foreach (char c in _text) if (c != ' ') _glyphCount++;
        _glyphCount = Math.Max(1, _glyphCount);

        _titleTypeface = SKTypeface.FromFamilyName("Bahnschrift", SKFontStyle.Bold)
                         ?? SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
        _monoTypeface = SKTypeface.FromFamilyName("Cascadia Mono")
                        ?? SKTypeface.FromFamilyName("Consolas");

        // ----- 用 100pt 探测：每 pt 的字宽 + 实际墨迹上下沿 -----
        // 注意：不能只看 Metrics.CapHeight。Hello World 里 l 比 H 高、y 有降部，
        // 用小写混合时按 cap 高度算会导致字号偏移、垂直位置发飘。
        float wPerPt;
        float inkTopPt;
        float inkBotPt;

        using (var probe = new SKFont(_titleTypeface, 100f))
        {
            // GetGlyphWidths 的 out 版本能同时拿到每字宽度与墨迹框（相对 baseline）
            float[] widths = probe.GetGlyphWidths(_text, out SKRect[] boxes, null);

            float sum = 0f;
            foreach (float w in widths) sum += w;
            wPerPt = sum / 100f;

            bool first = true;
            inkTopPt = 0f;
            inkBotPt = 0f;
            for (int i = 0; i < boxes.Length; i++)
            {
                if (_text[i] == ' ') continue;
                SKRect r = boxes[i];
                if (r.Height <= 0f) continue;
                if (first) { inkTopPt = r.Top; inkBotPt = r.Bottom; first = false; }
                else
                {
                    inkTopPt = MathF.Min(inkTopPt, r.Top);
                    inkBotPt = MathF.Max(inkBotPt, r.Bottom);
                }
            }
            if (first) { inkTopPt = -70f; inkBotPt = 0f; }   // 全是空格时兜底
        }

        // ----- 字号自适应：宽度收敛 + 墨迹高度封顶 -----
        float targetW = width * TargetWidthRatio;
        float trackingPerPt = TrackingEm * (_text.Length - 1);
        float inkPerPt = MathF.Max(0.2f, (inkBotPt - inkTopPt) / 100f);

        float sizeByWidth = wPerPt + trackingPerPt > 0.0001f
            ? targetW / (wPerPt + trackingPerPt)
            : targetW;

        // 关键补丁：字符很少时 sizeByWidth 会非常大（1 个字符能到 1300pt 以上），
        // 必须用墨迹高度封顶，否则字形直接顶出画面
        float sizeByHeight = (height * InkHeightRatio) / inkPerPt;

        _fontSize = MathF.Max(24f, MathF.Min(sizeByWidth, sizeByHeight));
        _titleFont = new SKFont(_titleTypeface, _fontSize);

        _subFont = new SKFont(_monoTypeface, 27f);
        _microFont = new SKFont(_monoTypeface, 15f);

        // ----- 逐字排版（含字距） -----
        _charW = _titleFont.GetGlyphWidths(_text, null);
        float tracking = _fontSize * TrackingEm;
        _charX = new float[_text.Length];
        {
            float acc = 0f;
            for (int i = 0; i < _text.Length; i++)
            {
                _charX[i] = acc;
                acc += _charW[i] + tracking;
            }
            _textW = acc - tracking;   // 末字后面不留字距
        }

        _textLeft = _cx - _textW / 2f;
        _textRight = _cx + _textW / 2f;

        // ----- 垂直定位：让「墨迹框」中心落在 46% 高处（视觉重心略偏上） -----
        float scale = _fontSize / 100f;
        _inkTop = inkTopPt * scale;
        _inkBottom = inkBotPt * scale;
        _inkH = _inkBottom - _inkTop;
        _baseline = height * 0.46f - (_inkTop + _inkBottom) * 0.5f;
        _midY = _baseline + (_inkTop + _inkBottom) * 0.5f;

        // ----- 逐字锁定时刻：总跨度归一，字数多少都读得舒服 -----
        _lockStep = _glyphCount <= 1
            ? 0f
            : Math.Clamp(LockSpan / (_glyphCount - 1), 0.022f, 0.055f);

        _lockT = new float[_text.Length];
        int seq = 0;
        for (int i = 0; i < _text.Length; i++)
        {
            _lockT[i] = LockStart + seq * _lockStep;
            if (_text[i] != ' ') seq++;
        }
        _lastLock = LockStart + (_glyphCount - 1) * _lockStep;

        // ---------- 缓存着色器 ----------
        _bgShader = SKShader.CreateRadialGradient(
            new SKPoint(_cx, _h * 0.42f), 1500f,
            new[] { new SKColor(0xFF0E1A33), new SKColor(0xFF070C18), new SKColor(0xFF04060C) },
            new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp);

        _vignetteShader = SKShader.CreateRadialGradient(
            new SKPoint(_cx, _h * 0.5f), 1150f,
            new[] { new SKColor(0x00000000), new SKColor(0x00000000), new SKColor(0x9E000000) },
            new[] { 0f, 0.58f, 1f }, SKShaderTileMode.Clamp);

        _gridMaskShader = SKShader.CreateRadialGradient(
            new SKPoint(_cx, _midY), 1080f,
            new[] { new SKColor(0xFFFFFFFF), new SKColor(0xFFFFFFFF), new SKColor(0x00FFFFFF) },
            new[] { 0f, 0.42f, 1f }, SKShaderTileMode.Clamp);

        _textShader = SKShader.CreateLinearGradient(
            new SKPoint(0, _baseline + _inkTop - 8f), new SKPoint(0, _baseline + _inkBottom + 10f),
            new[] { new SKColor(0xFFFFFFFF), new SKColor(0xFF9FEFFF), new SKColor(0xFF46CFFF), new SKColor(0xFF8E8BFF) },
            new[] { 0f, 0.20f, 0.58f, 1f }, SKShaderTileMode.Clamp);

        _auraShader = SKShader.CreateRadialGradient(
            new SKPoint(_cx, _midY), 820f,
            new[] { new SKColor(0x8C4FD8FF), new SKColor(0x2E8B7BFF), new SKColor(0x004FD8FF) },
            new[] { 0f, 0.44f, 1f }, SKShaderTileMode.Clamp);

        _scanShader = SKShader.CreateLinearGradient(
            new SKPoint(0, -190f), new SKPoint(0, 0f),
            new[] { new SKColor(0x0000E5FF), new SKColor(0x2400E5FF), new SKColor(0x7A9FEEFF) },
            new[] { 0f, 0.68f, 1f }, SKShaderTileMode.Clamp);
    }

    // ================= 主绘制 =================
    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

        // 透明模式必须清成透明，否则 alpha 通道全被底色填死
        if(_transparent != true)
            canvas.Clear(_transparent ? SKColors.Transparent : Backdrop);

        // 透明模式下的入场淡入：不能再用「盖一层黑」，改用图层 alpha 调制，
        // 这样叠到任何底图上都是干净地浮现
        float enter = _transparent ? EaseOut(Clamp01(t / 0.09f)) : 1f;
        bool useEnterLayer = _transparent && enter < 0.999f;
        if (useEnterLayer)
        {
            using var layerPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, (byte)(255 * enter))
            };
            canvas.SaveLayer(layerPaint);
        }

        // 极轻推镜，让静止画面保持呼吸
        float zoom = 1f + 0.030f * EaseInOut(t);
        canvas.Save();
        canvas.Translate(_cx, _h * 0.5f);
        canvas.Scale(zoom, zoom);
        canvas.Translate(-_cx, -_h * 0.5f);

        if (!_transparent) DrawBackground(canvas, t);
        DrawGrid(canvas, t);
        DrawAura(canvas, t);
        DrawTitle(canvas, t, frame);
        DrawHud(canvas, t, frame);
        DrawSubtitle(canvas, t);
        DrawScanline(canvas, t);

        canvas.Restore();

        if (!_transparent) DrawVignette(canvas);
        DrawGlobalFlash(canvas, t);

        if (useEnterLayer) canvas.Restore();
    }

    // ================= 背景 =================
    private void DrawBackground(SKCanvas canvas, float t)
    {
        using var paint = new SKPaint { Shader = _bgShader };
        canvas.DrawRect(new SKRect(0, 0, _w, _h), paint);
    }

    private void DrawGrid(SKCanvas canvas, float t)
    {
        float grow = Clamp01((t - 0.008f) / 0.20f);
        if (grow <= 0.001f) return;

        canvas.SaveLayer();

        const float step = 96f;
        float drift = (t * 28f) % step;

        // 透明底上网格会直接叠到别人画面上，alpha 压到 45%
        float mul = _transparent ? 0.45f : 1f;

        using (var minor = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = false,
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(11 * grow * mul))
        })
        {
            for (float x = 0; x <= _w + 1; x += step) canvas.DrawLine(x, 0, x, _h, minor);
            for (float y = -step + drift; y <= _h + step; y += step) canvas.DrawLine(0, y, _w, y, minor);
        }

        using (var major = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = false,
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(26 * grow * mul))
        })
        {
            for (float x = 0; x <= _w + 1; x += step * 4f) canvas.DrawLine(x, 0, x, _h, major);
            for (float y = -step * 4f + drift; y <= _h + step * 4f; y += step * 4f) canvas.DrawLine(0, y, _w, y, major);
        }

        // 径向遮罩：中心清晰、四周淡出
        using (var mask = new SKPaint { Shader = _gridMaskShader, BlendMode = SKBlendMode.DstIn })
            canvas.DrawRect(new SKRect(0, 0, _w, _h), mask);

        canvas.Restore();
    }

    private void DrawAura(SKCanvas canvas, float t)
    {
        // 光晕强度跟随「解码进度」，跨度随字符数变化，不能写死步进倍数
        float span = MathF.Max(0.06f, _lastLock - LockStart + _lockStep);
        float lockP = Clamp01((t - LockStart) / span);
        float a = 0.08f + 0.30f * lockP + 0.05f * MathF.Sin(t * 7.2f);

        // 透明模式：大面积青色光会在实拍画面上形成一层灰雾，压到 30%
        if (_transparent) a *= 0.30f;

        using var paint = new SKPaint
        {
            Shader = _auraShader,
            Color = new SKColor(255, 255, 255, (byte)(Clamp01(a) * 255)),
            BlendMode = SKBlendMode.Plus
        };
        canvas.DrawRect(new SKRect(0, 0, _w, _h), paint);
    }

    // ================= 标题 =================
    private void DrawTitle(SKCanvas canvas, float t, int frame)
    {
        if (t < 0.05f) return;

        float appear = Clamp01((t - 0.05f) / 0.05f);

        // 色散：扫描线靠近文字时最强，故障窗口内额外抖动
        float split = 0f;
        if (t >= _lastLock)
        {
            float prox = ScanProximity(t);
            split = 7f * prox + 9f * GlitchPulse(t);
        }

        // 注意：文字带字距，整串绘制会与逐字绘制错位，因此色散/辉光也必须逐字画
        if (split > 0.4f)
        {
            byte fa = (byte)(Math.Min(104f, 16f + split * 5f));
            using var fringe = new SKPaint
            {
                IsAntialias = true,
                BlendMode = SKBlendMode.Plus,
                Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, fa)
            };
            for (int i = 0; i < _text.Length; i++)
            {
                if (_text[i] == ' ') continue;
                float x0 = _textLeft + _charX[i];
                canvas.DrawText(_glyphs[i], x0 - split, _baseline, SKTextAlign.Left, _titleFont, fringe);
            }
            fringe.Color = new SKColor(Magenta.Red, Magenta.Green, Magenta.Blue, fa);
            for (int i = 0; i < _text.Length; i++)
            {
                if (_text[i] == ' ') continue;
                float x0 = _textLeft + _charX[i];
                canvas.DrawText(_glyphs[i], x0 + split, _baseline, SKTextAlign.Left, _titleFont, fringe);
            }
        }

        // 全字锁定后的整体辉光
        if (t >= _lastLock)
        {
            using var glow = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, 46),
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, MathF.Max(12f, _inkH * 0.14f))
            };
            for (int i = 0; i < _text.Length; i++)
            {
                if (_text[i] == ' ') continue;
                canvas.DrawText(_glyphs[i], _textLeft + _charX[i], _baseline, SKTextAlign.Left, _titleFont, glow);
            }
        }

        using var fill = new SKPaint { IsAntialias = true, Shader = _textShader };
        using var hot = new SKPaint { IsAntialias = true, Color = Ice, BlendMode = SKBlendMode.Plus };
        using var ghost = new SKPaint { IsAntialias = true, Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, 150) };
        using var tick = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = Magenta
        };

        for (int i = 0; i < _text.Length; i++)
        {
            if (_text[i] == ' ') continue;

            float x0 = _textLeft + _charX[i];
            float w = _charW[i];
            float age = t - _lockT[i];
            bool locked = age >= 0f;

            if (!locked)
            {
                // 解码中：随机字符在字格内跳动
                int seed = frame / 2;
                float r = Hash(i, seed);
                int ci = (int)(r * (Charset.Length - 1));
                string c = Charset.Substring(ci, 1);
                if (c == _glyphs[i]) c = Charset.Substring((ci + 7) % Charset.Length, 1);

                float jitter = (Hash(i, seed + 911) - 0.5f) * 7f;
                ghost.Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(150 * appear));
                canvas.DrawText(c, x0 + w * 0.5f, _baseline + jitter, SKTextAlign.Center, _titleFont, ghost);
                continue;
            }

            float flash = Clamp01(1f - age / LockFlash);
            float scale = 1f + 0.05f * EaseOut(flash);

            canvas.Save();
            canvas.Translate(x0 + w * 0.5f, _midY);
            canvas.Scale(scale, scale);
            canvas.Translate(-(x0 + w * 0.5f), -_midY);

            canvas.DrawText(_glyphs[i], x0, _baseline, SKTextAlign.Left, _titleFont, fill);

            if (flash > 0.01f)
            {
                hot.Color = new SKColor(Ice.Red, Ice.Green, Ice.Blue, (byte)(230 * flash * flash));
                canvas.DrawText(_glyphs[i], x0, _baseline, SKTextAlign.Left, _titleFont, hot);
            }
            canvas.Restore();

            // 锁定时上下各闪一段短刻度线
            if (flash > 0.02f)
            {
                tick.Color = new SKColor(Magenta.Red, Magenta.Green, Magenta.Blue, (byte)(210 * flash));
                float l = 26f * EaseOut(flash);
                float cxm = x0 + w * 0.5f;
                float topTick = _baseline + _inkTop - 14f;
                float botTick = _baseline + _inkBottom + 14f;
                canvas.DrawLine(cxm, topTick, cxm, topTick - l, tick);
                canvas.DrawLine(cxm, botTick, cxm, botTick + l, tick);
            }
        }
    }

    // ================= HUD 装饰 =================
    private void DrawHud(SKCanvas canvas, float t, int frame)
    {
        float grow = EaseOut(Clamp01((t - 0.04f) / 0.22f));
        if (grow <= 0.001f) return;

        const float inset = 64f;
        float arm = 62f * grow;

        using var bracket = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(130 * grow))
        };

        // 四角括号
        DrawCorner(canvas, inset, inset, 1, 1, arm, bracket);
        DrawCorner(canvas, _w - inset, inset, -1, 1, arm, bracket);
        DrawCorner(canvas, inset, _h - inset, 1, -1, arm, bracket);
        DrawCorner(canvas, _w - inset, _h - inset, -1, -1, arm, bracket);

        // 角标读数
        using var micro = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(120 * grow))
        };
        canvas.DrawText("NBVG/RENDER-ENGINE", inset + 22f, inset + 34f, SKTextAlign.Left, _microFont, micro);
        canvas.DrawText($"{_w}x{_h}/{Frames}F", _w - inset - 22f, inset + 34f, SKTextAlign.Right, _microFont, micro);

        using var microDim = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(70 * grow))
        };
        // 左下角显示实际字形数，方便确认自适应生效
        canvas.DrawText($"GLYPH/{_glyphCount:00}", inset + 22f, _h - inset - 22f, SKTextAlign.Left, _microFont, microDim);
        canvas.DrawText($"FRM/{frame:000}", _w - inset - 22f, _h - inset - 22f, SKTextAlign.Right, _microFont, microDim);

        // 标题下的装饰线：从中心向两侧展开
        float lineP = EaseOut(Clamp01((t - 0.16f) / 0.34f));
        if (lineP > 0.001f)
        {
            float y = MathF.Min(_baseline + _inkBottom + 64f, _baseline + 120f);
            // 最短 210px：单字符时线条才不会缩成一个点
            float half = (MathF.Max(_textW * 0.5f, 210f) + 30f) * lineP;
            using var lineShader = SKShader.CreateLinearGradient(
                new SKPoint(_cx - half, y), new SKPoint(_cx + half, y),
                new[] { new SKColor(0x005FE3FF), new SKColor(0xE6BFEFFF), new SKColor(0x005FE3FF) },
                new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
            using var line = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.4f,
                Shader = lineShader
            };
            canvas.DrawLine(_cx - half, y, _cx + half, y, line);

            using var node = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(Magenta.Red, Magenta.Green, Magenta.Blue, (byte)(235 * lineP))
            };
            canvas.DrawCircle(_cx, y, 3.4f * lineP, node);

            // 两端小刻度
            using var endTick = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.4f,
                Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(150 * lineP))
            };
            canvas.DrawLine(_cx - half, y - 7f, _cx - half, y + 7f, endTick);
            canvas.DrawLine(_cx + half, y - 7f, _cx + half, y + 7f, endTick);
        }
    }

    private static void DrawCorner(SKCanvas canvas, float x, float y, int sx, int sy, float arm, SKPaint paint)
    {
        canvas.DrawLine(x, y, x + arm * sx, y, paint);
        canvas.DrawLine(x, y, x, y + arm * sy, paint);
    }

    // ================= 副标题（打字机） =================
    private void DrawSubtitle(SKCanvas canvas, float t)
    {
        if (t < SubStart) return;

        float typed = (t - SubStart) / SubChar;
        int n = Math.Min(_subtitle.Length, (int)typed);
        if (n <= 0) return;

        const float tracking = 3.5f;
        float[] sw = _subFont.GetGlyphWidths(_subtitle, null);
        float total = sw.Sum() + tracking * (_subtitle.Length - 1);

        float x = _cx - total / 2f;
        float y = _baseline + _inkBottom + 132f;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0xCCB9D6F2)
        };

        for (int i = 0; i < n; i++)
        {
            string c = _subtitle.Substring(i, 1);
            if (c != " ") canvas.DrawText(c, x, y, SKTextAlign.Left, _subFont, paint);
            x += sw[i] + tracking;
        }

        // 光标：打字时实心，打完闪烁
        bool done = n >= _subtitle.Length;
        bool on = done ? (t * 4.2f) % 1f < 0.55f : true;
        if (on)
        {
            using var cursor = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(Magenta.Red, Magenta.Green, Magenta.Blue, 235)
            };
            float cw = sw[0] * 0.62f;
            canvas.DrawRect(new SKRect(x - 2f, y - 21f, x - 2f + cw, y + 6f), cursor);
        }
    }

    // ================= 扫描线 =================
    private static float ScanY(float t, int h)
    {
        float p = Clamp01((t - ScanStart) / (ScanEnd - ScanStart));
        if (p <= 0f || p >= 1f) return -9999f;
        // 轻微非线性：起步快、收尾慢
        float e = MathF.Pow(p, 0.88f);
        return Lerp(-80f, h + 80f, e);
    }

    private float ScanProximity(float t)
    {
        float y = ScanY(t, _h);
        if (y < -1000f) return 0f;
        float d = (y - _midY) / 120f;
        return MathF.Exp(-d * d);
    }

    private static float GlitchPulse(float t)
    {
        float g = 0f;
        foreach (var (start, dur) in GlitchWindows)
        {
            if (t < start || t > start + dur) continue;
            float u = (t - start) / dur;
            g = MathF.Max(g, MathF.Sin(u * MathF.PI));
        }
        return g;
    }

    private void DrawScanline(SKCanvas canvas, float t)
    {
        float y = ScanY(t, _h);
        if (y < -1000f) return;

        canvas.Save();
        canvas.Translate(0, y);

        // 透明模式：整屏横扫的青带叠到实拍上会很扎眼，只保留文字区宽度并压低
        float w = _transparent ? MathF.Min(_w, _textW + 260f) : _w;
        float x0 = (_w - w) * 0.5f;

        using (var trail = new SKPaint
        {
            IsAntialias = false,
            Shader = _scanShader,
            BlendMode = SKBlendMode.Plus
        })
        {
            canvas.DrawRect(new SKRect(x0, -190f, x0 + w, 0f), trail);
        }

        using (var core = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0xB4F4FFFF),
            StrokeWidth = 1.9f,
            Style = SKPaintStyle.Stroke,
            BlendMode = SKBlendMode.Plus
        })
        {
            canvas.DrawLine(x0, 0, x0 + w, 0, core);
        }

        using (var haze = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, 24),
            StrokeWidth = 22f,
            Style = SKPaintStyle.Stroke,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14f),
            BlendMode = SKBlendMode.Plus
        })
        {
            canvas.DrawLine(x0, 0, x0 + w, 0, haze);
        }

        canvas.Restore();
    }

    // ================= 收尾遮罩 =================
    private void DrawVignette(SKCanvas canvas)
    {
        using var paint = new SKPaint { Shader = _vignetteShader };
        canvas.DrawRect(new SKRect(0, 0, _w, _h), paint);
    }

    private void DrawGlobalFlash(SKCanvas canvas, float t)
    {
        // 入场黑场：仅不透明模式需要（透明模式已用图层 alpha 淡入）
        if (!_transparent)
        {
            float black = 1f - EaseOut(Clamp01(t / 0.07f));
            if (black > 0.003f)
            {
                using var bp = new SKPaint { Color = new SKColor(0, 0, 0, (byte)(255 * black)) };
                canvas.DrawRect(new SKRect(0, 0, _w, _h), bp);
            }
        }

        // 全字锁定瞬间：中心径向的白光脉冲（加法混合，不压暗四角）
        float pulse = 0f;
        if (t >= _lastLock && t <= _lastLock + 0.10f)
            pulse = 1f - (t - _lastLock) / 0.10f;

        if (pulse > 0.012f)
        {
            float peak = _transparent ? 90f : 140f;
            using var sh = SKShader.CreateRadialGradient(
                new SKPoint(_cx, _midY), 940f,
                new[]
                {
                    new SKColor(206, 246, 255, (byte)(peak * pulse)),
                    new SKColor(110, 198, 255, (byte)(peak * 0.31f * pulse)),
                    new SKColor(0, 0, 0, 0)
                },
                new[] { 0f, 0.44f, 1f }, SKShaderTileMode.Clamp);
            using var p = new SKPaint { Shader = sh, BlendMode = SKBlendMode.Plus };
            canvas.DrawRect(new SKRect(0, 0, _w, _h), p);
        }
    }

    // ================= 文案清洗 / 命名 =================
    /// <summary>
    /// 去首尾空白、压缩连续空格、校验字符集与长度，保留原始大小写。
    /// 不合法时抛 ArgumentException，由调用方决定如何提示。
    /// </summary>
    private static string NormalizeText(string raw)
    {
        var sb = new System.Text.StringBuilder(raw.Length);
        bool prevSpace = false;

        foreach (char c in raw.Trim())
        {
            if (c < ' ' || c > '~')
                throw new ArgumentException(
                    $"只支持英文/数字/常用符号（可见 ASCII），不支持的字符：U+{(int)c:X4} '{c}'");

            if (c == ' ')
            {
                if (prevSpace) continue;   // 连续空格压成一个
                prevSpace = true;
            }
            else prevSpace = false;

            sb.Append(c);
        }

        string s = sb.ToString();
        if (s.Length < MinChars)
            throw new ArgumentException($"文本不能为空，请输入 {MinChars}-{MaxChars} 个英文字符。");
        if (s.Length > MaxChars)
            throw new ArgumentException(
                $"文本共 {s.Length} 个字符（含空格），超过上限 {MaxChars}。请缩短后再试。");

        return s;
    }

    /// <summary>把文案变成安全的文件名片段：非字母数字一律替成下划线。</summary>
    private static string ToSlug(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text.ToLowerInvariant())
        {
            if (c >= 'a' && c <= 'z') sb.Append(c);
            else if (c >= '0' && c <= '9') sb.Append(c);
            else sb.Append('_');
        }
        return sb.ToString();
    }

    // ================= 工具 =================
    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    private static float EaseOut(float t) => 1f - MathF.Pow(1f - t, 3f);
    private static float EaseInOut(float t) => t * t * (3f - 2f * t);

    private static float Hash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
