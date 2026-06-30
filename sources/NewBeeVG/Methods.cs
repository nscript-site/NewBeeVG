using NewBeeMedia;
using NewBeeVG.Internal;
using Python.Runtime;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace NewBeeVG;

public static class Methods
{
    public static NBStage stage(int width = 1080, int height = 1920, SKColor? bg = null)
    {
        return new NBStage
        {
            Width = width,
            Height = height,
            Background = bg,
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

    public static NBLayoutableClip clip(string name = "clip", Func<NBDrawContext, NBClip, NBLayoutable?>? builder = null, int frames = 1, int? start = null)
    {
        return new NBLayoutableClip(name, builder, frames, start);
    }

    public static NBTTSClip ttsClip(string text, string voice = "Cherry", string lang = "Chinese", string instructions = "", string model = "mlx-tts", string name = "clip", int? start = null)
    {
        return new NBTTSClip(text, voice, lang, instructions, model, name, start);
    }

    public static void scripts(string path, string? output = null)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists == false)
        {
            Console.WriteLine($"脚本文件不存在:{path}");
            return;
        }
        var ext = fileInfo.Extension.ToLower();
        if (ext == ".md")
        {
            var codes = TTSScriptParser.BuildCode(path);
            if (output != null)
            {
                if(File.Exists(output))
                {
                    Console.WriteLine($"输出文件已存在:{output}, 请更换输出路径!");
                    return;
                }

                File.WriteAllText(output, codes);
                Console.WriteLine($"脚本已生成到:{output}");
            }
            else
            {
                Console.WriteLine(codes);
            }
        }
        else
        {
            Console.WriteLine($"不支持的脚本文件类型:{ext}");
        }
    }

    public static NBDrawingClip drawing(string name = "drawing_clip", Action<NBDrawContext, NBClip, SKCanvas>? builder = null, int frames = 1, int? start = null)
    {
        return new NBDrawingClip(name, builder, frames, start);
    }

    public static NBMaskedDrawingClip drawing_withmask(string name = "drawing_clip_withmask", 
        Action<NBDrawContext, NBClip, SKCanvas>? builder = null,
        Action<NBDrawContext, NBClip, SKCanvas>? maskBuilder = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int frames = 1, int? start = null)
    {
        return new NBMaskedDrawingClip(name, builder, maskBuilder, blend, frames, start);
    }

    public static NBTrack track()
    {
        return new NBTrack();
    }

    internal static void start(string[]? args = null)
    {
        App.Start(args);
    }

    public static void run()
    {
        run(stage(), []);
    }

