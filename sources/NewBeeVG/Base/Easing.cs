namespace NewBeeVG;

/// <summary>
/// 缓动函数工具类（精简版），doubao 写的，未检查
/// 所有函数接收 0~1 的进度值，返回 0~1 的缓动后进度
/// </summary>
public static class Easing
{
    // 常量定义（行业通用，减少魔法数字）
    private const double PI = Math.PI;
    private const double PI_HALF = PI / 2;
    private const double BACK_C1 = 1.70158;
    private const double BACK_C2 = BACK_C1 * 1.525;
    private const double BACK_C3 = BACK_C1 + 1;
    private const double ELASTIC_C4 = (2 * PI) / 3;
    private const double ELASTIC_C5 = (2 * PI) / 4.5;

    #region 基础缓动
    /// <summary>
    /// 线性缓动（匀速）
    /// </summary>
    public static double Linear(double progress)
    {
        progress = Clamp(progress);
        return progress;
    }
    #endregion

    #region 二次缓动（Quad）
    /// <summary>
    /// 二次淡入（慢→快）
    /// </summary>
    public static double QuadIn(double progress)
    {
        progress = Clamp(progress);
        return progress * progress;
    }

    /// <summary>
    /// 二次淡出（快→慢）
    /// </summary>
    public static double QuadOut(double progress)
    {
        progress = Clamp(progress);
        return 1 - (1 - progress) * (1 - progress);
    }

    /// <summary>
    /// 二次淡入淡出（慢→快→慢）
    /// </summary>
    public static double QuadInOut(double progress)
    {
        progress = Clamp(progress);
        return progress < 0.5 ? 2 * progress * progress : 1 - Math.Pow(-2 * progress + 2, 2) / 2;
    }
    #endregion

    #region 三次缓动（Cubic）
    public static double CubicIn(double progress)
    {
        progress = Clamp(progress);
        return progress * progress * progress;
    }

    public static double CubicOut(double progress)
    {
        progress = Clamp(progress);
        return 1 - Math.Pow(1 - progress, 3);
    }

    public static double CubicInOut(double progress)
    {
        progress = Clamp(progress);
        return progress < 0.5 ? 4 * progress * progress * progress : 1 - Math.Pow(-2 * progress + 2, 3) / 2;
    }
    #endregion

    #region 正弦缓动（Sine）
    public static double SineIn(double progress)
    {
        progress = Clamp(progress);
        return 1 - Math.Cos(progress * PI_HALF);
    }

    public static double SineOut(double progress)
    {
        progress = Clamp(progress);
        return Math.Sin(progress * PI_HALF);
    }

    public static double SineInOut(double progress)
    {
        progress = Clamp(progress);
        return -(Math.Cos(PI * progress) - 1) / 2;
    }
    #endregion

    #region 指数缓动（Expo）
    public static double ExpoIn(double progress)
    {
        progress = Clamp(progress);
        return progress == 0 ? 0 : Math.Pow(2, 10 * progress - 10);
    }

    public static double ExpoOut(double progress)
    {
        progress = Clamp(progress);
        return progress == 1 ? 1 : 1 - Math.Pow(2, -10 * progress);
    }

    public static double ExpoInOut(double progress)
    {
        progress = Clamp(progress);
        return progress == 0
            ? 0
            : progress == 1
                ? 1
                : progress < 0.5 ? Math.Pow(2, 20 * progress - 10) / 2
                : (2 - Math.Pow(2, -20 * progress + 10)) / 2;
    }
    #endregion

    #region 回弹缓动（Back）
    public static double BackIn(double progress)
    {
        progress = Clamp(progress);
        return BACK_C3 * progress * progress * progress - BACK_C1 * progress * progress;
    }

    public static double BackOut(double progress)
    {
        progress = Clamp(progress);
        return 1 + BACK_C3 * Math.Pow(progress - 1, 3) + BACK_C1 * Math.Pow(progress - 1, 2);
    }

