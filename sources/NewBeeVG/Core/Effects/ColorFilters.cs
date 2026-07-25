using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// 静态工具类，封装了基于 SkiaSharp 的常见颜色滤镜（Color Filter）。
/// 每个方法返回一个 NBColorFilter 对象，可应用于图像或画布的颜色变换。
/// </summary>
public static class ColorFilters
{
    // ========================================================================
    // 1. 基础颜色矩阵（已有）
    // ========================================================================

    /// <summary>
    /// 灰度滤镜（黑白转换）。
    /// 将彩色图像转换为灰度图像，使用标准亮度权重（ITU-R BT.709）：R=0.299, G=0.587, B=0.114。
    /// </summary>
    /// <param name="grayMat">可选的自定义灰度矩阵（4x5 浮点数数组），若为 null 则使用默认权重。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Gray(float[]? grayMat = null)
    {
        grayMat ??= new float[] {
                        0.299f,0.587f,0.114f,0,0,
                        0.299f,0.587f,0.114f,0,0,
                        0.299f,0.587f,0.114f,0,0,
                        0,0,0,1,0
                    };

        var filterGray = SKColorFilter.CreateColorMatrix(grayMat);
        return new NBSimpleColorFilter(filterGray);
    }

    /// <summary>
    /// 从自定义颜色矩阵创建颜色滤镜。
    /// 颜色矩阵为 4x5 矩阵（20 个 float），用于线性变换 RGBA 通道。
    /// </summary>
    /// <param name="mat">4x5 浮点数数组，按行优先顺序排列。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter FromColorMatrix(float[] mat)
    {
        var filter = SKColorFilter.CreateColorMatrix(mat);
        return new NBSimpleColorFilter(filter);
    }

    // ========================================================================
    // 2. 亮度 / 对比度 / 饱和度 / 色相（通过颜色矩阵）
    // ========================================================================

    /// <summary>
    /// 亮度调节滤镜。
    /// 增加或减少图像的整体亮度。
    /// </summary>
    /// <param name="brightness">亮度偏移量，范围 -1.0 ~ 1.0。正值变亮，负值变暗。0 表示不变。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Brightness(float brightness)
    {
        // 亮度调整矩阵：将每个颜色通道加上偏移量
        float[] mat = {
            1, 0, 0, 0, brightness,
            0, 1, 0, 0, brightness,
            0, 0, 1, 0, brightness,
            0, 0, 0, 1, 0
        };
        return FromColorMatrix(mat);
    }

    /// <summary>
    /// 对比度调节滤镜。
    /// 调整图像明暗对比度，使图像更鲜艳或更柔和。
    /// </summary>
    /// <param name="contrast">对比度系数，0 为全灰，1 为原始，大于 1 增加对比度（如 1.5）。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Contrast(float contrast)
    {
        // 对比度公式：C' = (C - 0.5) * contrast + 0.5
        float t = (1 - contrast) / 2f;
        float[] mat = {
            contrast, 0,        0,        0, t,
            0,        contrast, 0,        0, t,
            0,        0,        contrast, 0, t,
            0,        0,        0,        1, 0
        };
        return FromColorMatrix(mat);
    }

    /// <summary>
    /// 亮度 + 对比度组合调节（一次矩阵运算，效率更高）。
    /// </summary>
    /// <param name="brightness">亮度偏移量（-1~1）。</param>
    /// <param name="contrast">对比度系数（>=0）。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter BrightnessContrast(float brightness, float contrast)
    {
        // 组合：先应用 contrast，再应用 brightness
        float t = (1 - contrast) / 2f;
        float[] mat = {
            contrast, 0,        0,        0, t + brightness,
            0,        contrast, 0,        0, t + brightness,
            0,        0,        contrast, 0, t + brightness,
            0,        0,        0,        1, 0
        };
        return FromColorMatrix(mat);
    }

