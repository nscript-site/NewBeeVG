using SkiaSharp;
using SkiaSharp.Skottie;

namespace NewBeeVG;

public enum NBAnimateLoopMode
{
    Loop,
    AutoReverse
}

/// <summary>
/// 使用 Lottie 动画的控件。这样，可以通过 OmniLottie 来生成 Lottie 动画，并将其集成到 NewBeeVG 的渲染管线中。
/// </summary>
public class NBLottie : NBBaseImage
{
    public Stream? LottieStream { get; set; }

    public string? LottieCode { get; set; }

    public string? LottieFile
    {
        get;
        set
        {
            field = value;
            if (String.IsNullOrEmpty(value) == false && File.Exists(value))
            {
                // Load the file or perform some action
                LottieCode = File.ReadAllText(value);
                if(String.IsNullOrEmpty(LottieCode) == false)
                {
                    LottieStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(LottieCode));
                }
            }
        }
    }

    protected Animation? Animator { get; set; }

    protected bool IsLottieLoaded;

    private SKSize? LottieSize;

    public double Fps { get; protected set; }

    public double Duration { get; protected set; } = 0;

    public int TotalFrames { get; protected set; }

    public NBAnimateLoopMode LoopMode { get; set; } = NBAnimateLoopMode.Loop;

    /// <summary>
    /// 播放速度
    /// </summary>
    public double Speed { get; set; } = 1.0;

    protected override SKSize? GetImageSize()
    {
        TryLoadLottie();
        if (LottieSize != null)
        {
            return new SKSize(LottieSize.Value.Width, LottieSize.Value.Height);
        }
        return null;
    }

    protected void TryLoadLottie()
    {
        if (IsLottieLoaded == true) return;

        IsLottieLoaded = true;
        if (LottieStream != null)
        {
            try
            {
                var animation = Animation.Create(LottieStream);
                if (animation == null)
                {
                    DecodeException = new Exception("加载动画失败");
                    IsLottieLoaded = true;
                }
                else
                {
                    LottieSize = animation.Size;
                    Fps = animation.Fps;

                    Duration = animation.Duration.TotalSeconds;
                    Animator = animation;
                    TotalFrames = (int)(Fps * Duration + +0.0001);
                }
            }
            catch (Exception ex)
            {
                DecodeException = ex;
            }
        }
    }

    protected override void Draw(SKCanvas context, SKRect src, SKRect dest, SKPaint paint)
    {
        if (Animator == null || LottieSize == null || TotalFrames <= 0) return;

        var size = LottieSize.Value;
        int width = (int)size.Width;
        int height = (int)size.Height;
        if(width <= 0 || height <= 0) return;

        var frame = NBDrawContext.Current?.frame ?? 0;
        frame = (int)(frame * Speed);

        if(LoopMode == NBAnimateLoopMode.Loop)
        {
            frame = frame % TotalFrames;
        }
        else if (LoopMode == NBAnimateLoopMode.AutoReverse)
        {
            var cycleFrames = TotalFrames * 2;
            frame = frame % cycleFrames;
            if (frame >= TotalFrames)
            {
                frame = cycleFrames - frame - 1;
            }
        }

        Animator.SeekFrame(frame);

        using var srcBitmap = new SKBitmap(width, height);
        var canvas = new SKCanvas(srcBitmap);
        canvas.Clear(SKColors.Transparent);
        var rect = new SKRect(0, 0, width, height);
        Animator.Render(canvas, rect);
        context.DrawBitmap(srcBitmap, src, dest, paint);
    }
}

public static partial class NBExtentions
{
    public static T LoopMode<T>(this T self, NBAnimateLoopMode loopMode) where T : NBLottie
    {
        self.LoopMode = loopMode;
        return self;
    }

    public static T Speed<T>(this T self, double speed) where T : NBLottie
    {
        self.Speed = speed;
        return self;
    }
}