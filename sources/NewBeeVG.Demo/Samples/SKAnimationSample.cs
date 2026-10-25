using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class SKAnimationSample
{
    public static void Run()
    {
        SkiaAnim(new SKAnimationHelloWorld(1920, 1080, 120))
            .AsClip(out var clip, 120);
        run(stage(1920, 1080), [clip]);
    }
}

public class MyAnimation : SKCustomAnimation
{
    public MyAnimation(int width, int height, int frames)
        : base(width, height, frames)
    {
    }

    protected override void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
        // 每帧清空背景；位图带 Alpha，可改用 SKColors.Transparent 导出透明背景
        canvas.Clear(new SKColor(0xFF101820));

        // 帧号 -> 0..1 进度
        float t = Frames <= 1 ? 1f : frame / (float)(Frames - 1);

        float cx = size.Width / 2f;
        float cy = size.Height / 2f;

        // 在这里替换为实际绘制逻辑
        using (var paint = new SKPaint { IsAntialias = true, Color = new SKColor(0xFFF0B429) })
        {
            canvas.DrawCircle(cx, cy, 120f + 40f * MathF.Sin(t * MathF.PI * 2f), paint);
        }
    }
}

public sealed class SKAnimationHelloWorld : SKCustomAnimation
{
    // ---------- 文案 ----------
    private const string Title = "HELLO WORLD";
    private const string Subtitle = "> SYSTEM ONLINE";
    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&*+=<>/\\|[]{}";

    // ---------- 时间轴 ----------
    private const float LockStart = 0.12f;    // 首个字符锁定
    private const float LockStep = 0.030f;    // 逐字步进
    private const float LockFlash = 0.13f;    // 锁定闪光时长
    private const float ScanStart = 0.48f;
    private const float ScanEnd = 0.86f;
    private const float SubStart = 0.58f;
    private const float SubChar = 0.016f;     // 每字打字间隔

    // ---------- 配色 ----------
    private static readonly SKColor Cyan = new(0xFF5FE3FF);
    private static readonly SKColor Magenta = new(0xFFFF5FD8);
    private static readonly SKColor Ice = new(0xFFDFF7FF);

    // ---------- 几何 / 资源 ----------
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
    private readonly float _cx;
    private readonly float _textLeft;
    private readonly float _textRight;
    private readonly float _textW;
    private readonly float _baseline;
    private readonly float _capH;
    private readonly float _midY;
    private readonly float _lastLock;

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

    public SKAnimationHelloWorld(int width, int height, int frames) : base(width, height, frames)
    {
        _w = width;
        _h = height;
        _cx = width / 2f;

        _titleTypeface = SKTypeface.FromFamilyName("Bahnschrift", SKFontStyle.Bold)
                         ?? SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
        _monoTypeface = SKTypeface.FromFamilyName("Cascadia Mono")
                        ?? SKTypeface.FromFamilyName("Consolas");

        // 标题字号：按目标字宽收敛
        const float targetW = 1320f;
        using (var probe = new SKFont(_titleTypeface, 210f))
        {
            float tw = probe.GetGlyphWidths(Title, null).Sum();
            float size = tw > 1f ? 210f * targetW / tw : 210f;
            _titleFont = new SKFont(_titleTypeface, size);
        }

        _subFont = new SKFont(_monoTypeface, 27f);
        _microFont = new SKFont(_monoTypeface, 15f);

        _charW = _titleFont.GetGlyphWidths(Title, null);
        _textW = _charW.Sum();
        _charX = new float[Title.Length];
        {
            float acc = 0f;
            for (int i = 0; i < Title.Length; i++) { _charX[i] = acc; acc += _charW[i]; }
        }

        _textLeft = _cx - _textW / 2f;
        _textRight = _cx + _textW / 2f;

        var fm = _titleFont.Metrics;
        _capH = MathF.Abs(fm.CapHeight);
        _baseline = height * 0.50f + _capH * 0.30f;
        _midY = _baseline - _capH * 0.5f;

        // 逐字锁定时刻（跳过空格计时）
        _lockT = new float[Title.Length];
        int seq = 0;
        for (int i = 0; i < Title.Length; i++)
        {
            _lockT[i] = LockStart + seq * LockStep;
            if (Title[i] != ' ') seq++;
        }
        _lastLock = LockStart + (seq - 1) * LockStep;

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

        float yTop = _baseline - _capH;
        _textShader = SKShader.CreateLinearGradient(
            new SKPoint(0, yTop - 8f), new SKPoint(0, _baseline + 10f),
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

        canvas.Clear(new SKColor(0xFF04060C));

        // 极轻推镜，让静止画面保持呼吸
        float zoom = 1f + 0.030f * EaseInOut(t);
        canvas.Save();
        canvas.Translate(_cx, _h * 0.5f);
        canvas.Scale(zoom, zoom);
        canvas.Translate(-_cx, -_h * 0.5f);

        DrawBackground(canvas, t);
        DrawGrid(canvas, t);
        DrawAura(canvas, t);
        DrawTitle(canvas, t, frame);
        DrawHud(canvas, t, frame);
        DrawSubtitle(canvas, t);
        DrawScanline(canvas, t);

        canvas.Restore();

        DrawVignette(canvas);
        DrawGlobalFlash(canvas, t);
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

        using (var minor = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = false,
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(11 * grow))
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
            Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, (byte)(26 * grow))
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
        float lockP = Clamp01((t - LockStart) / (LockStep * 10f));
        float a = 0.08f + 0.30f * lockP + 0.05f * MathF.Sin(t * 7.2f);
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

        if (split > 0.4f)
        {
            byte fa = (byte)(Math.Min(104f, 16f + split * 5f));
            using var fringe = new SKPaint
            {
                IsAntialias = true,
                BlendMode = SKBlendMode.Plus,
                Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, fa)
            };
            canvas.DrawText(Title, _textLeft - split, _baseline, SKTextAlign.Left, _titleFont, fringe);
            fringe.Color = new SKColor(Magenta.Red, Magenta.Green, Magenta.Blue, fa);
            canvas.DrawText(Title, _textLeft + split, _baseline, SKTextAlign.Left, _titleFont, fringe);
        }

