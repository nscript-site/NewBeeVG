using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// 提供各种帧遮罩（Frame Mask）的生成方法。
/// 所有方法均返回 <see cref="NBFrameMask"/>，可用于视频/图形处理中的遮罩或纹理叠加。
/// 本类是对 <see cref="Textures"/> 中位图生成方法的封装，使其适配 <see cref="NBFrameMask"/> 的延迟计算模型。
/// </summary>
public static class FrameMasks
{
    /// <summary>
    /// 从现有的 <see cref="SKBitmap"/> 创建帧遮罩。
    /// </summary>
    public static NBFrameMask FromTexture(SKBitmap bitmap)
    {
        return new NBBitmapFrameMask((ctx, rect) => bitmap);
    }

    /// <summary>
    /// 从委托创建帧遮罩，委托参数为上下文和矩形区域，返回位图。
    /// </summary>
    public static NBFrameMask FromTexture(Func<NBDrawContext, SKRect, SKBitmap> func)
    {
        return new NBBitmapFrameMask(func);
    }

    /// <summary>
    /// 从委托创建帧遮罩，委托参数仅为矩形区域，返回位图。
    /// </summary>
    public static NBFrameMask FromTexture(Func<SKRect, SKBitmap> func)
    {
        return new NBBitmapFrameMask((ctx, rect) => func(rect));
    }

    /// <summary>
    /// 生成 Perlin 噪声帧遮罩。
    /// </summary>
    /// <param name="useTurbulence">true 使用湍流噪声，false 使用分形噪声（更柔和）</param>
    /// <param name="baseFrequencyX">X 方向频率（0~1），值越大纹理越细碎</param>
    /// <param name="baseFrequencyY">Y 方向频率（0~1）</param>
    /// <param name="numOctaves">八度音阶数（>=1），值越大细节越丰富</param>
    /// <param name="seed">随机种子，相同种子生成相同的噪声图</param>
    public static NBFrameMask Noise(
        bool useTurbulence = true,
        float baseFrequencyX = 0.05f,
        float baseFrequencyY = 0.05f,
        int numOctaves = 4,
        float seed = 0)
    {
        Func<NBDrawContext, SKRect, SKBitmap> func = (ctx, rect) =>
        {
            int width = (int)rect.Width;
            int height = (int)rect.Height;
            return Textures.GenerateNoiseBitmap(width, height, useTurbulence, baseFrequencyX, baseFrequencyY, numOctaves, seed);
        };
        return new NBBitmapFrameMask(func);
    }

    // ========================= 新增方法（对应 Textures 中所有 Generate 方法） =========================

