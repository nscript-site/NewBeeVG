using NewBeeMedia;
using NewBeeVG.Internal;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace NewBeeVG;

public static class Methods
{
    private static List<NBClip> clipLists = new List<NBClip>();
    private static Dictionary<string,NBClip> clipDict = new Dictionary<string, NBClip>();

    public static List<NBClip> clips(bool reset = false)
    {
        var list = clipLists;
        if(reset == true)
        {
            clipLists = new List<NBClip>();
            clipDict = new Dictionary<string, NBClip>();
        }
        return list;
    }

    public static NBClip? clip(string key)
    {
        if(clipDict.TryGetValue(key, out var clip))
            return clip;
        return null;
    }

    public static void add(NBClip clip, string? key)
    {
        clipLists.Add(clip);
        if(key != null)
            clipDict[key] = clip;
    }

    public static NBStage stage(int width = 1080, int height = 1920, SKColor? bg = null, double fps = 25.0)
    {
        return new NBStage
        {
            Width = width,
            Height = height,
            Background = bg,
            FrameRate = Math.Max(0.1, fps)
        };
    }

    public static NBWorkspace workspace()
    {
        return new NBWorkspace();
    }

    public static NBWork work()
    {
        return new NBWork();
    }

    public static NBDefer defer(Action action)
    {
        return new NBDefer(action);
    }

    public static Func<NBDrawContext, NBClip, NBVisual?>? build(Func<NBDrawContext, NBClip, NBVisual?>? builder)
    {
        return builder;
    }