    /// <summary>
    /// 饱和度调节滤镜。
    /// 调整图像颜色饱和度，0 为完全灰度，1 为原始，大于 1 增加饱和度。
    /// </summary>
    /// <param name="saturation">饱和度系数，0.0 ~ 2.0 或更高。0 为灰度，1 不变。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Saturation(float saturation)
    {
        // 饱和度矩阵：基于灰度权重
        float rW = 0.299f, gW = 0.587f, bW = 0.114f;
        float oneMinusS = 1 - saturation;
        float[] mat = {
            rW * oneMinusS + saturation, gW * oneMinusS,                    bW * oneMinusS,                    0, 0,
            rW * oneMinusS,                    gW * oneMinusS + saturation, bW * oneMinusS,                    0, 0,
            rW * oneMinusS,                    gW * oneMinusS,                    bW * oneMinusS + saturation, 0, 0,
            0,                             0,                             0,                             1, 0
        };
        return FromColorMatrix(mat);
    }

    /// <summary>
    /// 色相旋转滤镜。
    /// 将图像颜色沿色环旋转指定角度，实现色调偏移。
    /// </summary>
    /// <param name="degrees">旋转角度（度），范围 0~360。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter HueRotation(float degrees)
    {
        // 将角度转为弧度
        float radians = degrees * (float)Math.PI / 180f;
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);

        // 色相旋转矩阵（基于 RGB 空间）
        // 参考：https://beesbuzz.biz/code/16-hsv-color-transforms
        float rW = 0.299f, gW = 0.587f, bW = 0.114f;
        float oneMinusR = 1 - rW;
        float oneMinusG = 1 - gW;
        float oneMinusB = 1 - bW;