    public static double BackInOut(double progress)
    {
        progress = Clamp(progress);
        return progress < 0.5
            ? (Math.Pow(2 * progress, 2) * ((BACK_C2 + 1) * 2 * progress - BACK_C2)) / 2
            : (Math.Pow(2 * progress - 2, 2) * ((BACK_C2 + 1) * (progress * 2 - 2) + BACK_C2) + 2) / 2;
    }
    #endregion

    #region 弹性缓动（Elastic）
    public static double ElasticIn(double progress)
    {
        progress = Clamp(progress);
        return progress == 0
            ? 0
            : progress == 1
                ? 1
                : -Math.Pow(2, 10 * progress - 10) * Math.Sin((progress * 10 - 10.75) * ELASTIC_C4);
    }

    public static double ElasticOut(double progress)
    {
        progress = Clamp(progress);
        return progress == 0
            ? 0
            : progress == 1
                ? 1
                : Math.Pow(2, -10 * progress) * Math.Sin((progress * 10 - 0.75) * ELASTIC_C4) + 1;
    }

    public static double ElasticInOut(double progress)
    {
        progress = Clamp(progress);
        return progress == 0
            ? 0
            : progress == 1
                ? 1
                : progress < 0.5
                    ? -(Math.Pow(2, 20 * progress - 10) * Math.Sin((20 * progress - 11.125) * ELASTIC_C5)) / 2
                    : (Math.Pow(2, -20 * progress + 10) * Math.Sin((20 * progress - 11.125) * ELASTIC_C5)) / 2 + 1;
    }
    #endregion

    /// <summary>
    /// 获取所有缓动函数的名称和对应的委托
    /// </summary>
    /// <returns>包含函数名和委托的元组列表</returns>
    public static List<Tuple<string, Func<double, double>>> All()
    {
        // 按分类添加所有缓动函数，名称与方法名保持一致（便于外部识别）
        var easingFunctions = new List<Tuple<string, Func<double, double>>>
        {
            // 基础缓动
            Tuple.Create(nameof(Linear), (Func<double, double>)Linear),
            
            // 二次缓动
            Tuple.Create(nameof(QuadIn), (Func<double, double>)QuadIn),
            Tuple.Create(nameof(QuadOut), (Func<double, double>)QuadOut),
            Tuple.Create(nameof(QuadInOut), (Func<double, double>)QuadInOut),
            
            // 三次缓动
            Tuple.Create(nameof(CubicIn), (Func<double, double>)CubicIn),
            Tuple.Create(nameof(CubicOut), (Func<double, double>)CubicOut),
            Tuple.Create(nameof(CubicInOut), (Func<double, double>)CubicInOut),
            
            // 正弦缓动
            Tuple.Create(nameof(SineIn), (Func<double, double>)SineIn),
            Tuple.Create(nameof(SineOut), (Func<double, double>)SineOut),
            Tuple.Create(nameof(SineInOut), (Func<double, double>)SineInOut),
            
            // 指数缓动
            Tuple.Create(nameof(ExpoIn), (Func<double, double>)ExpoIn),
            Tuple.Create(nameof(ExpoOut), (Func<double, double>)ExpoOut),
            Tuple.Create(nameof(ExpoInOut), (Func<double, double>)ExpoInOut),
            
            // 回弹缓动
            Tuple.Create(nameof(BackIn), (Func<double, double>)BackIn),
            Tuple.Create(nameof(BackOut), (Func<double, double>)BackOut),
            Tuple.Create(nameof(BackInOut), (Func<double, double>)BackInOut),
            
            // 弹性缓动
            Tuple.Create(nameof(ElasticIn), (Func<double, double>)ElasticIn),
            Tuple.Create(nameof(ElasticOut), (Func<double, double>)ElasticOut),
            Tuple.Create(nameof(ElasticInOut), (Func<double, double>)ElasticInOut)
        };

        return easingFunctions;
    }

    /// <summary>
    /// 辅助方法：限制进度在 0~1 之间（简化重载）
    /// </summary>
    private static double Clamp(double progress)
    {
        return Math.Max(0, Math.Min(1, progress));
    }
}
