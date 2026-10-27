using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// 视频主题色。
/// 一个主题色、三个中性色、一个分隔色，另加两个数据可视化用的功能色。
/// </summary>
public partial class NBVideoTheme
{
    /// <summary>主题色 #BF3B2B。用于最需要被看到的强调信息。</summary>
    public SKColor Primary { get; set; } = new SKColor(0xBF, 0x3B, 0x2B);

    /// <summary>背景色 #F4F1EA。画面底色。</summary>
    public SKColor Background { get; set; } = new SKColor(0xF4, 0xF1, 0xEA);

    /// <summary>主文字色 #17160F。标题与正文。</summary>
    public SKColor TextPrimary { get; set; } = new SKColor(0x17, 0x16, 0x0F);

    /// <summary>次文字色 #8C877C。副标题、注释、辅助信息。</summary>
    public SKColor TextSecondary { get; set; } = new SKColor(0x8C, 0x87, 0x7C);

    /// <summary>分隔色 #CFC9BB。细线、边框、表格线。</summary>
    public SKColor Divider { get; set; } = new SKColor(0xCF, 0xC9, 0xBB);

    /// <summary>正向色 #BF3B2B。默认与主题色一致。</summary>
    public SKColor Positive { get; set; } = new SKColor(0xBF, 0x3B, 0x2B);

    /// <summary>负向色 #45624E。</summary>
    public SKColor Negative { get; set; } = new SKColor(0x45, 0x62, 0x4E);

    /// <summary>主题名称（中文），用于主题选择器、预览标题等展示场景。</summary>
    public string Name { get; set; } = "默认";
}

public partial class NBVideoTheme
{
    /// <summary>
    /// 生成主题色预览图：以 <see cref="Background"/> 铺底，绘制标题与 7 个颜色槽位的色块、名称和十六进制值。
    /// </summary>
    /// <param name="width">预览图宽度（像素），必须大于 0。</param>
    /// <param name="height">预览图高度（像素），必须大于 0。</param>
    /// <returns>绘制完成的位图（Bgra8888 + 预乘 Alpha），调用方负责释放。</returns>
    public SKBitmap Preview(int width = 1920, int height = 1080)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "宽度必须大于 0");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "高度必须大于 0");

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bmp = new SKBitmap(info);

        using var canvas = new SKCanvas(bmp);

        // 以主题背景铺满整张预览图
        canvas.Clear(Background);

        // 所有尺寸按图像宽高等比缩放，保证不同分辨率下排版一致
        float padding = MathF.Max(16f, height * 0.05f);
        float titleSize = MathF.Max(16f, height * 0.05f);
        float labelSize = MathF.Max(11f, height * 0.026f);
        float hexSize = MathF.Max(9f, height * 0.021f);
        float gap = MathF.Max(8f, width * 0.012f);

        using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei") ?? SKTypeface.Default;
        using var titleFont = new SKFont(typeface, titleSize);
        using var labelFont = new SKFont(typeface, labelSize);
        using var hexFont = new SKFont(typeface, hexSize);

        using var titlePaint = new SKPaint { IsAntialias = true, Color = TextPrimary };
        using var labelPaint = new SKPaint { IsAntialias = true, Color = TextPrimary };
        using var hexPaint = new SKPaint { IsAntialias = true, Color = TextSecondary };

        // 标题：主题名称 + 说明；名称为空时退回纯英文标题
        string title = string.IsNullOrWhiteSpace(Name) ? "Video Theme Preview" : $"{Name} · Video Theme Preview";
        canvas.DrawText(title, padding, padding + titleSize, SKTextAlign.Left, titleFont, titlePaint);

        // 标题下方的主题色装饰条
        using (var accent = new SKPaint { IsAntialias = true, Color = Primary })
        {
            canvas.DrawRect(
                padding,
                padding + titleSize + gap * 0.4f,
                MathF.Max(40f, width * 0.06f),
                MathF.Max(3f, height * 0.006f),
                accent);
        }

        // 7 个颜色槽位，按 4 列 2 行排列
        var items = new (string Name, SKColor Color)[]
        {
            (nameof(Primary), Primary),
            (nameof(Background), Background),
            (nameof(TextPrimary), TextPrimary),
            (nameof(TextSecondary), TextSecondary),
            (nameof(Divider), Divider),
            (nameof(Positive), Positive),
            (nameof(Negative), Negative)
        };

        const int cols = 4;
        int rows = (items.Length + cols - 1) / cols;
        float gridTop = padding + titleSize + gap * 2f;
        float gridWidth = width - padding * 2f;
        float gridHeight = height - gridTop - padding;
        float cellWidth = (gridWidth - gap * (cols - 1)) / cols;
        float cellHeight = (gridHeight - gap * (rows - 1)) / rows;
        float textArea = labelSize + hexSize * 1.8f + gap * 0.6f;
        float swatchHeight = MathF.Max(4f, cellHeight - textArea);
        float radius = MathF.Min(cellWidth, swatchHeight) * 0.06f;

        using var border = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = MathF.Max(1f, height * 0.0015f),
            Color = Divider
        };

        for (int i = 0; i < items.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = padding + col * (cellWidth + gap);
            float y = gridTop + row * (cellHeight + gap);

            var swatchRect = new SKRect(x, y, x + cellWidth, y + swatchHeight);

            // 色块填充
            using (var fill = new SKPaint { IsAntialias = true, Color = items[i].Color })
            {
                canvas.DrawRoundRect(swatchRect, radius, radius, fill);
            }

            // 细边框，避免浅色色块与背景融为一体
            canvas.DrawRoundRect(swatchRect, radius, radius, border);

            // 颜色名称
            float nameBaseline = y + swatchHeight + labelSize + gap * 0.3f;
            canvas.DrawText(items[i].Name, x, nameBaseline, SKTextAlign.Left, labelFont, labelPaint);

            // 十六进制值
            var color = items[i].Color;
            string hex = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
            canvas.DrawText(hex, x, nameBaseline + hexSize * 1.6f, SKTextAlign.Left, hexFont, hexPaint);
        }

        canvas.Flush();
        return bmp;
    }
}