        // 全字锁定后的整体辉光（一次模糊绘制，成本可控）
        if (t >= _lastLock)
        {
            using var glow = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(Cyan.Red, Cyan.Green, Cyan.Blue, 46),
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 26f)
            };
            canvas.DrawText(Title, _textLeft, _baseline, SKTextAlign.Left, _titleFont, glow);
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

        for (int i = 0; i < Title.Length; i++)
        {
            if (Title[i] == ' ') continue;

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
                if (c == Title.Substring(i, 1)) c = Charset.Substring((ci + 7) % Charset.Length, 1);

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

            canvas.DrawText(Title.Substring(i, 1), x0, _baseline, SKTextAlign.Left, _titleFont, fill);

            if (flash > 0.01f)
            {
                hot.Color = new SKColor(Ice.Red, Ice.Green, Ice.Blue, (byte)(230 * flash * flash));
                canvas.DrawText(Title.Substring(i, 1), x0, _baseline, SKTextAlign.Left, _titleFont, hot);
            }
            canvas.Restore();

            // 锁定时上下各闪一段短刻度线
            if (flash > 0.02f)
            {
                tick.Color = new SKColor(Magenta.Red, Magenta.Green, Magenta.Blue, (byte)(210 * flash));
                float l = 26f * EaseOut(flash);
                float cxm = x0 + w * 0.5f;
                canvas.DrawLine(cxm, _baseline - _capH - 14f, cxm, _baseline - _capH - 14f - l, tick);
                canvas.DrawLine(cxm, _baseline + 14f, cxm, _baseline + 14f + l, tick);
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
        canvas.DrawText("SEQ/0F3A-2026", inset + 22f, _h - inset - 22f, SKTextAlign.Left, _microFont, microDim);
        canvas.DrawText($"FRM/{frame:000}", _w - inset - 22f, _h - inset - 22f, SKTextAlign.Right, _microFont, microDim);

        // 标题下的装饰线：从中心向两侧展开
        float lineP = EaseOut(Clamp01((t - 0.16f) / 0.34f));
        if (lineP > 0.001f)
        {
            float y = _baseline + 64f;
            float half = (_textW * 0.5f + 30f) * lineP;
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
        int n = Math.Min(Subtitle.Length, (int)typed);
        if (n <= 0) return;

        const float tracking = 3.5f;
        float[] sw = _subFont.GetGlyphWidths(Subtitle, null);
        float total = sw.Sum() + tracking * (Subtitle.Length - 1);

        float x = _cx - total / 2f;
        float y = _baseline + 132f;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0xCCB9D6F2)
        };

        for (int i = 0; i < n; i++)
        {
            string c = Subtitle.Substring(i, 1);
            if (c != " ") canvas.DrawText(c, x, y, SKTextAlign.Left, _subFont, paint);
            x += sw[i] + tracking;
        }

        // 光标：打字时实心，打完闪烁
        bool done = n >= Subtitle.Length;
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

        using (var trail = new SKPaint
        {
            IsAntialias = false,
            Shader = _scanShader,
            BlendMode = SKBlendMode.Plus
        })
        {
            canvas.DrawRect(new SKRect(0, -190f, _w, 0f), trail);
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
            canvas.DrawLine(0, 0, _w, 0, core);
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
            canvas.DrawLine(0, 0, _w, 0, haze);
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
        // 入场黑场
        float black = 1f - EaseOut(Clamp01(t / 0.07f));
        if (black > 0.003f)
        {
            using var bp = new SKPaint { Color = new SKColor(0, 0, 0, (byte)(255 * black)) };
            canvas.DrawRect(new SKRect(0, 0, _w, _h), bp);
        }

        // 全字锁定瞬间：中心径向的白光脉冲（加法混合，不压暗四角）
        float pulse = 0f;
        if (t >= _lastLock && t <= _lastLock + 0.10f)
            pulse = 1f - (t - _lastLock) / 0.10f;

        if (pulse > 0.012f)
        {
            using var sh = SKShader.CreateRadialGradient(
                new SKPoint(_cx, _midY), 940f,
                new[]
                {
                    new SKColor(206, 246, 255, (byte)(140 * pulse)),
                    new SKColor(110, 198, 255, (byte)(44 * pulse)),
                    new SKColor(0, 0, 0, 0)
                },
                new[] { 0f, 0.44f, 1f }, SKShaderTileMode.Clamp);
            using var p = new SKPaint { Shader = sh, BlendMode = SKBlendMode.Plus };
            canvas.DrawRect(new SKRect(0, 0, _w, _h), p);
        }
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
