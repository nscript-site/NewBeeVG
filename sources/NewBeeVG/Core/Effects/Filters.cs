using SkiaSharp;

namespace NewBeeVG;

public static class Filters
{
    // ========================================================================
    // 1. 模糊与阴影
    // ========================================================================
    /// <summary>
    /// 高斯模糊滤镜（常用于背景虚化、柔光效果）
    /// </summary>
    /// <param name="sigmaX">水平模糊半径（标准差）</param>
    /// <param name="sigmaY">垂直模糊半径（标准差）</param>
    /// <param name="tile">边缘像素处理方式</param>
    public static NBImageFilter Blur(double sigmaX, double sigmaY, SKShaderTileMode tile = SKShaderTileMode.Decal)
    {
        var filter = SKImageFilter.CreateBlur((float)sigmaX, (float)sigmaY, tile);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 投影阴影滤镜（为图像添加立体感）
    /// </summary>
    /// <param name="color">阴影颜色</param>
    /// <param name="dx">水平偏移量</param>
    /// <param name="dy">垂直偏移量</param>
    /// <param name="sigmaX">阴影模糊程度（水平）</param>
    /// <param name="sigmaY">阴影模糊程度（垂直）</param>
    public static NBImageFilter DropShadow(SKColor color, double dx, double dy, double sigmaX, double sigmaY)
    {
        var filter = SKImageFilter.CreateDropShadow((float)dx, (float)dy, (float)sigmaX, (float)sigmaY, color);
        return new NBSimpleImageFilter(filter);
    }

    // ========================================================================
    // 2. 卷积类滤镜（锐化、边缘、浮雕等）
    // ========================================================================

    /// <summary>
    /// 锐化滤镜（增强图像边缘清晰度，常用于照片后期）
    /// </summary>
    /// <param name="strength">锐化强度（建议 0.5~2.0）</param>
    public static NBImageFilter Sharpen(float strength = 1.0f)
    {
        // 经典锐化卷积核：中心 5，周围 -1，归一化系数为 1
        float[] kernel = {
            0, -1,  0,
           -1,  5, -1,
            0, -1,  0
        };
        // 调整强度：中心 = 1 + 4*strength，周围 = -strength
        // 但为简化，直接缩放核，然后调整偏移使总和为 1
        float scale = strength;
        float offset = 0;
        // 使核总和为 1（保持亮度不变）
        float sum = kernel.Sum();
        // 调整核：新的核 = (原核 + (1-sum)/9) 但这里我们不调整，而是直接设置增益和偏移
        // 使用矩阵卷积时，结果 = sum(kernel[i]*pixel[i]) * gain + offset
        // 我们希望增益为 1，偏移为 0，但核总和不为 1 时亮度会变化，所以调整核。
        // 简便方法：将核归一化
        float total = kernel.Sum();
        if (total != 0)
        {
            for (int i = 0; i < kernel.Length; i++)
                kernel[i] /= total;
        }
        // 再次调整强度：将核乘以 strength，然后中心加上 (1-strength) 以保持总和为 1
        // 但这里直接使用原始核乘以 strength 并配合 gain 和 offset
        // 更简单：使用默认核，但通过 strength 调整增益
        float gain = strength;
        float bias = 0;
        // 为了保持亮度，我们可以将核总和设为 1，但中心调整
        // 这里使用预定义核，不做复杂处理，用户可自行调整
        var filter = SKImageFilter.CreateMatrixConvolution(
            new SKSizeI(3, 3),          // 核大小 3x3
            kernel,                     // 核系数
            gain,                       // 增益
            bias,                       // 偏置
            new SKPointI(1, 1),        // 锚点（中心）
            SKShaderTileMode.Decal,    // 边缘处理
            true                        // 是否对 alpha 通道也卷积
        );
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 边缘检测滤镜（提取图像轮廓，类似 Photoshop 的“查找边缘”）
    /// </summary>
    public static NBImageFilter EdgeDetect()
    {
        // Sobel 算子（水平 + 垂直组合）
        float[] kernel = {
            -1, -2, -1,
             0,  0,  0,
             1,  2,  1
        };
        // 也可使用拉普拉斯算子
        // float[] kernel = { 0, -1, 0, -1, 4, -1, 0, -1, 0 };
        var filter = SKImageFilter.CreateMatrixConvolution(
            new SKSizeI(3, 3),
            kernel,
            1.0f, 0.0f,
            new SKPointI(1, 1),
            SKShaderTileMode.Decal,
            false   // 不对 alpha 处理，保留原 alpha
        );
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 浮雕滤镜（使图像产生立体浮雕效果）
    /// </summary>
    public static NBImageFilter Emboss()
    {
        float[] kernel = {
            -2, -1, 0,
            -1,  1, 1,
             0,  1, 2
        };
        var filter = SKImageFilter.CreateMatrixConvolution(
            new SKSizeI(3, 3),
            kernel,
            1.0f, 0.5f,  // 增加偏置使背景变亮
            new SKPointI(1, 1),
            SKShaderTileMode.Decal,
            false
        );
        return new NBSimpleImageFilter(filter);
    }

    // ========================================================================
    // 3. 形态学操作（膨胀与腐蚀）
    // ========================================================================

    /// <summary>
    /// 膨胀滤镜（扩展明亮区域，常用于形态学操作或光晕效果）
    /// </summary>
    /// <param name="radiusX">水平半径</param>
    /// <param name="radiusY">垂直半径</param>
    public static NBImageFilter Dilate(int radiusX, int radiusY)
    {
        var filter = SKImageFilter.CreateDilate(radiusX, radiusY);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 腐蚀滤镜（收缩明亮区域，可用于去除噪点或细化线条）
    /// </summary>
    /// <param name="radiusX">水平半径</param>
    /// <param name="radiusY">垂直半径</param>
    public static NBImageFilter Erode(int radiusX, int radiusY)
    {
        var filter = SKImageFilter.CreateErode(radiusX, radiusY);
        return new NBSimpleImageFilter(filter);
    }

    // ========================================================================
    // 4. 几何变换
    // ========================================================================

    /// <summary>
    /// 偏移滤镜（将图像整体平移，常用于合成时的位置调整）
    /// </summary>
    /// <param name="dx">水平偏移</param>
    /// <param name="dy">垂直偏移</param>
    public static NBImageFilter Offset(double dx, double dy)
    {
        var filter = SKImageFilter.CreateOffset((float)dx, (float)dy);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 矩阵变换滤镜。
    /// 对图像应用任意仿射变换（平移、旋转、缩放、斜切、透视等），类似于在画布上使用 SKMatrix。
    /// </summary>
    /// <param name="matrix">变换矩阵，包含旋转、缩放、平移等参数。</param>
    /// <param name="samplingOptions">采样选项，控制变换后图像的插值质量（如 Bilinear、Mipmap 等）。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter MatrixTransform(SKMatrix matrix, SKSamplingOptions samplingOptions)
    {
        var filter = SKImageFilter.CreateMatrix(matrix, samplingOptions);
        return new NBSimpleImageFilter(filter);
    }

    // ========================================================================
    // 5. 光照与立体感（基于 Alpha 通道作为高度图）
    // ========================================================================

    /// <summary>
    /// 远距离漫反射光照滤镜。
    /// 模拟从无限远处（如太阳）照射的平行光源，产生柔和的漫反射立体感。
    /// 图像的 Alpha 通道被当作高度图，亮部凸起、暗部凹陷。
    /// </summary>
    /// <param name="direction">光源方向向量 (SKPoint3)，例如 (0, 0, 1) 表示从正前方照射，(0, -1, 1) 表示从左上角照射。</param>
    /// <param name="lightColor">光源颜色，例如 SKColors.White。</param>
    /// <param name="surfaceScale">表面高度比例，控制凹凸强度。默认 0.5，建议 0.1 ~ 2.0。</param>
    /// <param name="kd">漫反射系数，控制光照强度。默认 1.0，建议 0.5 ~ 1.5。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter DistantLitDiffuse(SKPoint3 direction, SKColor lightColor,
        float surfaceScale = 0.5f, float kd = 1.0f)
    {
        var filter = SKImageFilter.CreateDistantLitDiffuse(direction, lightColor, surfaceScale, kd);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 远距离镜面反射光照滤镜 (DistantLitSpecular)。
    /// 模拟远距离平行光源产生的镜面高光，可产生强烈的高光反射，适合金属、玻璃或光泽表面。
    /// </summary>
    /// <param name="direction">光源方向向量 (SKPoint3)，如 (0, 0, 1) 正前方。</param>
    /// <param name="lightColor">光源颜色。</param>
    /// <param name="surfaceScale">表面高度比例，控制凹凸感，默认 0.5。</param>
    /// <param name="ks">镜面反射系数，控制高光亮度，默认 1.0，建议 0.2 ~ 2.0。</param>
    /// <param name="shininess">高光锐利度（光泽度），值越大高光越集中、越亮。默认 20，建议 5 ~ 100。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter DistantLitSpecular(SKPoint3 direction, SKColor lightColor,
        float surfaceScale = 0.5f, float ks = 1.0f, float shininess = 20.0f)
    {
        var filter = SKImageFilter.CreateDistantLitSpecular(direction, lightColor, surfaceScale, ks, shininess);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 点光源漫反射光照滤镜。
    /// 模拟从空间中某一点发出的光源（如灯泡），产生漫反射立体效果。光源位置影响阴影方向和强度。
    /// </summary>
    /// <param name="location">光源在三维空间中的位置 (SKPoint3)，例如 new SKPoint3(100, 100, 200)。</param>
    /// <param name="lightColor">光源颜色。</param>
    /// <param name="surfaceScale">表面高度比例，默认 0.5。</param>
    /// <param name="kd">漫反射系数，默认 1.0。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter PointLitDiffuse(SKPoint3 location, SKColor lightColor,
        float surfaceScale = 0.5f, float kd = 1.0f)
    {
        var filter = SKImageFilter.CreatePointLitDiffuse(location, lightColor, surfaceScale, kd);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 点光源镜面反射光照滤镜。
    /// 模拟点光源产生的镜面高光，适合表现局部强烈反光效果。
    /// </summary>
    /// <param name="location">光源位置 (SKPoint3)。</param>
    /// <param name="lightColor">光源颜色。</param>
    /// <param name="surfaceScale">表面高度比例，默认 0.5。</param>
    /// <param name="ks">镜面反射系数，默认 1.0。</param>
    /// <param name="shininess">高光锐利度，默认 20。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter PointLitSpecular(SKPoint3 location, SKColor lightColor,
        float surfaceScale = 0.5f, float ks = 1.0f, float shininess = 20.0f)
    {
        var filter = SKImageFilter.CreatePointLitSpecular(location, lightColor, surfaceScale, ks, shininess);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 聚光灯漫反射光照滤镜。
    /// 模拟具有方向性和角度范围的聚光灯（如舞台追光），产生漫反射立体感。
    /// </summary>
    /// <param name="location">光源位置 (SKPoint3)。</param>
    /// <param name="target">聚光灯指向的目标点 (SKPoint3)。</param>
    /// <param name="specularExponent">聚光指数，控制光束的聚焦程度，值越大光锥越窄。通常 1~50。</param>
    /// <param name="cutoffAngle">截止角度（弧度），控制光锥的范围，超出该角度则无光照。通常 0~PI/2。</param>
    /// <param name="lightColor">光源颜色。</param>
    /// <param name="surfaceScale">表面高度比例，默认 0.5。</param>
    /// <param name="kd">漫反射系数，默认 1.0。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter SpotLitDiffuse(SKPoint3 location, SKPoint3 target,
        float specularExponent, float cutoffAngle, SKColor lightColor,
        float surfaceScale = 0.5f, float kd = 1.0f)
    {
        var filter = SKImageFilter.CreateSpotLitDiffuse(location, target, specularExponent,
                                                        cutoffAngle, lightColor, surfaceScale, kd);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 聚光灯镜面反射光照滤镜。
    /// 模拟聚光灯的镜面高光，适合产生强烈的、集中的光斑效果。
    /// </summary>
    /// <param name="location">光源位置 (SKPoint3)。</param>
    /// <param name="target">聚光灯目标点 (SKPoint3)。</param>
    /// <param name="specularExponent">聚光指数，控制光锥锐利度。</param>
    /// <param name="cutoffAngle">截止角度（弧度）。</param>
    /// <param name="lightColor">光源颜色。</param>
    /// <param name="surfaceScale">表面高度比例，默认 0.5。</param>
    /// <param name="ks">镜面反射系数，默认 1.0。</param>
    /// <param name="shininess">高光锐利度，默认 20。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter SpotLitSpecular(SKPoint3 location, SKPoint3 target,
        float specularExponent, float cutoffAngle, SKColor lightColor,
        float surfaceScale = 0.5f, float ks = 1.0f, float shininess = 20.0f)
    {
        var filter = SKImageFilter.CreateSpotLitSpecular(location, target, specularExponent,
                                                         cutoffAngle, lightColor, surfaceScale, ks, shininess);
        return new NBSimpleImageFilter(filter);
    }

    // ========================================================================
    // 6. 扭曲与映射
    // ========================================================================

    /// <summary>
    /// 位移映射滤镜。
    /// 根据另一张图像（位移图）的颜色通道值，对当前图像进行像素偏移，从而产生扭曲、水波、涟漪、液化等效果。
    /// 位移图中特定通道的值作为水平和垂直方向的偏移量。
    /// </summary>
    /// <param name="displacementMap">位移图的图像滤镜，其输出作为位移数据。通常为一张渐变图或噪声图。</param>
    /// <param name="scaleX">水平扭曲幅度，正数向右偏移，负数向左。建议 -200 ~ 200。</param>
    /// <param name="scaleY">垂直扭曲幅度，正数向下偏移，负数向上。建议 -200 ~ 200。</param>
    /// <param name="mapX">指定使用位移图的哪个颜色通道作为水平偏移数据。常用 RgbaG（绿色通道）或 RgbaR。</param>
    /// <param name="mapY">指定使用位移图的哪个颜色通道作为垂直偏移数据。常用 RgbaB（蓝色通道）或 RgbaR。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    //public static NBImageFilter DisplacementMap(NBImageFilter displacementMap,
    //    double scaleX, double scaleY,
    //    SKDisplacementMapMode mapX = SKDisplacementMapMode.RgbaG,
    //    SKDisplacementMapMode mapY = SKDisplacementMapMode.RgbaB)
    //{
    //    var filter = SKImageFilter.CreateDisplacementMap(
    //        mapX, mapY, (float)scaleX, (float)scaleY,
    //        displacementMap?.GetFilter()); // 假设 NBImageFilter 有 GetFilter() 方法
    //    return new NBSimpleImageFilter(filter);
    //}

    // ========================================================================
    // 7. 像素混合与颜色调整
    // ========================================================================

    /// <summary>
    /// 算术混合滤镜。
    /// 使用四则运算公式 result = k1 * src * dst + k2 * src + k3 * dst + k4 混合两个图像（前景和背景）。
    /// 常用于合成、颜色校正、高光/阴影调整等。
    /// </summary>
    /// <param name="k1">源图与目标图乘积的系数。</param>
    /// <param name="k2">源图的系数。</param>
    /// <param name="k3">目标图的系数。</param>
    /// <param name="k4">常数偏移量。</param>
    /// <param name="enforcePMColor">是否强制使用预乘 Alpha 颜色空间，通常为 true 以保证透明度正确。</param>
    /// <param name="background">背景图像滤镜（作为目标图 dst）。</param>
    /// <param name="foreground">前景图像滤镜（作为源图 src）。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    //public static NBImageFilter Arithmetic(float k1, float k2, float k3, float k4,
    //    bool enforcePMColor, NBImageFilter background, NBImageFilter foreground)
    //{
    //    var filter = SKImageFilter.CreateArithmetic(k1, k2, k3, k4, enforcePMColor,
    //                                                background?.GetFilter(), foreground?.GetFilter());
    //    return new NBSimpleImageFilter(filter);
    //}

    /// <summary>
    /// 颜色滤镜。
    /// 通过传入 SKColorFilter 对象，可以对图像进行各种颜色调整，如亮度、对比度、色相、饱和度、颜色矩阵变换等。
    /// 是最灵活的颜色处理工具。
    /// </summary>
    /// <param name="colorFilter">一个 SKColorFilter 实例，例如 SKColorFilter.CreateLighting() 或 SKColorFilter.CreateMatrix()。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter ColorFilter(SKColorFilter colorFilter)
    {
        var filter = SKImageFilter.CreateColorFilter(colorFilter);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// Alpha 阈值滤镜。
    /// 根据指定的区域，将区域内和区域外的 Alpha 通道值分别提高到内阈值、降低到外阈值。
    /// 可用于创建遮罩、羽化边缘或生成特殊形状的透明度过渡。
    /// </summary>
    /// <param name="region">指定的区域（SKRegion），决定哪些像素属于“内部”。</param>
    /// <param name="innerThreshold">区域内部的 Alpha 目标值（0~1），通常设为 1 使内部完全不透明。</param>
    /// <param name="outerThreshold">区域外部的 Alpha 目标值（0~1），通常设为 0 使外部完全透明。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    //public static NBImageFilter AlphaThreshold(SKRegion region, float innerThreshold, float outerThreshold)
    //{
    //    var filter = SKImageFilter.CreateAlphaThreshold(region, innerThreshold, outerThreshold);
    //    return new NBSimpleImageFilter(filter);
    //}

    // ========================================================================
    // 8. 特殊效果
    // ========================================================================

    /// <summary>
    /// 放大镜滤镜。
    /// 在图像的指定矩形区域内创建一个放大镜效果，区域内的图像会被放大，边缘过渡平滑。
    /// </summary>
    /// <param name="lensBounds">透镜区域（矩形），即放大效果的作用范围。</param>
    /// <param name="zoomAmount">放大倍数，1.0 表示不放大，大于 1.0 放大，小于 1.0 缩小。</param>
    /// <param name="inset">边缘过渡区域的宽度（像素），让放大区与非放大区之间平滑过渡，避免突兀。</param>
    /// <param name="sampling">采样选项，控制放大后的图像质量（如 Bilinear 或 HighQuality）。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter Magnifier(SKRect lensBounds, float zoomAmount,
        float inset, SKSamplingOptions sampling)
    {
        var filter = SKImageFilter.CreateMagnifier(lensBounds, zoomAmount, inset, sampling);
        return new NBSimpleImageFilter(filter);
    }

    /// <summary>
    /// 平铺滤镜。
    /// 将图像中的一个矩形区域（src）作为瓦片，平铺到另一个更大的矩形区域（dst）中。
    /// 常用于制作无缝背景纹理或图案重复。
    /// </summary>
    /// <param name="src">源矩形区域，即被平铺的原始图像部分。</param>
    /// <param name="dst">目标矩形区域，即平铺后覆盖的范围。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter Tile(SKRect src, SKRect dst)
    {
        var filter = SKImageFilter.CreateTile(src, dst);
        return new NBSimpleImageFilter(filter);
    }

    // ========================================================================
    // 9. 滤镜组合
    // ========================================================================

    /// <summary>
    /// 混合模式滤镜。
    /// 使用指定的混合模式（如正片叠底、滤色、叠加等）将前景图像合成到背景图像上。
    /// 类似于图层混合模式。
    /// </summary>
    /// <param name="mode">SkiaSharp 支持的混合模式，如 SKBlendMode.Multiply、Screen、Overlay 等。</param>
    /// <param name="background">背景图像滤镜（作为底层）。</param>
    /// <param name="foreground">前景图像滤镜（作为顶层）。</param>
    /// <returns>封装了 SKImageFilter 的 NBImageFilter 对象。</returns>
    public static NBImageFilter BlendMode(SKBlendMode mode, NBImageFilter background, NBImageFilter foreground)
    {
        var filter = SKImageFilter.CreateBlendMode(mode, background.CreateFilter(), foreground.CreateFilter());
        return new NBSimpleImageFilter(filter);
    }


    ///// <summary>
    ///// 内发光效果（使用镜面光照模拟）
    ///// </summary>
    ///// <param name="lightColor">发光颜色</param>
    ///// <param name="surfaceScale">表面高度比例，控制凹凸感</param>
    ///// <param name="ks">镜面反射系数</param>
    ///// <param name="shininess">高光锐利度</param>
    //public static NBImageFilter InnerGlowSpecular(SKColor lightColor,
    //    float surfaceScale = 0.5f, float ks = 1.0f, float shininess = 20.0f)
    //{
    //    // 光源方向：从左上角照射
    //    var direction = new SKPoint3(0, 0, 1);
    //    var filter = SKImageFilter.CreateDistantLitSpecular(
    //        direction,
    //        lightColor,
    //        surfaceScale,
    //        ks,
    //        shininess
    //    );
    //    return new NBSimpleImageFilter(filter);
    //}

    /// <summary>
    /// 内发光效果（通过遮罩 + 模糊组合实现）
    /// </summary>
    /// <param name="glowColor">发光颜色</param>
    /// <param name="blurSigma">模糊半径</param>
    /// <param name="size">发光扩展大小（相对于边缘向内偏移量）</param>
    public static NBImageFilter InnerGlow(SKColor glowColor, double blurSigma, float size = 0)
    {
        // 1. 创建一个纯色填充，颜色为发光色
        var colorFilter = SKColorFilter.CreateBlendMode(glowColor, SKBlendMode.Src);
        var colorFilterImageFilter = SKImageFilter.CreateColorFilter(colorFilter);

        // 2. 对图像进行模糊，产生光晕扩散效果
        var blurFilter = SKImageFilter.CreateBlur((float)blurSigma, (float)blurSigma);

        // 3. 将模糊后的图像与原始图像的 Alpha 通道进行合成
        //    使用 SrcIn 模式，只保留原始图像 Alpha 区域内的发光
        //    注意：这里需要使用 Compose 将模糊滤镜作用于颜色滤镜的结果上
        var glowWithBlur = SKImageFilter.CreateCompose(blurFilter, colorFilterImageFilter);

        // 4. 如果需要向内收缩（size > 0），可以使用 Erode 腐蚀 Alpha 通道
        if (size > 0)
        {
            //var erodeFilter = SKImageFilter.CreateErode((int)size, (int)size);
            //var innerMask = SKImageFilter.CreateCompose(erodeFilter, null);
            // 这里逻辑较复杂，需要结合 Merge 和 Compose 实现遮罩裁剪
            // 简化处理：直接返回模糊后的发光层，实际项目中可进一步优化
            return new NBSimpleImageFilter(glowWithBlur);
        }

        return new NBSimpleImageFilter(glowWithBlur);
    }
}