    public static NBDrawingClip clip(
        Func<NBDrawContext, NBClip, NBVisual?>? builder,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "clip",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, builder, null, blend, frames, start);
    }

    public static NBDrawingClip clip(
        NBVisual? content,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "clip",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, content, null, blend, frames, start);
    }

    public static NBDrawingClip clip(
        Func<NBDrawContext, NBClip, NBVisual?>? builder,
        Func<NBDrawContext, NBClip, NBVisual?>? mask,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "clip",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, builder, mask, blend, frames, start);
    }

    public static NBDrawingClip clip(
        NBVisual? content,
        NBVisual? mask,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "clip",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, content, mask, blend, frames, start);
    }

    public static NBDrawingClip clip(
        Func<NBDrawContext, NBClip, NBVisual?>? builder,
        Action<NBDrawContext, NBClip, SKCanvas>? mask,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "clip",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, builder, mask, blend, frames, start);
    }

    public static NBDrawingClip drawing(
        Action<NBDrawContext, NBClip, SKCanvas>? builder,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "drawing",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, builder, null, blend, frames, start);
    }

    public static NBDrawingClip drawing(
        Action<NBDrawContext, NBClip, SKCanvas>? builder,
        Func<NBDrawContext, NBClip, NBLayoutable?>? mask,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "drawing",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, builder, mask, blend, frames, start);
    }

    public static NBDrawingClip drawing(
        Action<NBDrawContext, NBClip, SKCanvas>? builder,
        Action<NBDrawContext, NBClip, SKCanvas>? mask,
        SKBlendMode blend = SKBlendMode.SrcIn,
        string name = "drawing",
        int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, builder, mask, blend, frames, start);
    }

    public static NBTrack track()
    {
        return new NBTrack();
    }

    #region shapes

    public static NBRect Rect(double width = 100, double height = 100, SKColor? fill = null, double cornerRadius = 0)
    {
        return new NBRect(width, height) { Fill = fill, CornerRadius = cornerRadius };
    }

    public static NBEllipse Ellipse(double width = 100, double height = 100, SKColor? fill = null)
    {
        return new NBEllipse(width, height) { Fill = fill };
    }

    public static NBPath VecPath(Action<SKPath> onCreate, SKColor? fill = null, NBBorder? border = null)
    {
        return new NBPath(onCreate, fill, border);
    }

    public static NBPath VecPath(SKColor? fill = null, NBBorder? border = null)
    {
        return new NBPath((SKPath?)null, fill, border);
    }

    public static NBPath VecPath(SKPath path, SKColor? fill = null, NBBorder? border = null)
    {
        return new NBPath(path, fill, border);
    }

    public static NBPath VecPath(SKPath p0, SKPath p1, float t, SKColor? fill = null, NBBorder? border = null)
    {
        return new NBPath(p0, p1, t, fill, border);
    }

    public static SKPath RectPath(float x, float y, float width, float height)
    {
        var p = new SKPath();
        p.AddRect(new SKRect(x, y, x + width, x + height));
        return p;
    }

    public static SKPath CirclePath(float x, float y, float radius)
    {
        var p = new SKPath();
        p.AddCircle(x, y, radius);
        return p;
    }

    /// <summary>
    /// 创建一个插值矢量路径，支持动画
    /// </summary>
    /// <param name="onCreateStart"></param>
    /// <param name="onCreateEnd"></param>
    /// <param name="t"></param>
    /// <param name="fill"></param>
    /// <param name="border"></param>
    /// <returns></returns>
    public static NBPath VecPath(Action<SKPath> onCreateStart, Action<SKPath> onCreateEnd, float t, SKColor? fill = null, NBBorder? border = null)
    {
        return new NBPath(onCreateStart, onCreateEnd, t, fill, border);
    }

    #endregion

    internal static void start(string programSubName, string[]? args = null)
    {
        App.Start(programSubName, args);
    }

    public static void run()
    {
        run(stage(), []);
    }

    private static void PrepareClips(IList<NBClip> clips, NBStage stage)
    {
        foreach (var clip in clips)
        {
            try
            {
                clip.Prepare(stage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Clip[{clip.Name} prepare fail: {ex.Message}]");
            }
        }
    }

    /// <summary>
    /// 运行一段动画：创建工作区并准备片段，再根据 <paramref name="args"/> 决定是打开预览窗口，
    /// 还是导出 MP4 视频或单帧 PNG。
    /// </summary>
    /// <param name="stage">舞台参数：画幅宽高、帧率、背景色等。</param>
    /// <param name="clips">主片段列表；会先逐个调用 Prepare，再放入工作区的同一条轨道。</param>
    /// <param name="clipLists">
    /// 额外的片段分组。传入后会把每组片段也 Prepare 并合并到同一条轨道，适合把片段构建拆成多段代码；
    /// 为 null 或空数组时只使用 <paramref name="clips"/>。
    /// </param>
    /// <param name="args">
    /// 运行参数，长度不同用途不同：
    /// <list type="bullet">
    /// <item><description>
    /// <c>null</c> 或空数组：启动 Avalonia 预览窗口，阻塞到窗口关闭，返回 -1。
    /// </description></item>
    /// <item><description>
    /// 1 个元素（MP4 路径）：把所有片段导出为 MP4，例如 <c>args = ["output/demo.mp4"]</c>。
    /// </description></item>
    /// <item><description>
    /// 2 个及以上元素（帧号, PNG 路径）：只渲染指定帧并保存为 PNG，
    /// 例如 <c>args = ["42", "output/frame_042.png"]</c>；第 3 个及之后的元素会被忽略。
    /// </description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// 预览模式返回 -1；没有可导出的内容返回 0；MP4 / PNG 导出模式返回动画总帧数。
    /// </returns>
    /// <remarks>
    /// <para>典型用法：在 file-based app 顶层把命令行参数直接透传进来。</para>
    /// <code>
    /// run(stage(1920, 1080, bg: SKColors.Black), [clip1, clip2], null, args);
    ///
    /// // dotnet run                      → 打开预览窗口
    /// // dotnet run -- out.mp4           → 导出 MP4
    /// // dotnet run -- 42 out.png        → 导出第 42 帧为 PNG
    /// </code>
    /// <para>片段分多批构建时，用 <paramref name="clipLists"/> 合并到同一条轨道：</para>
    /// <code>
    /// run(stage(1920, 1080), mainClips, [overlayClips, captionClips], args);
    /// </code>
    /// <para>注意事项：</para>
    /// <list type="bullet">
    /// <item><description>预览模式会启动桌面 UI，必须在主线程调用，并会阻塞到窗口关闭。</description></item>
    /// <item><description>MP4 导出内部调用 <see cref="save"/>，目标文件已存在时会拒绝覆盖；且依赖 ffmpeg。</description></item>
    /// <item><description>帧号不是整数时直接返回 -1；帧号越界只打印提示，仍会尝试渲染并保存。</description></item>
    /// <item><description>PNG 导出使用 <see cref="DrawingHelper.Save"/>，目标目录不存在时会自动创建。</description></item>
    /// <item><description>片段 Prepare 失败只打印错误、不中断整个流程。</description></item>
    /// </list>
    /// </remarks>
    public static int run(NBStage stage, IList<NBClip> clips, IList<NBClip>[]? clipLists = null, string[]? args = null, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
    {
        // 先准备主片段：计算时长、初始化资源等；失败只打印错误，不中断流程
        PrepareClips(clips, stage);
        if(clipLists?.Length > 0)
        {
            // 有额外分组时，把它们全部合并进同一条轨道
            var all = new List<NBClip>(clips.Count * 2);
            all.AddRange(clips);
            foreach(var list in clipLists)
            {
                PrepareClips(list, stage);
                all.AddRange(list);
            }
            NBWorkspace.Current = NBWorkspace.Create(stage, all);
        }
        else
        {
            NBWorkspace.Current = NBWorkspace.Create(stage, clips);
        }
        NBWorkspace ws = NBWorkspace.Current;
        // 无参数：启动预览窗口（阻塞到窗口关闭），返回 -1
        if(args == null || args.Length == 0)
        {
            string subName = String.Empty;
            if (string.IsNullOrEmpty(callerFilePath) == false)
            {
                subName = new FileInfo(callerFilePath).Name;
            }
            start(subName);
            return -1;
        }

        var work = ws.Works.FirstOrDefault();
        if (work == null)
        {
            Console.WriteLine($"视频总帧数:{0}");
            return 0;
        }

        int frames = work.Measure();

        // 1 个参数：导出 MP4 到 args[0]；save 内部会拒绝覆盖已存在文件
        if (args.Length == 1)
        {
            var outMp4Path = args[0].Trim();
            save(outMp4Path, work.Stage, work);
        }
        // 2 个及以上参数：args[0] = 帧号，args[1] = PNG 输出路径
        else if (args.Length > 1)
        {
            var framesStr = args[0].Trim();
            int frame = -1;
            bool parseOk = int.TryParse(framesStr, out frame);
            if (parseOk == false)
            {
                Console.WriteLine($"帧 index 格式错误: {framesStr}");
                return -1;
            }
            // 帧号越界只提示，仍继续渲染；调用方可自行检查返回的总帧数
            if (frame < 0 || frame >= frames)
            {
                Console.WriteLine($"帧 index 格式不存在，存在的帧索引为: [0, {frames - 1}]");
            }
            var outPngPath = args[1].Trim();
            var bitmap = work.CreateBitmap();
            IPlayable? track = work.Tracks.FirstOrDefault();
            if (track != null)
            {
                track.Render(bitmap, work.Stage, frame, true);
            }

            DrawingHelper.Save(bitmap, outPngPath);
            Console.WriteLine($"文件已保存: {outPngPath}");
        }

        Console.WriteLine($"视频总帧数:{frames}");

        return frames;
    }

    public static void save(string path, NBStage stage, IList<NBClip> clips)
    {
        var fileInfo = new FileInfo(path);
        if(fileInfo.Exists == true)
        {
            Console.WriteLine($"文件已存在:{path}, 保存失败!");
            return;
        }

        foreach (var clip in clips)
        {
            try
            {
                clip.Prepare(stage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Clip[{clip.Name} prepare fail: {ex.Message}]");
            }
        }

        var work = NBWorkspace.Create(stage, clips);
        if(work.Works.Count == 0)
        {
            Console.WriteLine($"没有可导出的视频内容!");
            return;
        }

        save(path, stage, work.Works[0]);
    }

    public static void save(string path, NBStage stage, NBWork work)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists == true)
        {
            Console.WriteLine($"文件已存在:{path}, 保存失败!");
            return;
        }

        NBGlobal.CheckOrLoadFFmpeg();

        var frames = work.Measure();

        var fileName = fileInfo.FullName;

        IPlayable Playable = work;
        var pcm = Playable.MixedAudioData;

        try
        {
            var writer = new MediaWriter(fileName, stage.Width, stage.Height, stage.FrameRate, true, stage.AudioFrameRate);
            if (pcm != null)
            {
                pcm.WriteTo(writer, stage.AudioFrameRate);
            }
            for (int CurrentFrame = 0; CurrentFrame < frames; CurrentFrame++)
            {
                using var bmp = Playable.RenderBitmap(stage, CurrentFrame, true, false);
                if (bmp == null) break;

                writer.WriteFrame(bmp);

                if (CurrentFrame % 10 == 0)
                {
                    var msg = $"正在导出视频... {CurrentFrame}/{frames}";
                    Console.WriteLine(msg);
                }
            }

            writer.Close();
        }
        finally
        {
        }
    }

    private static Dictionary<string, string> _localConfigData = new Dictionary<string, string>();
    private static bool _localConfigLoaded = false;

    internal static string GetApiKey(string apiKey)
    {
        if (apiKey.StartsWith("@"))
        {
            string envVarName = apiKey.Substring(1);
            string? envVarValue = Environment.GetEnvironmentVariable(envVarName);

            // 优先使用环境变量中的值，如果环境变量不存在或为空，则尝试从本地配置文件中获取
            if (!string.IsNullOrEmpty(envVarValue))
            {
                return envVarValue;
            }
            else
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (home != null)
                {
                    CheckLoadLocalConfig();
                    if (_localConfigData.TryGetValue(envVarName, out var localConfigValue))
                    {
                        if (string.IsNullOrEmpty(localConfigValue) == false)
                        {
                            return localConfigValue;
                        }
                    }
                }
            }
        }

        if (apiKey.StartsWith("\"") == true) apiKey = apiKey.Substring(1);
        else if (apiKey.EndsWith("\"")) apiKey = apiKey.Substring(0, apiKey.Length - 1);

        return apiKey;
    }

    internal static void CheckLoadLocalConfig()
    {
        if (_localConfigLoaded)
            return;

        lock (_localConfigData)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (home != null)
            {
                var configFile = Path.Combine(home, ".nbag", "config.ini");
                Console.WriteLine($"Checking config file: {configFile}");
                if (File.Exists(configFile))
                {
                    var lines = File.ReadAllLines(configFile);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                            continue;
                        var parts = trimmedLine.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            if (String.IsNullOrEmpty(key) == false)
                                _localConfigData[key] = value;
                        }
                    }
                }
            }
            _localConfigLoaded = true;
        }
    }

    public static string GetStringWithMask(string str, int visibleChars = 8)
    {
        visibleChars = visibleChars / 2;
        if (string.IsNullOrEmpty(str))
            return str;
        if (str.Length <= visibleChars * 2)
            return new string('*', str.Length);
        var prefix = str.Substring(0, visibleChars);
        str = str.Substring(visibleChars);
        int maskedChars = str.Length - visibleChars;
        return prefix + new string('*', maskedChars) + str.Substring(maskedChars);
    }

    #region Widgets

    private static string? _globalFontFaminy = null;

    public static string GetDefaultFontFamily()
    {
        if (String.IsNullOrEmpty(_globalFontFaminy) == false) return _globalFontFaminy;

        // 在不同平台上设置默认字体
        if (OperatingSystem.IsWindows())
        {
            return "Microsoft YaHei";
        }
        else if (OperatingSystem.IsMacOS())
        {
            return "Helvetica";
        }
        else if (OperatingSystem.IsLinux())
        {
            return "DejaVu Sans";
        }
        else
        {
            return "Arial"; // 默认回退字体
        }
    }

    public static void font(string name)
    {
        _globalFontFaminy = name;
    }

    /// <summary>
    /// 使用绘制回调创建 SK 动画，并直接包装为 NewBeeVG 可视节点。
    /// </summary>
    /// <param name="size">单帧画面尺寸（像素），内部会截断为整数宽高。</param>
    /// <param name="frames">动画总帧数，有效帧号为 0 ~ frames-1。</param>
    /// <param name="onFrame">逐帧绘制回调：参数依次为画布、画面尺寸、当前帧号。</param>
    /// <returns>可直接参与布局与渲染的 <see cref="NBSkiaAnimation"/> 节点。</returns>
    /// <remarks>
    /// <see cref="SkiaAnim(ISKAnimation)"/> 包装为节点。
    /// </remarks>
    public static NBSkiaAnimation SkiaAnim(SKSize size, int frames, Action<SKCanvas, SKSize, int> onFrame)
    {
        var skanim = new SKAnimation((int)size.Width, (int)size.Height, frames, onFrame);
        // 先创建原始动画，再包装为可视节点
        return new NBSkiaAnimation { Animation = skanim };
    }

    /// <summary>
    /// 将已有的 <see cref="ISKAnimation"/> 包装为 NewBeeVG 可视节点，便于放入 Grid、Stack、Layer 等布局容器中渲染。
    /// </summary>
    /// <param name="animation">动画实例；节点的尺寸与总帧数均取自该实例。</param>
    /// <returns>可直接参与布局与渲染的 <see cref="NBSkiaAnimation"/> 节点。</returns>
    /// <remarks>
    /// 节点不会复制动画数据，而是与传入实例共享同一份 <see cref="ISKAnimation"/>。
    /// 默认循环播放（<see cref="NBSkiaAnimation.LoopMode"/> 为 <see cref="NBAnimateLoopMode.Loop"/>），
    /// 帧号取自 <see cref="NBDrawContext.Current"/>，并按节点布局区域把画面映射到目标矩形。
    /// </remarks>
    public static NBSkiaAnimation SkiaAnim(ISKAnimation animation)
    {
        // 只做包装，不复制动画，节点与调用方共享同一个动画实例
        return new NBSkiaAnimation { Animation = animation };
    }

    /// <summary>
    /// 将 APNG（动态 PNG）动画包装为 NewBeeVG 的可视节点。
    /// </summary>
    /// <param name="filePath">APNG 文件路径；相对路径按当前工作目录解析。</param>
    /// <returns>可直接放入 Grid、Stack、Layer 等布局容器中渲染的 <see cref="NBPngAnimation"/> 节点。</returns>
    /// <remarks>
    /// <para>
    /// 构造时立即把文件读入内存，实际解码延迟到首次布局或绘制；节点以 APNG 第一帧的像素尺寸参与布局，
    /// 并按 <see cref="NBBaseLoopAnimation.TotalFrames"/> 逐帧播放。
    /// </para>
    /// <para>
    /// 默认 <see cref="NBBaseLoopAnimation.LoopMode"/> 为 <see cref="NBAnimateLoopMode.Stop"/>（播放到最后一帧后停住），
    /// 可用 <c>LoopMode(NBAnimateLoopMode.Loop)</c> / <c>Speed(...)</c> 链式调整播放方式与速度。
    /// </para>
    /// <para>
    /// 文件不存在或 APNG 解码失败时不会抛出异常，错误记录在 <see cref="NBBaseImage.DecodeException"/>，此时节点不绘制内容。
    /// </para>
    /// </remarks>
    public static NBPngAnimation PngAnim(string filePath)
    {
        return new NBPngAnimation(filePath);
    }

    /// <summary>
    /// 将 Lottie（JSON）动画文件包装为 NewBeeVG 的可视节点。
    /// </summary>
    /// <param name="path">Lottie JSON 文件路径；相对路径按当前工作目录解析。压缩包内的资源请改用 <c>LottieFile(basePath, fileName)</c>。</param>
    /// <returns>可直接放入 Grid、Stack、Layer 等布局容器中渲染的 <see cref="NBLottieAnimation"/> 节点。</returns>
    /// <remarks>
    /// <para>
    /// 赋值时立即读取 JSON 文本，真正的 Lottie 解析延迟到首次布局或绘制；解析完成后由动画的 Fps 与时长换算出
    /// <see cref="NBBaseLoopAnimation.TotalFrames"/>，节点按 Lottie 画布尺寸参与布局。
    /// </para>
    /// <para>
    /// 默认 <see cref="NBBaseLoopAnimation.LoopMode"/> 为 <see cref="NBAnimateLoopMode.Stop"/>（播放到最后一帧后停住），
    /// 可用 <c>LoopMode(NBAnimateLoopMode.Loop)</c> / <c>Speed(...)</c> 链式调整播放方式与速度。
    /// </para>
    /// <para>
    /// 文件不存在时仅输出控制台提示、节点不绘制内容；解析失败记录在 <see cref="NBBaseImage.DecodeException"/>。
    /// </para>
    /// </remarks>
    public static NBLottieAnimation LottieAnim(string path)
    {
        var file = new NBLottieAnimation();
        file.LottieFile = path;
        return file;
    }

    /// <summary>
    /// 将 Typst 文档包装为按页播放的 NewBeeVG 可视节点。
    /// </summary>
    /// <param name="path">Typst（.typ）文件路径；相对路径按当前工作目录解析。</param>
    /// <returns>可直接放入 Grid、Stack、Layer 等布局容器中渲染的 <see cref="NBTypstAnimation"/> 节点。</returns>
    /// <remarks>
    /// <para>
    /// 赋值时立即读取并编译文档，每一页渲染为 SVG 后转成一张 <see cref="SKPicture"/>，按页顺序组成动画帧；
    /// <see cref="NBBaseLoopAnimation.TotalFrames"/> 等于成功渲染的页数，节点尺寸取第一页。
    /// </para>
    /// <para>
    /// 编译时以 .typ 文件所在目录为工作区，因此 <c>#include</c> / <c>#import</c> 的相对路径与同目录资源可正常解析；
    /// 同时会搜索 <c>./fonts</c>、<c>./assets/typography</c> 与系统字体。
    /// </para>
    /// <para>
    /// 默认 <see cref="NBBaseLoopAnimation.LoopMode"/> 为 <see cref="NBAnimateLoopMode.Stop"/>（播放到最后一页后停住）；
    /// 文件不存在或编译失败记录在 <see cref="NBBaseImage.DecodeException"/>，此时节点不绘制内容。
    /// </para>
    /// </remarks>
    public static NBTypstAnimation TypstAnim(string path)
    {
        var file = new NBTypstAnimation();
        file.TypstFile = path;
        return file;
    }

    public static NBText Text(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null, bool wrap = true, int textAlign = -1)
    {
        return TextBlock(text, fontSize, color, fontFamily, wrap, textAlign);
    }

    public static NBText TextBlock(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null, bool wrap = true, int textAlign = -1)
    {
        var tb = new NBText { Text = text, FontFamily = fontFamily ?? GetDefaultFontFamily(), FontSize = fontSize, Foreground = color ?? SKColors.Black };
        tb.IsWrapText = wrap;
        tb.TextAlign = textAlign.ToNBTextAlign();
        return tb;
    }

    public static NBTextRun TextRun(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null)
    {
        var tb = new NBTextRun { Text = text, FontFamily = fontFamily ?? GetDefaultFontFamily(), FontSize = fontSize, Foreground = color ?? SKColors.Black };
        return tb;
    }

    public static NBRichText RichText(NBTextRun?[]? runs = null, bool wrap = true, int textAlign = -1)
    {
        var tb = new NBRichText();
        tb.IsWrapText = wrap;
        tb.TextAlign = textAlign.ToNBTextAlign();
        if(runs != null)
        {
            foreach(var run in runs)
            {
                if (run != null)
                {
                    tb.Add(run);
                }
            }
        }
        return tb;
    }

    public static NBPanel Panel(NBVisual[]? childs = null)
    {
        var panel = new NBPanel();
        if (childs != null)
        {
            panel.Childs(childs);
        }
        return panel;
    }

    public static NBWrapPanel WrapPanel(NBVisual[]? childs = null, bool isHorizontal = true)
    {
        var panel = new NBWrapPanel { Orientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical };
        if (childs != null)
        {
            panel.Childs(childs);
        }
        return panel;
    }

    public static NBLayer Layer(NBVisual?[]? childs = null)
    {
        var layer = new NBLayer();
        layer.Align(0, 0);
        if(childs != null)
        {
            if(childs.Length > 0)
            {
                layer.Source = childs[0];
            }

            if(childs.Length > 1)
            {
                layer.Mask = childs[1];
            }
        }
        return layer;
    }

    //public static NBLayer Layer(double? width = null, double? height = null, int? hAlign = 0, int? vAlign = 0)
    //{
    //    var layer = new NBLayer { Width = width ?? double.NaN, Height = height ?? double.NaN };
    //    layer.Align(hAlign, vAlign);
    //    return layer;
    //}

    public static NBGrid Grid(NBVisual[]? childs = null, string? rowDef = null, string? colDef = null)
    {
        var panel = new NBGrid();
        if (rowDef != null)
        {
            panel.RowDefinitions = NBRowDefinitions.Parse(rowDef);
        }
        if (colDef != null)
        {
            panel.ColumnDefinitions = NBColumnDefinitions.Parse(colDef);
        }
        if (childs != null)
        {
            panel.Childs(childs);
        }
        return panel;
    }

    public static NBGrid HGrid(string colDef, NBVisual?[]? childs)
    {
        var panel = new NBGrid() { ColumnDefinitions = NBColumnDefinitions.Parse(colDef) };
        if (childs != null)
        {
            for (int i = 0; i < childs.Length; i++)
            {
                childs[i]?.Col(i);
            }
            panel.Childs(childs);
        }
        return panel;
    }

    public static NBGrid VGrid(string rowDef, NBVisual?[]? childs)
    {
        var panel = new NBGrid() { RowDefinitions = NBRowDefinitions.Parse(rowDef) };
        if (childs != null)
        {
            for (int i = 0; i < childs.Length; i++)
            {
                childs[i]?.Row(i);
            }
            panel.Childs(childs);
        }
        return panel;
    }

    public static NBStack HStack(NBVisual[]? childs)
    {
        var stack = new NBStack { Orientation = Orientation.Horizontal};
        if (childs != null)
        {
            stack.Childs(childs);
        }
        return stack;
    }

    public static NBStack VStack(NBVisual[]? childs)
    {
        var stack = new NBStack { Orientation = Orientation.Vertical };
        if (childs != null)
        {
            stack.Childs(childs);
        }
        return stack;
    }

    private static Dictionary<string,SKBitmap> _imageCache = new Dictionary<string, SKBitmap>();
    private static object _imageCacheLock = new object();
    private static SKBitmap LoadImage(string source)
    {
        lock (_imageCacheLock)
        {
            if (_imageCache.TryGetValue(source, out var bitmap))
            {
                return bitmap;
            }
            else
            {
                bitmap = SKBitmap.Decode(source);
                _imageCache[source] = bitmap;
                return bitmap;
            }
        }
    }

    public static NBImage Image(string source, float? width = null, float? height = null)
    {
        return Image(SKBitmap.Decode(source), width, height);
    }

    public static NBImage Image(byte[] data, float? width = null, float? height = null)
    {
        using var stream = new MemoryStream(data);
        return Image(SKBitmap.Decode(stream), width, height);
    }

    public static NBImage Image(SKBitmap source, float? width = null, float? height = null)
    {
        var img = new NBImage { Source = source };
        if (width.HasValue) img.Width = width.Value;
        if (height.HasValue) img.Height = height.Value;
        return img;
    }

    public static NBImage Image(string basePath, string fileName, float? width = null, float? height = null)
    {
        var img = new NBImage();
        img.TryLoad(
            () => img.Source = SKBitmap.Decode(FileHelpers.LoadStream(basePath, fileName))
        );
        if (width.HasValue) img.Width = width.Value;
        if (height.HasValue) img.Height = height.Value;
        return img;
    }

    public static NBSvg SVG(Stream stream, float? width = null, float? height = null)
    {
        var svg = new NBSvg { SvgStream = stream };
        if (width.HasValue) svg.Width = width.Value;
        if (height.HasValue) svg.Height = height.Value;
        return svg;
    }

    public static NBSvg SVG(string content, float? width = null, float? height = null)
    {
        var svg = new NBSvg();
        svg.SvgContent(content);
        if (width.HasValue) svg.Width = width.Value;
        if (height.HasValue) svg.Height = height.Value;
        return svg;
    }

    public static NBSvg SVG(string basePath, string fileName, float? width = null, float? height = null)
    {
        var svg = new NBSvg();
        
        svg.TryLoad(()=> svg.SvgStream = FileHelpers.LoadStream(basePath, fileName));

        if (width.HasValue) svg.Width = width.Value;
        if (height.HasValue) svg.Height = height.Value;

        return svg;
    }

    public static NBTypst TypstFile(string path, float? width = null, float? height = null)
    {
        var file = new NBTypst();
        file.TypstFile = path;
        if (width.HasValue) file.Width = width.Value;
        if (height.HasValue) file.Height = height.Value;
        return file;
    }

    public static NBTypstContent TypstContent(string content, int? pageMargin = 10)
    {
        var file = new NBTypstContent();
        file.TypstContent = content;
        if (pageMargin != null)
            file.PageMargin(pageMargin.Value);
        return file;
    }

    public static NBTypstMath TypstMath(string content, float? fontSize = null, int? pageMargin = 10)
    {
        var file = new NBTypstMath();
        file.TypstContent = content;
        file.FontSize = fontSize;
        if(pageMargin != null)
            file.PageMargin(pageMargin.Value);
        return file;
    }

    public static NBTypstCode TypstCode(string content, int pageWidth = 600, string? lang = null, bool showLang = true, float? fontSize = null, int? pageMargin = 10, SKColor? boxBg = null)
    {
        var file = new NBTypstCode();
        file.TypstContent = content;
        file.Lang = lang;
        file.ShowLang = showLang;
        file.FontSize = fontSize;
        file.PageWidth = pageWidth;
        file.CodeBoxBackgroundColor = boxBg;
        if (pageMargin != null)
            file.PageMargin(pageMargin.Value);
        return file;
    }

    public static NBTypstCode TypstCodeFile([CallerFilePath] string path = "", int pageWidth = 600, float? fontSize = null, int? pageMargin = 10)
    {
        string content = String.Empty;
        
        try
        {
            content = File.ReadAllText(path);
        }
        catch(Exception ex)
        {
            content = ex.Message;
        }

        return TypstCode(content, pageWidth, "cs", true, fontSize, pageMargin);
    }

    public static NBLottieAnimation LottieFile(string path, float? width = null, float? height = null, double speed = 1.0, NBAnimateLoopMode loop = NBAnimateLoopMode.Loop)
    {
        var file = new NBLottieAnimation();
        file.LottieFile = path;
        if (width.HasValue) file.Width = width.Value;
        if (height.HasValue) file.Height = height.Value;
        file.Speed = speed;
        file.LoopMode = loop;
        return file;
    }

    public static NBLottieAnimation LottieFile(string basePath, string fileName, float? width = null, float? height = null, double speed = 1.0, NBAnimateLoopMode loop = NBAnimateLoopMode.Loop)
    {
        var file = new NBLottieAnimation();
        file.LoadFromFile(basePath, fileName);
        if (width.HasValue) file.Width = width.Value;
        if (height.HasValue) file.Height = height.Value;
        file.Speed = speed;
        file.LoopMode = loop;
        return file;
    }

    public static NBAudioClip AudioFile(string path)
    {
        var file = new NBAudioClip(path);
        return file;
    }

    #endregion
}
