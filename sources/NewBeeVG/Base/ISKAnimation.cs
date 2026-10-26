using NewBeeMedia;
using NewBeeMedia.Encoders.Gif;
using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// SK 动画接口：描述一段可以逐帧渲染的位图动画。
/// 实现方需提供总帧数、单帧尺寸，以及按帧号直接绘制、生成位图、导出图片/视频的能力。
/// </summary>
public interface ISKAnimation
{
    /// <summary>动画总帧数（有效帧号范围为 0 ~ Frames-1）。</summary>
    public int Frames { get;}

    /// <summary>单帧画面宽度（像素）。</summary>
    public int Width { get; }

    /// <summary>单帧画面高度（像素）。</summary>
    public int Height { get;}

    /// <summary>
    /// 构建指定帧的位图。
    /// </summary>
    /// <param name="frame">帧号，从 0 开始。</param>
    /// <returns>渲染完成的位图；帧号越界或未设置渲染回调时返回 null。调用方负责释放该位图。</returns>
    public SKBitmap? BuildFrame(int frame);

    /// <summary>
    /// 将指定帧直接绘制到给定画布（不创建中间位图），用于嵌入已有渲染流程。
    /// </summary>
    /// <param name="canvas">目标画布；其当前变换矩阵与裁切状态由调用方负责。</param>
    /// <param name="frame">帧号，有效范围为 0 ~ Frames-1。</param>
    /// <returns>绘制成功返回 true；帧号越界、尺寸非法或未设置渲染回调时返回 false。</returns>
    public bool Draw(SKCanvas canvas, int frame);

    /// <summary>
    /// 将指定帧渲染并保存为 PNG 图片（无损格式，保留 Alpha 通道）。
    /// </summary>
    /// <param name="frame">帧号，从 0 开始。</param>
    /// <param name="filePath">输出文件路径；所在目录不存在时会自动创建。</param>
    /// <returns>保存成功返回 true；帧号越界或渲染结果为空时返回 false。</returns>
    public bool SavePNG(int frame, string filePath);

    /// <summary>
    /// 将全部帧渲染并编码为 MP4（H.264）视频。
    /// 输出目录不会自动创建，<paramref name="filePath"/> 所在目录必须已存在。
    /// </summary>
    /// <param name="fps">视频帧率（帧/秒），必须大于 0；视频时长 = Frames / fps。</param>
    /// <param name="filePath">输出 MP4 文件路径。</param>
    public void SaveMp4(double fps, string filePath);

    /// <summary>
    /// 将全部帧渲染并编码为 GIF 动图。
    /// 输出目录不会自动创建，<paramref name="filePath"/> 所在目录必须已存在。
    /// </summary>
    /// <param name="fps">目标帧率（帧/秒），必须大于 0，用于换算 GIF 帧延迟。</param>
    /// <param name="filePath">输出 GIF 文件路径。</param>
    /// <remarks>
    /// GIF 帧延迟以 1/100 秒为单位，精度有限：建议使用能整除 100 的帧率（如 10、20、25、50），
    /// 30fps 等会被取整，实际播放速度与目标值存在偏差。
    /// 输出尺寸受 MaxWidth/MaxHeight 限制（当前为 1200），超出时按比例缩小；
    /// GIF 仅支持二值透明（Alpha 阈值 128）。
    /// </remarks>
    public void SaveGif(double fps, string filePath);
}

/// <summary>
/// <see cref="ISKAnimation"/> 的默认实现：由外部传入的渲染回调逐帧绘制。
/// </summary>
public class SKAnimation : ISKAnimation
{
    /// <summary>动画总帧数。</summary>
    public int Frames { get; init; }

    /// <summary>单帧画面宽度（像素）。</summary>
    public int Width { get; init; }

    /// <summary>单帧画面高度（像素）。</summary>
    public int Height { get; init; }

    /// <summary>
    /// 创建动画实例。
    /// </summary>
    /// <param name="width">单帧画面宽度（像素），应大于 0。</param>
    /// <param name="height">单帧画面高度（像素），应大于 0。</param>
    /// <param name="frames">动画总帧数。</param>
    /// <param name="onFrame">逐帧渲染回调：参数依次为画布、画面尺寸、当前帧号；可为 null。</param>
    public SKAnimation(int width, int height, int frames, Action<SKCanvas, SKSize, int>? onFrame = null)
    {
        this.Width = width;
        this.Height = height;
        this.Frames = frames;
        this.OnFrame = onFrame;
    }

    /// <summary>
    /// 逐帧渲染回调。参数依次为：绘制用画布、当前画面尺寸、当前帧号。
    /// 为 null 时无法生成任何帧。
    /// </summary>
    public Action<SKCanvas,SKSize,int>? OnFrame { get; init; }

