using NewBeeVG.Internal;
using SkiaSharp;

namespace NewBeeVG;

public class QWenVoices
{
    /// <summary>
    /// 晨煦. 标准普通话，带部分北方口音。阳光、温暖、活力、朝气（男性）
    /// </summary>
    public static string Male_Ethan = "Ethan";

    /// <summary>
    /// 音色名：芊悦
    ///  描述：阳光积极、亲切自然小姐姐（女性）
    /// </summary>
    public static string Female_Cherry = "Cherry";

    /// <summary>
    /// 音色名：苏瑶
    /// 描述：温柔小姐姐（女性）
    /// </summary>
    public static string Female_Serena = "Serena";

    /// <summary>
    /// 音色名：千雪
    /// 描述：二次元虚拟女友（女性）
    /// </summary>
    public static string Female_Chelsie = "Chelsie";

    /// <summary>
    /// 音色名：茉兔
    /// 描述：撒娇搞怪，逗你开心（女性）
    /// </summary>
    public static string Female_Momo = "Momo";

    /// <summary>
    /// 音色名：十三
    /// 描述：拽拽的、可爱的小暴躁（女性）
    /// </summary>
    public static string Female_Vivian = "Vivian";

    /// <summary>
    /// 音色名：月白
    /// 描述：率性帅气的月白（男性）
    /// </summary>
    public static string Male_Moon = "Moon";

    /// <summary>
    /// 音色名：四月
    /// 描述：知性与温柔的碰撞（女性）
    /// </summary>
    public static string Female_Maia = "Maia";

    /// <summary>
    /// 音色名：凯
    /// 描述：耳朵的一场SPA（男性）
    /// </summary>
    public static string Male_Kai = "Kai";

    /// <summary>
    /// 音色名：不吃鱼
    /// 描述：不会翘舌音的设计师（男性）
    /// </summary>
    public static string Male_Nofish = "Nofish";

    /// <summary>
    /// 音色名：萌宝
    /// 描述：喝酒不打醉拳的小萝莉（女性）
    /// </summary>
    public static string Female_Bella = "Bella";

    /// <summary>
    /// 音色名：沧明子
    /// 描述：沉稳睿智的老者，沧桑如松却心明如镜（男性）
    /// </summary>
    public static string Male_EldricSage = "Eldric Sage";

    /// <summary>
    /// 音色名：乖小妹
    /// 描述：温顺如春水，乖巧如初雪（女性）
    /// </summary>
    public static string Female_Mia = "Mia";

    /// <summary>
    /// 音色名：沙小弥
    /// 描述：聪明伶俐的小大人，童真未泯却早慧如禅（男性）
    /// </summary>
    public static string Male_Mochi = "Mochi";

    /// <summary>
    /// 音色名：燕铮莺
    /// 描述：声音洪亮，吐字清晰，人物鲜活，听得人热血沸腾；金戈铁马入梦来，字正腔圆间尽显千面人声的江湖（女性）
    /// </summary>
    public static string Female_Bellona = "Bellona";

    /// <summary>
    /// 音色名：田叔
    /// 描述：一口独特的沙哑烟嗓，一开口便道尽了千军万马与江湖豪情（男性）
    /// </summary>
    public static string Male_Vincent = "Vincent";

    /// <summary>
    /// 音色名：萌小姬
    /// 描述：“萌属性”爆棚的小萝莉（女性）
    /// </summary>
    public static string Female_Bunny = "Bunny";

    /// <summary>
    /// 音色名：阿闻
    /// 描述：平直的基线语调，字正腔圆的咬字发音，这就是最专业的新闻主持人（男性）
    /// </summary>
    public static string Male_Neil = "Neil";

    /// <summary>
    /// 音色名：墨讲师
    /// 描述：既保持学科严谨性，又通过叙事技巧将复杂知识转化为可消化的认知模块（女性）
    /// </summary>
    public static string Female_Elias = "Elias";

    /// <summary>
    /// 音色名：徐大爷
    /// 描述：被岁月和旱烟浸泡过的质朴嗓音，不疾不徐地摇开了满村的奇闻异事（男性）
    /// </summary>
    public static string Male_Arthur = "Arthur";

    /// <summary>
    /// 音色名：邻家妹妹
    /// 描述：糯米糍一样又软又黏的嗓音，那一声声拉长了的“哥哥”，甜得能把人的骨头都叫酥了（女性）
    /// </summary>
    public static string Female_Nini = "Nini";

    /// <summary>
    /// 音色名：小婉
    /// 描述：温和舒缓的声线，助你更快地进入睡眠，晚安，好梦（女性）
    /// </summary>
    public static string Female_Seren = "Seren";

    /// <summary>
    /// 音色名：顽屁小孩
    /// 描述：调皮捣蛋却充满童真的他来了，这是你记忆中的小新吗（男性）
    /// </summary>
    public static string Male_Pip = "Pip";

    /// <summary>
    /// 音色名：少女阿月
    /// 描述：平时是甜到发腻的迷糊少女音，但在喊出“代表月亮消灭你”时，瞬间充满不容置疑的爱与正义（女性）
    /// </summary>
    public static string Female_Stella = "Stella";
}

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