public class NBVideoThemes
{
    /// <summary>
    /// 返回全部 23 个内置主题模板，顺序与静态属性声明顺序一致。
    /// </summary>
    /// <returns>
    /// 新建的列表，调用方可自由排序、筛选或增删；
    /// 列表中的主题实例是共享的静态对象，请勿修改其颜色属性。
    /// </returns>
    public static List<NBVideoTheme> AllThemes()
    {
        // 每次调用返回新列表，避免调用方修改列表结构时影响其他使用者
        return new List<NBVideoTheme>
        {
            Tech,
            Eco,
            Finance,
            Healthcare,
            Education,
            Media,
            Retail,
            Nature,
            Ocean,
            Sunset,
            Minimal,
            Cyberpunk,
            Luxury,
            Retro,
            Candy,
            Arctic,
            Forest,
            Corporate,
            AI,
            Podcast,
            NightCopper,
            ParchmentDay,
            ColdArchive
        };
    }

    #region 主题模板

    // 说明：以下静态主题均为共享实例，请勿在运行时修改其属性（会影响所有使用方）。
    // 如需微调，请先创建一个新的 NBVideoTheme 再覆盖需要变化的颜色。

    /// <summary>
    /// 由 0xRRGGBB 创建不透明颜色（Alpha = 255）。
    /// 注意不要直接用 SKColor(uint)，它按 0xAARRGGBB 解析，6 位写法会得到全透明。
    /// </summary>
    private static SKColor Hex(uint rgb)
    {
        return new SKColor((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    /// <summary>
    /// 科技主题（暗色）：深空底 + 电光蓝，适合产品发布、技术解读、开发者内容。
    /// </summary>
    public static NBVideoTheme Tech { get; } = new NBVideoTheme
    {
        Name = "科技",
        Primary = Hex(0x2E6BFF),
        Background = Hex(0x0E1116),
        TextPrimary = Hex(0xF2F5FA),
        TextSecondary = Hex(0x8B94A6),
        Divider = Hex(0x262C36),
        Positive = Hex(0x22C55E),
        Negative = Hex(0xEF4444)
    };

    /// <summary>
    /// 环保主题（浅色）：自然绿 + 米白底，适合可持续发展、绿色能源、公益内容。
    /// </summary>
    public static NBVideoTheme Eco { get; } = new NBVideoTheme
    {
        Name = "环保",
        Primary = Hex(0x2E8B57),
        Background = Hex(0xF5FAF3),
        TextPrimary = Hex(0x14251A),
        TextSecondary = Hex(0x6B7F6E),
        Divider = Hex(0xD4E4D3),
        Positive = Hex(0x1F9D55),
        Negative = Hex(0xC2410C)
    };

    /// <summary>
    /// 金融主题（浅色）：香槟金 + 暖白底，适合财报、投研、财富管理内容。
    /// </summary>
    public static NBVideoTheme Finance { get; } = new NBVideoTheme
    {
        Name = "金融",
        Primary = Hex(0xC89B3C),
        Background = Hex(0xFAF7EF),
        TextPrimary = Hex(0x1C1A14),
        TextSecondary = Hex(0x7A7365),
        Divider = Hex(0xE3DCCB),
        Positive = Hex(0x1F7A4D),
        Negative = Hex(0xB23A32)
    };

    /// <summary>
    /// 医疗主题（浅色）：洁净青 + 冷白底，适合健康科普、医疗器械、诊疗流程内容。
    /// </summary>
    public static NBVideoTheme Healthcare { get; } = new NBVideoTheme
    {
        Name = "医疗",
        Primary = Hex(0x0E8F8C),
        Background = Hex(0xF3FAFA),
        TextPrimary = Hex(0x0F2426),
        TextSecondary = Hex(0x5F7B7C),
        Divider = Hex(0xCCE6E5),
        Positive = Hex(0x1E9E6A),
        Negative = Hex(0xC0392B)
    };

    /// <summary>
    /// 教育主题（浅色）：活力橙 + 奶油底，适合课程讲解、知识科普、培训课件。
    /// </summary>
    public static NBVideoTheme Education { get; } = new NBVideoTheme
    {
        Name = "教育",
        Primary = Hex(0xE07A2F),
        Background = Hex(0xFFF8F0),
        TextPrimary = Hex(0x241A12),
        TextSecondary = Hex(0x8A7565),
        Divider = Hex(0xF0DCC8),
        Positive = Hex(0x2F8F5B),
        Negative = Hex(0xC0392B)
    };

    /// <summary>
    /// 影视主题（暗色）：剧院紫 + 近黑底，适合预告片、影评、娱乐资讯。
    /// </summary>
    public static NBVideoTheme Media { get; } = new NBVideoTheme
    {
        Name = "影视",
        Primary = Hex(0x7C4DFF),
        Background = Hex(0x0B0A12),
        TextPrimary = Hex(0xF1EEFF),
        TextSecondary = Hex(0x918CA8),
        Divider = Hex(0x232030),
        Positive = Hex(0x34D399),
        Negative = Hex(0xF87171)
    };

    /// <summary>
    /// 零售主题（浅色）：促销红 + 暖白底，适合电商上新、折扣活动、带货内容。
    /// </summary>
    public static NBVideoTheme Retail { get; } = new NBVideoTheme
    {
        Name = "零售",
        Primary = Hex(0xE8483F),
        Background = Hex(0xFFF7F6),
        TextPrimary = Hex(0x251312),
        TextSecondary = Hex(0x8C6F6C),
        Divider = Hex(0xF2D8D5),
        Positive = Hex(0x2E8B57),
        Negative = Hex(0xB3261E)
    };

    /// <summary>
    /// 自然主题（浅色）：橄榄绿 + 亚麻底，适合户外、农业、人文旅行内容。
    /// </summary>
    public static NBVideoTheme Nature { get; } = new NBVideoTheme
    {
        Name = "自然",
        Primary = Hex(0x6B8E23),
        Background = Hex(0xF7F5EC),
        TextPrimary = Hex(0x221F14),
        TextSecondary = Hex(0x7C7663),
        Divider = Hex(0xE0DCC8),
        Positive = Hex(0x4F7D3A),
        Negative = Hex(0xB3541E)
    };

    /// <summary>
    /// 海洋主题（浅色）：深海蓝 + 浅水底，适合旅游、航运、生态环境内容。
    /// </summary>
    public static NBVideoTheme Ocean { get; } = new NBVideoTheme
    {
        Name = "海洋",
        Primary = Hex(0x0077B6),
        Background = Hex(0xF1F8FC),
        TextPrimary = Hex(0x0C1D2A),
        TextSecondary = Hex(0x5E7A8C),
        Divider = Hex(0xCFE3EF),
        Positive = Hex(0x1B998B),
        Negative = Hex(0xD1495B)
    };

    /// <summary>
    /// 日落主题（暗色）：珊瑚橙 + 深酒红底，适合情感叙事、音乐、生活方式内容。
    /// </summary>
    public static NBVideoTheme Sunset { get; } = new NBVideoTheme
    {
        Name = "日落",
        Primary = Hex(0xFF6B4A),
        Background = Hex(0x1A0F14),
        TextPrimary = Hex(0xFFF3EC),
        TextSecondary = Hex(0xB79A93),
        Divider = Hex(0x33202A),
        Positive = Hex(0x4ADE80),
        Negative = Hex(0xFF4D6D)
    };

    /// <summary>
    /// 极简主题（浅色）：黑白灰三色，适合观点表达、访谈、严肃文档类内容。
    /// </summary>
    public static NBVideoTheme Minimal { get; } = new NBVideoTheme
    {
        Name = "极简",
        Primary = Hex(0x111111),
        Background = Hex(0xFFFFFF),
        TextPrimary = Hex(0x111111),
        TextSecondary = Hex(0x6E6E6E),
        Divider = Hex(0xE5E5E5),
        Positive = Hex(0x1F7A4D),
        Negative = Hex(0xB3261E)
    };

    /// <summary>
    /// 赛博朋克主题（暗色）：霓虹青 + 电子紫黑底，适合游戏、AIGC、潮流文化内容。
    /// </summary>
    public static NBVideoTheme Cyberpunk { get; } = new NBVideoTheme
    {
        Name = "赛博朋克",
        Primary = Hex(0x00E5FF),
        Background = Hex(0x08060F),
        TextPrimary = Hex(0xEAF6FF),
        TextSecondary = Hex(0x7C8AA5),
        Divider = Hex(0x1B2130),
        Positive = Hex(0x39FF88),
        Negative = Hex(0xFF3B5C)
    };

    /// <summary>
    /// 奢华主题（暗色）：香槟金 + 曜石黑底，适合高端品牌、腕表、珠宝、私享服务内容。
    /// </summary>
    public static NBVideoTheme Luxury { get; } = new NBVideoTheme
    {
        Name = "奢华",
        Primary = Hex(0xD4AF37),
        Background = Hex(0x0C0B09),
        TextPrimary = Hex(0xF7F3E8),
        TextSecondary = Hex(0xA49A82),
        Divider = Hex(0x2A2620),
        Positive = Hex(0x4CAF7D),
        Negative = Hex(0xC0504D)
    };

    /// <summary>
    /// 复古主题（浅色）：做旧橙 + 泛黄纸底，适合历史、人文、怀旧叙事内容。
    /// </summary>
    public static NBVideoTheme Retro { get; } = new NBVideoTheme
    {
        Name = "复古",
        Primary = Hex(0xD97706),
        Background = Hex(0xFDF6E3),
        TextPrimary = Hex(0x2B2118),
        TextSecondary = Hex(0x8A7860),
        Divider = Hex(0xEBD9B8),
        Positive = Hex(0x3F8F5E),
        Negative = Hex(0xB5452C)
    };

    /// <summary>
    /// 糖果主题（浅色）：莓果粉 + 粉白底，适合美妆、母婴、甜品与年轻向内容。
    /// </summary>
    public static NBVideoTheme Candy { get; } = new NBVideoTheme
    {
        Name = "糖果",
        Primary = Hex(0xFF5C8A),
        Background = Hex(0xFFF5F8),
        TextPrimary = Hex(0x2A1720),
        TextSecondary = Hex(0x8F6C7B),
        Divider = Hex(0xF7D7E1),
        Positive = Hex(0x2FA36B),
        Negative = Hex(0xC2334D)
    };

    /// <summary>
    /// 极地主题（浅色）：冰川蓝 + 冷白底，适合数据报告、科研、冬季与清洁能源内容。
    /// </summary>
    public static NBVideoTheme Arctic { get; } = new NBVideoTheme
    {
        Name = "极地",
        Primary = Hex(0x4FA3D1),
        Background = Hex(0xF4FAFD),
        TextPrimary = Hex(0x132730),
        TextSecondary = Hex(0x6C8794),
        Divider = Hex(0xD6E7F0),
        Positive = Hex(0x2F9E8F),
        Negative = Hex(0xC2544F)
    };

    /// <summary>
    /// 深林主题（暗色）：苔绿 + 墨绿黑底，适合生态纪录片、户外品牌、可持续议题。
    /// </summary>
    public static NBVideoTheme Forest { get; } = new NBVideoTheme
    {
        Name = "深林",
        Primary = Hex(0x3FA34D),
        Background = Hex(0x0D1512),
        TextPrimary = Hex(0xEAF4EC),
        TextSecondary = Hex(0x8FA396),
        Divider = Hex(0x1E2A24),
        Positive = Hex(0x5BD37E),
        Negative = Hex(0xE5675F)
    };

    /// <summary>
    /// 商务主题（浅色）：稳重蓝 + 浅灰底，适合企业介绍、年报、B 端方案演示。
    /// </summary>
    public static NBVideoTheme Corporate { get; } = new NBVideoTheme
    {
        Name = "商务",
        Primary = Hex(0x1F4E8C),
        Background = Hex(0xF5F7FA),
        TextPrimary = Hex(0x121A24),
        TextSecondary = Hex(0x66707C),
        Divider = Hex(0xDDE3EA),
        Positive = Hex(0x2E7D5B),
        Negative = Hex(0xB23A32)
    };

    /// <summary>
    /// 智能主题（浅色）：AI 紫 + 淡紫底，适合人工智能、算法解读、创新科技内容。
    /// </summary>
    public static NBVideoTheme AI { get; } = new NBVideoTheme
    {
        Name = "智能",
        Primary = Hex(0x8B5CF6),
        Background = Hex(0xF7F5FF),
        TextPrimary = Hex(0x191428),
        TextSecondary = Hex(0x6F6885),
        Divider = Hex(0xE4DFF7),
        Positive = Hex(0x10B981),
        Negative = Hex(0xEF4444)
    };

    /// <summary>
    /// 播客主题（暗色）：琥珀金 + 深棕黑底，适合访谈、音频节目、口播类内容。
    /// </summary>
    public static NBVideoTheme Podcast { get; } = new NBVideoTheme
    {
        Name = "播客",
        Primary = Hex(0xE8A33D),
        Background = Hex(0x14100C),
        TextPrimary = Hex(0xFBF3E6),
        TextSecondary = Hex(0xA89680),
        Divider = Hex(0x2B231B),
        Positive = Hex(0x58C08A),
        Negative = Hex(0xE06C5A)
    };

    /// <summary>
    /// 夜墨铜金（暗色）：夜墨蓝底 + 铜金强调 + 冷青辅助，适合档案感、财经解说、纪录片式内容。
    /// 由 ContentTheme.NightCopper 映射而来；Sweep / PaperTint / PaperLight / PaperGlow / Shadow
    /// 属于原主题的扩展色，不在 NBVideoTheme 的 7 个颜色槽位内。
    /// </summary>
    public static NBVideoTheme NightCopper { get; } = new NBVideoTheme
    {
        Name = "夜墨铜金",
        Primary = Hex(0xC2914E),
        Background = Hex(0x0A1420),
        TextPrimary = Hex(0xEFE6D2),
        TextSecondary = Hex(0x4E8FA0),
        Divider = Hex(0x4E473E),
        Positive = Hex(0xCE4A32),
        Negative = Hex(0x3F7D5C)
    };

    /// <summary>
    /// 羊皮纸日间（浅色）：暖纸底 + 陶红强调，适合档案、文件、引文页；
    /// 与 <see cref="NBVideoTheme"/> 的默认配色一致。
    /// </summary>
    public static NBVideoTheme ParchmentDay { get; } = new NBVideoTheme
    {
        Name = "羊皮纸日间",
        Primary = Hex(0xBF3B2B),
        Background = Hex(0xF4F1EA),
        TextPrimary = Hex(0x17160F),
        TextSecondary = Hex(0x8C877C),
        Divider = Hex(0xCFC9BB),
        Positive = Hex(0xBF3B2B),
        Negative = Hex(0x45624E)
    };

    /// <summary>
    /// 冷青夜航（暗色）：深青黑底 + 冷青强调 + 暗金次文，适合机制说明、数据段落、技术叙事。
    /// 由 ContentTheme.ColdArchive 映射而来；扩展色同样不在 NBVideoTheme 的 7 个颜色槽位内。
    /// </summary>
    public static NBVideoTheme ColdArchive { get; } = new NBVideoTheme
    {
        Name = "冷青夜航",
        Primary = Hex(0x4E8FA0),
        Background = Hex(0x081118),
        TextPrimary = Hex(0xE4ECF0),
        TextSecondary = Hex(0xA98A5E),
        Divider = Hex(0x2B3942),
        Positive = Hex(0xCE4A32),
        Negative = Hex(0x3F7D5C)
    };

    #endregion
}