    /// <summary>
    /// 生成烟雾效果帧遮罩（带透明通道）。
    /// 使用分形 Perlin 噪声模拟烟雾，背景透明，烟雾为半透明白色。
    /// </summary>
    /// <param name="color">烟雾颜色，默认 null 为白色</param>
    /// <param name="baseFrequencyX">X 方向频率（0~1），值越大烟雾越细碎</param>
    /// <param name="baseFrequencyY">Y 方向频率（0~1）</param>
    /// <param name="numOctaves">八度音阶数（>=1），值越大细节越丰富</param>
    /// <param name="seed">随机种子</param>
    /// <param name="alpha">烟雾的不透明度（0~255），默认 128</param>
    public static NBFrameMask Smoke(
        SKColor? color = null,
        float baseFrequencyX = 0.05f,
        float baseFrequencyY = 0.05f,
        int numOctaves = 4,
        float seed = 0,
        byte alpha = 128)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateSmokeBitmap(w, h, color, baseFrequencyX, baseFrequencyY, numOctaves, seed, alpha);
        });
    }

    /// <summary>
    /// 生成胶片划痕纹理帧遮罩（随机线条）。
    /// 可用于模拟老旧电影或破损胶片的视觉效果。
    /// </summary>
    /// <param name="count">划痕数量</param>
    /// <param name="maxLength">最大划痕长度（像素）</param>
    /// <param name="lineWidth">线条粗细（像素）</param>
    /// <param name="color">划痕颜色（默认白色）</param>
    /// <param name="alpha">透明度（0~255）</param>
    /// <param name="seed">随机种子</param>
    public static NBFrameMask Scratches(
        int count = 50,
        int maxLength = 200,
        float lineWidth = 2f,
        SKColor? color = null,
        byte alpha = 180,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateScratchesBitmap(w, h, count, maxLength, lineWidth, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成雨滴纹理帧遮罩（随机椭圆点，模拟雨滴下落轨迹）。
    /// </summary>
    /// <param name="count">雨滴数量</param>
    /// <param name="minLength">最小长度（像素）</param>
    /// <param name="maxLength">最大长度（像素）</param>
    /// <param name="lineWidth">线条粗细（像素）</param>
    /// <param name="color">雨滴颜色（默认淡蓝/白）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static NBFrameMask Raindrops(
        int count = 300,
        float minLength = 10f,
        float maxLength = 40f,
        float lineWidth = 1.5f,
        SKColor? color = null,
        byte alpha = 180,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateRaindropsBitmap(w, h, count, minLength, maxLength, lineWidth, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成雪花纹理帧遮罩（随机小圆点或小星形）。
    /// </summary>
    /// <param name="count">雪花数量</param>
    /// <param name="minRadius">最小半径</param>
    /// <param name="maxRadius">最大半径</param>
    /// <param name="color">雪花颜色（默认白色）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static NBFrameMask Snowflakes(
        int count = 200,
        float minRadius = 1f,
        float maxRadius = 5f,
        SKColor? color = null,
        byte alpha = 200,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateSnowflakesBitmap(w, h, count, minRadius, maxRadius, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成灰尘/粒子纹理帧遮罩（微小随机点）。
    /// </summary>
    /// <param name="count">粒子数量</param>
    /// <param name="maxRadius">最大半径</param>
    /// <param name="color">粒子颜色（默认白色）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static NBFrameMask Dust(
        int count = 1000,
        float maxRadius = 2f,
        SKColor? color = null,
        byte alpha = 100,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateDustBitmap(w, h, count, maxRadius, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成镜头光晕纹理帧遮罩（径向渐变光晕）。
    /// </summary>
    /// <param name="centerX">中心点X（0~1，相对位置）</param>
    /// <param name="centerY">中心点Y</param>
    /// <param name="maxRadius">最大半径（像素）</param>
    /// <param name="color">光晕颜色（默认白色）</param>
    /// <param name="alpha">透明度</param>
    public static NBFrameMask LensFlare(
        float centerX = 0.5f,
        float centerY = 0.5f,
        float maxRadius = 200f,
        SKColor? color = null,
        byte alpha = 150)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateLensFlareBitmap(w, h, centerX, centerY, maxRadius, color, alpha);
        });
    }

    /// <summary>
    /// 生成扫描线/网格纹理帧遮罩（水平或垂直条纹）。
    /// </summary>
    /// <param name="lineSpacing">线条间距（像素）</param>
    /// <param name="lineThickness">线条粗细（像素）</param>
    /// <param name="horizontal">true 水平扫描线，false 垂直扫描线</param>
    /// <param name="color">线条颜色（默认黑色）</param>
    /// <param name="alpha">透明度</param>
    public static NBFrameMask Scanlines(
        int lineSpacing = 4,
        int lineThickness = 2,
        bool horizontal = true,
        SKColor? color = null,
        byte alpha = 100)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateScanlinesBitmap(w, h, lineSpacing, lineThickness, horizontal, color, alpha);
        });
    }

    /// <summary>
    /// 生成胶片颗粒噪点纹理帧遮罩（随机像素点）。
    /// </summary>
    /// <param name="density">噪点密度（0~1），值越大点越多</param>
    /// <param name="color">噪点颜色（默认灰色）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static NBFrameMask Grain(
        float density = 0.1f,
        SKColor? color = null,
        byte alpha = 100,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateGrainBitmap(w, h, density, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成棋盘格纹理帧遮罩（使用平铺位图着色器）。
    /// </summary>
    /// <param name="cellSize">格子大小（像素）</param>
    /// <param name="color1">第一种颜色（默认黑色）</param>
    /// <param name="color2">第二种颜色（默认白色）</param>
    /// <param name="alpha">不透明度</param>
    public static NBFrameMask Checkerboard(
        int cellSize = 40,
        SKColor? color1 = null,
        SKColor? color2 = null,
        byte alpha = 255)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateCheckerboardBitmap(w, h, cellSize, color1, color2, alpha);
        });
    }

    /// <summary>
    /// 生成斜条纹纹理帧遮罩（对角线方向）。
    /// </summary>
    /// <param name="stripeWidth">条纹宽度（像素）</param>
    /// <param name="gapWidth">间隙宽度（像素）</param>
    /// <param name="angle">倾斜角度（度），默认 45°</param>
    /// <param name="color">条纹颜色（默认黑色）</param>
    /// <param name="gapColor">间隙颜色（默认透明）</param>
    /// <param name="alpha">条纹透明度</param>
    /// <param name="seed">随机种子（未使用）</param>
    public static NBFrameMask DiagonalStripes(
        int stripeWidth = 30,
        int gapWidth = 20,
        float angle = 45f,
        SKColor? color = null,
        SKColor? gapColor = null,
        byte alpha = 255,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateDiagonalStripesBitmap(w, h, stripeWidth, gapWidth, angle, color, gapColor, alpha, seed);
        });
    }

    /// <summary>
    /// 生成暗角纹理帧遮罩（Vignette）。
    /// </summary>
    /// <param name="color">暗角颜色（默认黑色）</param>
    /// <param name="centerX">中心点X（0~1）</param>
    /// <param name="centerY">中心点Y（0~1）</param>
    /// <param name="radius">暗角半径（像素），默认取宽高的最小值</param>
    /// <param name="alpha">中心透明度（0~255），0 完全透明，255 不透明</param>
    /// <param name="edgeAlpha">边缘透明度（0~255）</param>
    public static NBFrameMask Vignette(
        SKColor? color = null,
        float centerX = 0.5f,
        float centerY = 0.5f,
        float? radius = null,
        byte alpha = 0,
        byte edgeAlpha = 200)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateVignetteBitmap(w, h, color, centerX, centerY, radius, alpha, edgeAlpha);
        });
    }

    /// <summary>
    /// 生成云彩纹理帧遮罩（使用分形噪声并映射为彩色云朵）。
    /// </summary>
    /// <param name="cloudColor">云朵颜色（默认白色）</param>
    /// <param name="baseFrequencyX">X 频率</param>
    /// <param name="baseFrequencyY">Y 频率</param>
    /// <param name="numOctaves">八度</param>
    /// <param name="seed">随机种子</param>
    /// <param name="maxAlpha">最大不透明度（0~255）</param>
    public static NBFrameMask Cloud(
        SKColor? cloudColor = null,
        float baseFrequencyX = 0.02f,
        float baseFrequencyY = 0.02f,
        int numOctaves = 5,
        float seed = 0,
        byte maxAlpha = 200)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateCloudBitmap(w, h, cloudColor, baseFrequencyX, baseFrequencyY, numOctaves, seed, maxAlpha);
        });
    }

    /// <summary>
    /// 生成木纹纹理帧遮罩（模拟木材的年轮和纹理）。
    /// </summary>
    /// <param name="woodColor">木材主色（默认棕色）</param>
    /// <param name="ringColor">年轮颜色（默认深棕色）</param>
    /// <param name="frequency">年轮密度（值越大年轮越密集）</param>
    /// <param name="seed">随机种子</param>
    /// <param name="alpha">不透明度（默认255）</param>
    public static NBFrameMask Wood(
        SKColor? woodColor = null,
        SKColor? ringColor = null,
        float frequency = 0.03f,
        float seed = 0,
        byte alpha = 255)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateWoodBitmap(w, h, woodColor, ringColor, frequency, seed, alpha);
        });
    }

    /// <summary>
    /// 生成点阵纹理帧遮罩（规则排列的圆点）。
    /// </summary>
    /// <param name="dotRadius">圆点半径（像素）</param>
    /// <param name="spacingX">水平间距</param>
    /// <param name="spacingY">垂直间距</param>
    /// <param name="color">圆点颜色（默认黑色）</param>
    /// <param name="alpha">不透明度</param>
    /// <param name="offsetX">水平偏移量</param>
    /// <param name="offsetY">垂直偏移量</param>
    public static NBFrameMask DotMatrix(
        float dotRadius = 4f,
        float spacingX = 20f,
        float spacingY = 20f,
        SKColor? color = null,
        byte alpha = 255,
        float offsetX = 0f,
        float offsetY = 0f)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateDotMatrixBitmap(w, h, dotRadius, spacingX, spacingY, color, alpha, offsetX, offsetY);
        });
    }

    /// <summary>
    /// 生成线性渐变纹理帧遮罩（从起始颜色到结束颜色过渡）。
    /// </summary>
    /// <param name="startColor">起始颜色</param>
    /// <param name="endColor">结束颜色</param>
    /// <param name="startX">起始点X（0~1）</param>
    /// <param name="startY">起始点Y（0~1）</param>
    /// <param name="endX">结束点X（0~1）</param>
    /// <param name="endY">结束点Y（0~1）</param>
    /// <param name="tileMode">平铺模式</param>
    public static NBFrameMask LinearGradient(
        SKColor startColor,
        SKColor endColor,
        float startX = 0f,
        float startY = 0f,
        float endX = 1f,
        float endY = 1f,
        SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateLinearGradientBitmap(w, h, startColor, endColor, startX, startY, endX, endY, tileMode);
        });
    }

    /// <summary>
    /// 生成胶片颗粒纹理帧遮罩（通过添加细微噪点模拟老电影质感）。
    /// </summary>
    public static NBFrameMask FilmGrain(
        float density = 0.15f,
        SKColor? color = null,
        byte alpha = 80,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateFilmGrainBitmap(w, h, density, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成胶片灼烧/漏光纹理帧遮罩（模拟胶片意外曝光产生的暖色光晕）。
    /// </summary>
    public static NBFrameMask FilmBurn(
        SKColor? primaryColor = null,
        SKColor? secondaryColor = null,
        float seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateFilmBurnBitmap(w, h, primaryColor, secondaryColor, seed);
        });
    }

    /// <summary>
    /// 生成 VHS 磁带纹理帧遮罩（模拟老式录像带的播放效果，包含时间码、噪点和跟踪条）。
    /// </summary>
    public static NBFrameMask VHSTexture(
        SKColor? color = null,
        byte alpha = 100,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateVHSTextureBitmap(w, h, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成灰尘与污渍纹理帧遮罩（随机散布的微小斑点）。
    /// </summary>
    public static NBFrameMask DustAndDirt(
        int count = 300,
        float maxRadius = 3f,
        SKColor? color = null,
        byte alpha = 80,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateDustAndDirtBitmap(w, h, count, maxRadius, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成故障艺术纹理帧遮罩（模拟数字信号的画面撕裂、色彩错位和像素块）。
    /// </summary>
    public static NBFrameMask Glitch(
        SKColor? color = null,
        byte alpha = 200,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateGlitchBitmap(w, h, color, alpha, seed);
        });
    }

    /// <summary>
    /// 生成半色调纹理帧遮罩（通过不同大小的点阵来表现明暗和颜色）。
    /// </summary>
    public static NBFrameMask Halftone(
        SKColor? color = null,
        float dotSize = 4f,
        float spacing = 12f,
        byte alpha = 200,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateHalftoneBitmap(w, h, color, dotSize, spacing, alpha, seed);
        });
    }

    /// <summary>
    /// 生成像素化纹理帧遮罩（马赛克效果）。
    /// </summary>
    public static NBFrameMask Pixelate(
        int blockSize = 20,
        SKColor? color1 = null,
        SKColor? color2 = null,
        int seed = 0)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GeneratePixelateBitmap(w, h, blockSize, color1, color2, seed);
        });
    }

    /// <summary>
    /// 生成动态转场纹理帧遮罩（墨迹/水流效果）。
    /// </summary>
    public static NBFrameMask DynamicTransition(
        SKColor? color = null,
        float baseFrequencyX = 0.02f,
        float baseFrequencyY = 0.02f,
        int numOctaves = 6,
        float seed = 0,
        byte alpha = 200)
    {
        return new NBBitmapFrameMask((ctx, rect) =>
        {
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            return Textures.GenerateDynamicTransitionBitmap(w, h, color, baseFrequencyX, baseFrequencyY, numOctaves, seed, alpha);
        });
    }
}