    /// <summary>
    /// 将指定帧直接绘制到给定画布（不创建中间位图）。
    /// </summary>
    /// <param name="canvas">目标画布；其当前变换矩阵与裁切状态由调用方负责。</param>
    /// <param name="frame">帧号，有效范围为 0 ~ Frames-1。</param>
    /// <returns>绘制成功返回 true；帧号越界、尺寸非法或未设置回调时返回 false。</returns>
    public bool Draw(SKCanvas canvas,int frame)
    {
        // 帧号越界、尺寸非法或缺少渲染回调时不绘制，直接返回 false
        if (frame < 0 || frame >= Frames || OnFrame == null) return false;
        if (Width <= 0 || Height <= 0) return false;

        // 把画布与帧号一并交给回调，由回调完成该帧的实际绘制
        OnFrame(canvas, new SKSize(Width, Height), frame);
        return true;
    }

    /// <summary>
    /// 构建指定帧的位图：创建与动画尺寸一致的 SKBitmap，再调用 <see cref="OnFrame"/> 完成绘制。
    /// </summary>
    /// <param name="frame">帧号，有效范围为 0 ~ Frames-1。</param>
    /// <returns>渲染完成的位图；帧号越界、尺寸非法或未设置回调时返回 null。调用方负责释放。</returns>
    public SKBitmap? BuildFrame(int frame)
    {
        // 帧号越界、尺寸非法或缺少渲染回调时，直接返回 null
        if (frame < 0 || frame >= Frames || OnFrame == null) return null;
        if (Width <= 0 || Height <= 0) return null;

        var info = new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bmp = new SKBitmap(info);

        // Canvas 仅用于绘制，绘制结束后立即释放；位图本身返回给调用方
        using var canvas = new SKCanvas(bmp);
        Draw(canvas, frame);
        return bmp;
    }

    /// <summary>
    /// 将指定帧渲染后以 PNG 格式保存到文件（PNG 为无损格式，可保留 Alpha 通道）。
    /// </summary>
    /// <param name="frame">帧号，从 0 开始。</param>
    /// <param name="filePath">输出文件路径；所在目录不存在时会自动创建。</param>
    /// <returns>保存成功返回 true；帧号越界或渲染结果为空时返回 false。</returns>
    /// <exception cref="ArgumentException">filePath 为空或仅包含空白字符。</exception>
    /// <exception cref="InvalidOperationException">PNG 编码失败。</exception>
    public bool SavePNG(int frame, string filePath)
    {
        // 先渲染目标帧；using 确保位图在方法结束时释放
        using var bmp = BuildFrame(frame);
        if (bmp == null || bmp.IsEmpty) return false;

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath 不能为空", nameof(filePath));

        // 确保目标目录存在（目录为空说明是相对路径的当前目录）
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // SKImage 直接引用位图像素，因此必须在 bmp 释放前完成编码
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        if (data == null || data.Size == 0)
            throw new InvalidOperationException($"PNG 编码失败：{filePath}");

        // 写入文件；FileMode.Create 会覆盖同名文件
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        data.SaveTo(fs);
        return true;
    }

    /// <summary>
    /// 将全部帧渲染并编码为 MP4（H.264）视频。
    /// 输出目录不会自动创建，<paramref name="filePath"/> 所在目录必须已存在。
    /// </summary>
    /// <param name="fps">视频帧率（帧/秒），必须大于 0；视频时长 = Frames / fps。</param>
    /// <param name="filePath">输出 MP4 文件路径。</param>
    /// <remarks>
    /// 调用前会通过 <see cref="NBGlobal.CheckOrLoadFFmpeg"/> 确保 ffmpeg 可用。
    /// 某一帧渲染失败（返回 null）时停止写入；每 10 帧输出一次进度。
    /// </remarks>
    public void SaveMp4(double fps, string filePath)
    {
        // 确保 ffmpeg 运行库已加载，否则 MediaWriter 无法创建编码器
        NBGlobal.CheckOrLoadFFmpeg();

        // containAudio = true：同时创建音轨；本方法不写入音频，因此音轨为空
        var writer = new MediaWriter(filePath, Width, Height, fps, true);

        for (int CurrentFrame = 0; CurrentFrame < Frames; CurrentFrame++)
        {
            // 逐帧渲染，using 确保位图在写入后立即释放
            using var bmp = BuildFrame(CurrentFrame);
            if (bmp == null) break; // 帧号非法或尺寸异常，提前结束

            writer.WriteFrame(bmp);

            // 每 10 帧打印一次进度，避免刷屏
            if (CurrentFrame % 10 == 0)
            {
                var msg = $"正在导出视频... {CurrentFrame}/{Frames}";
                Console.WriteLine(msg);
            }
        }

        // Close 会 flush 剩余帧并写入文件尾，必须在所有帧写入后调用
        writer.Close();
        Console.WriteLine($"视频导出完毕: {filePath}");
    }