        float[] mat = {
            rW + (oneMinusR) * cos - (rW) * sin, // R' -> R
            gW - (gW) * cos - (gW) * sin,       // G' -> R
            bW - (bW) * cos + (oneMinusB) * sin, // B' -> R
            0, 0,
            rW - (rW) * cos + (oneMinusR) * sin, // R' -> G
            gW + (oneMinusG) * cos - (gW) * sin, // G' -> G
            bW - (bW) * cos - (bW) * sin,       // B' -> G
            0, 0,
            rW - (rW) * cos - (rW) * sin,       // R' -> B
            gW - (gW) * cos + (oneMinusG) * sin, // G' -> B
            bW + (oneMinusB) * cos - (bW) * sin, // B' -> B
            0, 0,
            0, 0, 0, 1, 0
        };
        return FromColorMatrix(mat);
    }

    // ========================================================================
    // 3. 通道调整（颜色平衡、色调）
    // ========================================================================

    /// <summary>
    /// 颜色平衡（通道乘数）。
    /// 分别调整红、绿、蓝通道的增益，实现色彩偏移。
    /// </summary>
    /// <param name="redScale">红色通道乘数（0.0 ~ 2.0）。</param>
    /// <param name="greenScale">绿色通道乘数（0.0 ~ 2.0）。</param>
    /// <param name="blueScale">蓝色通道乘数（0.0 ~ 2.0）。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter ColorBalance(float redScale, float greenScale, float blueScale)
    {
        float[] mat = {
            redScale, 0,        0,        0, 0,
            0,        greenScale, 0,        0, 0,
            0,        0,        blueScale, 0, 0,
            0,        0,        0,        1, 0
        };
        return FromColorMatrix(mat);
    }

    /// <summary>
    /// 色调（Tint）滤镜。
    /// 为图像叠加一种颜色（类似彩色滤镜），同时保留亮度信息。
    /// </summary>
    /// <param name="tintColor">目标色调颜色。</param>
    /// <param name="strength">混合强度（0~1），0 为原图，1 完全着色。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Tint(SKColor tintColor, float strength = 1.0f)
    {
        // 方法：先将图像转为灰度，然后乘以目标颜色的 RGB，再与原图混合
        // 简化实现：直接使用颜色矩阵混合
        float r = tintColor.Red / 255f;
        float g = tintColor.Green / 255f;
        float b = tintColor.Blue / 255f;
        float s = strength;
        float t = 1 - s;

        // 混合矩阵：原图 * t + (灰度图 * tintColor) * s
        // 灰度权重
        float rW = 0.299f, gW = 0.587f, bW = 0.114f;
        float[] mat = {
            t + s * rW * r,     s * gW * r,     s * bW * r,     0, 0,
            s * rW * g,     t + s * gW * g,     s * bW * g,     0, 0,
            s * rW * b,     s * gW * b,     t + s * bW * b,     0, 0,
            0,              0,              0,              1, 0
        };
        return FromColorMatrix(mat);
    }

    // ========================================================================
    // 4. 特殊效果（反转、复古、曝光等）
    // ========================================================================

    /// <summary>
    /// 反转（负片）滤镜。
    /// 将图像颜色反转，实现摄影负片效果。
    /// </summary>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Invert()
    {
        float[] mat = {
            -1,  0,  0, 0, 1,
             0, -1,  0, 0, 1,
             0,  0, -1, 0, 1,
             0,  0,  0, 1, 0
        };
        return FromColorMatrix(mat);
    }

    /// <summary>
    /// 复古（怀旧）滤镜（Sepia）。
    /// 模拟老旧照片的棕褐色调。
    /// </summary>
    /// <param name="intensity">复古强度，0~1，默认 1 完全复古。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Sepia(float intensity = 1.0f)
    {
        // 标准 Sepia 矩阵
        float[] sepiaMat = {
        0.393f, 0.769f, 0.189f, 0, 0,
        0.349f, 0.686f, 0.168f, 0, 0,
        0.272f, 0.534f, 0.131f, 0, 0,
        0,      0,      0,      1, 0
    };
        if (intensity >= 1.0f)
            return FromColorMatrix(sepiaMat);
        else
        {
            // 创建 sepia 滤镜和单位矩阵滤镜
            var sepiaFilter = SKColorFilter.CreateColorMatrix(sepiaMat);
            var identity = SKColorFilter.CreateColorMatrix(new float[] {
            1, 0, 0, 0, 0,
            0, 1, 0, 0, 0,
            0, 0, 1, 0, 0,
            0, 0, 0, 1, 0
        });
            // 正确顺序：weight, filter0(原图), filter1(sepia)
            var blended = SKColorFilter.CreateLerp(intensity, identity, sepiaFilter);
            return new NBSimpleColorFilter(blended);
        }
    }

    /// <summary>
    /// 曝光（Exposure）调节。
    /// 模拟相机曝光调整，使图像变亮或变暗（对数映射）。
    /// </summary>
    /// <param name="ev">曝光补偿值，单位 EV（曝光值）。正数增加曝光，负数减少。通常范围 -2.0 ~ 2.0。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Exposure(float ev)
    {
        // 曝光本质是亮度乘数：2^ev
        float gain = (float)Math.Pow(2, ev);
        float[] mat = {
            gain, 0,    0,    0, 0,
            0,    gain, 0,    0, 0,
            0,    0,    gain, 0, 0,
            0,    0,    0,    1, 0
        };
        return FromColorMatrix(mat);
    }

    /// <summary>
    /// Gamma 校正。
    /// 对图像进行 Gamma 校正，调整中间调亮度。
    /// </summary>
    /// <param name="gamma">Gamma 值（>0）。小于 1 使图像变亮，大于 1 使图像变暗。常用 0.45 ~ 2.2。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Gamma(float gamma)
    {
        // Gamma 校正通常需要非线性变换，但可用颜色矩阵近似（分段或幂函数）。
        // 这里使用查找表（LUT）方式更精确，但为保持一致性，使用颜色矩阵近似不准确。
        // 推荐使用 CreateTable 实现精确 Gamma。
        // 因为用户可能期望精确，我们用 CreateTable。
        byte[] table = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            float v = i / 255f;
            v = (float)Math.Pow(v, 1.0f / gamma);
            table[i] = (byte)(v * 255);
        }
        var filter = SKColorFilter.CreateTable(table);
        return new NBSimpleColorFilter(filter);
    }

    // ========================================================================
    // 5. 混合模式与光照
    // ========================================================================

    /// <summary>
    /// 使用混合模式叠加颜色。
    /// 类似在图像上覆盖一层颜色，并应用混合模式（如 Multiply、Screen、Overlay 等）。
    /// </summary>
    /// <param name="color">要叠加的颜色。</param>
    /// <param name="mode">混合模式（如 SKBlendMode.Multiply）。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter ColorOverlay(SKColor color, SKBlendMode mode)
    {
        // 创建一个纯色滤镜
        var colorFilter = SKColorFilter.CreateBlendMode(color, mode);
        return new NBSimpleColorFilter(colorFilter);
    }

    /// <summary>
    /// 光照颜色滤镜。
    /// 使用 SKColorFilter.CreateLighting 调整颜色通道的乘数和偏移，模拟光照。
    /// </summary>
    /// <param name="mul">乘法颜色（每个通道乘数）。</param>
    /// <param name="add">加法颜色（每个通道偏移量）。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Lighting(SKColor mul, SKColor add)
    {
        var filter = SKColorFilter.CreateLighting(mul, add);
        return new NBSimpleColorFilter(filter);
    }

    // ========================================================================
    // 6. 高级：组合与混合
    // ========================================================================

    /// <summary>
    /// 线性插值混合两个颜色滤镜。
    /// 根据权重在滤镜 A 和 B 之间进行混合。
    /// </summary>
    /// <param name="filterA">滤镜 A。</param>
    /// <param name="filterB">滤镜 B。</param>
    /// <param name="weight">权重（0~1），0 为完全 A，1 为完全 B。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Lerp(NBColorFilter filterA, NBColorFilter filterB, float weight)
    {
        // 假设 NBColorFilter 有 CreateFilter() 方法
        var skA = filterA.CreateFilter();
        var skB = filterB.CreateFilter();
        var blended = SKColorFilter.CreateLerp(weight, skA, skB);
        return new NBSimpleColorFilter(blended);
    }

    /// <summary>
    /// 组合两个颜色滤镜（先应用 inner，再应用 outer）。
    /// 相当于外层滤镜作用于内层滤镜的结果。
    /// </summary>
    /// <param name="outer">外层滤镜（后应用）。</param>
    /// <param name="inner">内层滤镜（先应用）。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Compose(NBColorFilter outer, NBColorFilter inner)
    {
        var composed = SKColorFilter.CreateCompose(outer.CreateFilter(), inner.CreateFilter());
        return new NBSimpleColorFilter(composed);
    }

    // ========================================================================
    // 7. 查找表（LUT）滤镜
    // ========================================================================

    /// <summary>
    /// 使用查找表（LUT）进行颜色映射。
    /// 可以实现曲线、色阶、色调分离、伪彩色等效果。
    /// </summary>
    /// <param name="table">长度为 256 的字节数组，将每个亮度值映射为新值。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Table(byte[] table)
    {
        if (table.Length != 256)
            throw new ArgumentException("Table must have exactly 256 entries.");
        var filter = SKColorFilter.CreateTable(table);
        return new NBSimpleColorFilter(filter);
    }

    /// <summary>
    /// 色调分离（Posterize）效果。
    /// 减少每个通道的色阶数，产生类似插画的效果。
    /// </summary>
    /// <param name="levels">每个通道的色阶数（如 4 表示每个通道只有 4 个级别）。</param>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter Posterize(int levels)
    {
        if (levels < 2) throw new ArgumentException("Levels must be at least 2.");
        byte[] table = new byte[256];
        float step = 255f / (levels - 1);
        for (int i = 0; i < 256; i++)
        {
            // 量化到最近的级别
            int idx = (int)Math.Round(i / step);
            table[i] = (byte)(idx * step);
        }
        return Table(table);
    }

    // ========================================================================
    // 8. 常用颜色矩阵预设
    // ========================================================================

    /// <summary>
    /// 黑白滤镜（高对比度黑白）。
    /// 使用非标准权重，获得更强烈的黑白对比。
    /// </summary>
    /// <returns>封装了 SKColorFilter 的 NBColorFilter 对象。</returns>
    public static NBColorFilter BlackAndWhite()
    {
        // 使用均匀权重或自定义权重
        float[] mat = {
            0.3f, 0.59f, 0.11f, 0, 0,
            0.3f, 0.59f, 0.11f, 0, 0,
            0.3f, 0.59f, 0.11f, 0, 0,
            0,    0,    0,     1, 0
        };
        return FromColorMatrix(mat);
    }
}