    public static void run(NBStage stage, IList<NBClip> clips)
    {
        foreach(var clip in clips)
        {
            try
            {
                clip.Prepare();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Clip[{clip.Name} prepare fail: {ex.Message}]");
            }
        }

        NBWorkspace.Current = NBWorkspace.Create(stage, clips);
        start();
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
                clip.Prepare();
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

        NBGlobal.CheckOrLoadFFmpeg();

        var frames = work.Works[0].Measure();

        var fileName = fileInfo.FullName;

        IPlayable Playable = work.Works[0];

        var writer = new MediaWriter(fileName, stage.Width, stage.Height, stage.FrameRate, true);

        try
        {
            for (int CurrentFrame = 0; CurrentFrame < frames; CurrentFrame++)
            {
                using var bmp = Playable.RenderBitmap(stage, CurrentFrame, true);
                if (bmp == null) break;

                writer.WriteFrame(bmp);

                if (CurrentFrame % 10 == 0)
                {
                    var msg = $"正在导出视频... {CurrentFrame}/{frames}";
                    Console.WriteLine (msg);
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

    #region embed python

    public static void embed_python312_win32(string? pythonDir = null, [CallerFilePath] string filePath = "")
    {
        FileInfo file = new FileInfo(filePath);
        var dir = file.Directory?.FullName ?? "./";

        var dllDir = @"../_lib/python-3.12.8-embed-amd64/";

        var dllDirInfo = pythonDir == null ? new DirectoryInfo(Path.Combine(dir, dllDir)) : new DirectoryInfo(pythonDir);

        string pythonHomePath = dllDirInfo.FullName;
        string pythonDllPath = $"{pythonHomePath}python312.dll";
        // 对应python内的重要路径
        string[] py_paths = { "python312.zip", "lib", "lib/site-packages" };
        string pySearchPath = $"{pythonHomePath};";
        foreach (string p in py_paths)
        {
            pySearchPath += $"{pythonHomePath}/{p};";
        }

        Runtime.PythonDLL = pythonDllPath;
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDllPath);
        PythonEngine.PythonHome = pythonHomePath;
        PythonEngine.PythonPath = pySearchPath;
        PythonEngine.Initialize();
    }

    public static void embed_python312_macosx(string? pythonDir = null)
    {
        string pythonDllPath = pythonDir ?? $"/opt/homebrew/opt/python@3.12/Frameworks/Python.framework/Versions/3.12/Python";
        Runtime.PythonDLL = pythonDllPath;
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDllPath);
        PythonEngine.Initialize();
    }

    public static Python.Runtime.Py.GILState py_gil()
    {
        return Py.GIL();
    }

    public static PyObject py_module(string path)
    {
        var fileInfo = new FileInfo(path);
        var dir = fileInfo.Directory;
        using(py_gil())
        {
            if (dir != null)
            {
                dynamic sys = Py.Import("sys");
                sys.path.append(dir.FullName); // 加入路径    
            }

            var fileName = fileInfo.Name;
            if (fileInfo.Extension == ".py")
            {
                fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 3);
            }
            return Py.Import(fileName);
        }
    }

    public static SKBitmap? py_imdecode(PyObject obj)
    {
        // 1. 安全读取元组
        int width = Convert.ToInt32(obj[0]);
        int height = Convert.ToInt32(obj[1]);
        dynamic data = obj[2];
        byte[] rawBytes = (byte[])data; // bytes直接转C# byte[]

        int expected = width * height * 4;
        if (rawBytes.Length != expected)
        {
            throw new Exception($"图像数据长度不匹配：预期{expected}，实际{rawBytes.Length}");
        }

        // 2. 显式指定RGBA8888，和matplotlib canvas.buffer_rgba输出完全匹配
        SKImageInfo imgInfo = new SKImageInfo(width, height, SKColorType.Rgba8888);
        SKBitmap bitmap = new SKBitmap(imgInfo);

        // 3. 拷贝外部像素到SKBitmap自有内存，彻底解决生命周期问题
        unsafe
        {
            fixed (byte* srcPtr = rawBytes)
            {
                // 获取bitmap内部像素缓冲区指针
                byte* dstPtr = (byte*)bitmap.GetPixels();
                int totalBytes = width * height * 4;
                // 内存拷贝
                Buffer.MemoryCopy(srcPtr, dstPtr, totalBytes, totalBytes);
            }
        }

        return bitmap;
    }

    #endregion

    #region Widgets

    public static NBText TextBlock(string text, float fontSize = 40, SKColor? color = null, string? fontFamily = null, bool wrap = true, int textAlign = -1, Action<NBText>[]? styles = null)
    {
        var tb = new NBText { Text = text, FontFamily = fontFamily ?? "Arial", FontSize = fontSize, Foreground = color ?? SKColors.Black };
        tb.IsWrapText = wrap;
        tb.TextAlign = textAlign switch
        {
            0 => SKTextAlign.Center,
            <0 => SKTextAlign.Left,
            >0 => SKTextAlign.Right
        };
        tb.Styles(styles);
        return tb;
    }

    public static NBPanel Panel(NBVisual[]? childs)
    {
        var panel = new NBPanel();
        if (childs != null)
        {
            panel.Childs(childs);
        }
        return panel;
    }

    public static NBWrapPanel WrapPanel(NBVisual[]? childs, bool isHorizontal = true)
    {
        var panel = new NBWrapPanel { Orientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical };
        if (childs != null)
        {
            panel.Childs(childs);
        }
        return panel;
    }

    public static NBGrid Grid(NBVisual[]? childs, string? rowDef = null, string? colDef = null)
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

    public static NBTypst TypstFile(string path, float? width = null, float? height = null)
    {
        var file = new NBTypst();
        file.TypstFile = path;
        if (width.HasValue) file.Width = width.Value;
        if (height.HasValue) file.Height = height.Value;
        return file;
    }

    #endregion
}