    /// <summary>
    /// 将全部帧渲染并编码为 GIF 动图。
    /// 输出目录不会自动创建，<paramref name="filePath"/> 所在目录必须已存在。
    /// </summary>
    /// <param name="fps">目标帧率（帧/秒），必须大于 0，用于换算 GIF 帧延迟。</param>
    /// <param name="filePath">输出 GIF 文件路径。</param>
    /// <remarks>
    /// GIF 帧延迟以 1/100 秒为单位，精度有限：建议使用能整除 100 的帧率（如 10、20、25、50），
    /// 30fps 等会被取整，实际播放速度与目标值存在偏差。
    /// 输出尺寸受 MaxWidth/MaxHeight 限制（当前为 1200），超出时按比例缩小；
    /// GIF 仅支持二值透明（Alpha 阈值 128）。
    /// </remarks>
    public void SaveGif(double fps, string filePath)
    {
        // 帧延迟：单位是 1/100 秒，例如 25fps → 4
        int delayHundredths = (int)Math.Round(100.0 / fps);

        // 限制输出尺寸，避免生成过大的 GIF 文件
        var opt = new GifEncoderOptions();
        opt.MaxWidth = 1200;
        opt.MaxHeight = 1200;

        // using 保证 Dispose 时写入文件尾，完成 GIF 封装
        using var gifEncoder2 = GifEncoder.Create(filePath, delayHundredths, opt);

        for (int CurrentFrame = 0; CurrentFrame < Frames; CurrentFrame++)
        {
            // 逐帧渲染，using 确保位图在加入编码后立即释放
            using var bmp = BuildFrame(CurrentFrame);
            if (bmp == null) break; // 帧号非法或尺寸异常，提前结束

            gifEncoder2.AddFrame(bmp);

            if (CurrentFrame % 10 == 0)
            {
                var msg = $"正在导出 gif... {CurrentFrame}/{Frames}";
                Console.WriteLine(msg);
            }
        }

        Console.WriteLine($"gif 导出完毕: {filePath}");
    }
}

/// <summary>
/// <see cref="SKAnimation"/> 的子类实现：通过重写 <see cref="DrawFrame"/> 提供每帧绘制逻辑，
/// 适合绘制代码较多、需要拆分为方法的场景。
/// 类中同时提供主题色 <see cref="Theme"/>，便于子类统一取用背景色、文字色与强调色。
/// </summary>
public class SKCustomAnimation : SKAnimation
{
    /// <summary>
    /// 动画使用的主题色，子类可在 <see cref="DrawFrame"/> 中取用背景、文字、分隔线等颜色。
    /// 由构造函数注入，构造完成后不可更改。
    /// </summary>
    public NBVideoTheme Theme { get; private set; }

    /// <summary>
    /// 创建自定义动画实例。
    /// </summary>
    /// <param name="width">单帧画面宽度（像素），应大于 0。</param>
    /// <param name="height">单帧画面高度（像素），应大于 0。</param>
    /// <param name="frames">动画总帧数，有效帧号为 0 ~ frames-1。</param>
    /// <param name="theme">
    /// 动画主题色；为 <c>null</c> 时使用默认的 <see cref="NBVideoThemes.Nature"/>（自然主题）。
    /// </param>
    public SKCustomAnimation(int width, int height, int frames, NBVideoTheme? theme = null)
        : base(width, height, frames, null)
    {
        // 基类构造传入 null，这里把逐帧回调指向子类的 DrawFrame
        this.OnFrame = this.DrawFrame;

        // 未指定主题时使用自然主题作为默认配色
        this.Theme = theme ?? NBVideoThemes.Nature;
    }

    /// <summary>
    /// 每帧绘制逻辑，子类重写此方法即可；默认实现为空（输出全透明画面）。
    /// </summary>
    /// <param name="canvas">
    /// 目标画布，绘制前需自行清空或铺背景；通常使用 <see cref="Theme"/> 的
    /// <see cref="NBVideoTheme.Background"/> 作为底色。
    /// </param>
    /// <param name="size">当前画面尺寸（像素），通常等于 <see cref="SKAnimation.Width"/> / <see cref="SKAnimation.Height"/>。</param>
    /// <param name="frame">当前帧号，有效范围为 0 ~ Frames-1。</param>
    protected virtual void DrawFrame(SKCanvas canvas, SKSize size, int frame)
    {
    }
}