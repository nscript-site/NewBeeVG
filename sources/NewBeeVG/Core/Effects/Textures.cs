using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// NBImageGenerator 提供了一系列静态方法，用于生成各种噪声纹理、烟雾、划痕、雨滴、雪花、灰尘、光晕等效果的位图。
/// 本方法由 AI 生成，使用 SkiaSharp 进行图形绘制，适用于游戏、图形应用或视觉效果制作。
/// </summary>
public static class Textures
{
    /// <summary>
    /// 生成一张 Perlin 噪声位图并返回 SKBitmap 对象。
    /// </summary>
    /// <param name="width">位图宽度（像素）</param>
    /// <param name="height">位图高度（像素）</param>
    /// <param name="useTurbulence">true 使用湍流噪声，false 使用分形噪声（更柔和）</param>
    /// <param name="baseFrequencyX">X 方向频率（0~1），值越大纹理越细碎</param>
    /// <param name="baseFrequencyY">Y 方向频率（0~1）</param>
    /// <param name="numOctaves">八度音阶数（>=1），值越大细节越丰富</param>
    /// <param name="seed">随机种子，相同种子生成相同的噪声图</param>
    /// <returns>包含噪声纹理的 SKBitmap 对象</returns>
    public static SKBitmap GenerateNoiseBitmap(
        int width,
        int height,
        bool useTurbulence = true,
        float baseFrequencyX = 0.05f,
        float baseFrequencyY = 0.05f,
        int numOctaves = 4,
        float seed = 0)
    {
        // 1. 创建噪声着色器
        SKShader noiseShader;
        if (useTurbulence)
            noiseShader = SKShader.CreatePerlinNoiseTurbulence(
                baseFrequencyX, baseFrequencyY, numOctaves, seed);
        else
            noiseShader = SKShader.CreatePerlinNoiseFractalNoise(
                baseFrequencyX, baseFrequencyY, numOctaves, seed);

        // 2. 创建离屏画布
        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;

        // 3. 用噪声着色器填充整个画布
        using var paint = new SKPaint
        {
            Shader = noiseShader
        };
        canvas.DrawRect(0, 0, width, height, paint);

        // 4. 从画布中提取 SKBitmap
        using var image = surface.Snapshot();
        var bitmap = SKBitmap.FromImage(image);  // 复制一份独立的位图
        return bitmap;
    }

    /// <summary>
    /// 生成一张烟雾效果位图（带透明通道）。
    /// 使用分形 Perlin 噪声模拟烟雾，背景透明，烟雾为半透明白色。
    /// </summary>
    /// <param name="width">位图宽度（像素）</param>
    /// <param name="height">位图高度（像素）</param>
    /// <param name="color">烟雾颜色，默认 null 为白色</param>
    /// <param name="baseFrequencyX">X 方向频率（0~1），值越大烟雾越细碎</param>
    /// <param name="baseFrequencyY">Y 方向频率（0~1）</param>
    /// <param name="numOctaves">八度音阶数（>=1），值越大细节越丰富</param>
    /// <param name="seed">随机种子，相同种子生成相同的烟雾图</param>
    /// <param name="alpha">烟雾的不透明度（0~255），默认 128 为半透明</param>
    /// <returns>包含烟雾纹理的 SKBitmap 对象（RGBA 格式，带透明通道）</returns>
    public static SKBitmap GenerateSmokeBitmap(
        int width,
        int height,
        SKColor? color = null,
        float baseFrequencyX = 0.05f,
        float baseFrequencyY = 0.05f,
        int numOctaves = 4,
        float seed = 0,
        byte alpha = 128)
    {
        // 1. 创建分形 Perlin 噪声着色器（烟雾用分形更柔和）
        var noiseShader = SKShader.CreatePerlinNoiseFractalNoise(
            baseFrequencyX,
            baseFrequencyY,
            numOctaves,
            seed
        );

        // 2. 创建一个透明背景的画布
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;

        // 先清除为完全透明
        canvas.Clear(SKColors.Transparent);

        // 3. 用带有透明度颜色的画笔绘制噪声
        var c = color ?? new SKColor(255, 255, 255, alpha); // 使用指定颜色或默认白色 + 指定透明度
        c = new SKColor(c.Red, c.Green, c.Blue, alpha); // 强制使用指定 alpha
        using var paint = new SKPaint
        {
            Shader = noiseShader,
            Color = c, // 使用指定颜色 + 指定透明度
            BlendMode = SKBlendMode.SrcOver           // 常规混合
        };

        // 绘制全屏矩形，生成烟雾
        canvas.DrawRect(0, 0, width, height, paint);

        // 4. 从画布中提取位图
        using var image = surface.Snapshot();
        var bitmap = SKBitmap.FromImage(image); // 复制独立副本

        return bitmap;
    }

    /// <summary>
    /// 生成胶片划痕纹理（随机线条）。
    /// 可用于模拟老旧电影或破损胶片的视觉效果。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="count">划痕数量</param>
    /// <param name="maxLength">最大划痕长度（像素）</param>
    /// <param name="lineWidth">线条粗细（像素）</param>
    /// <param name="color">划痕颜色（默认白色）</param>
    /// <param name="alpha">透明度（0~255）</param>
    /// <param name="seed">随机种子</param>
    public static SKBitmap GenerateScratchesBitmap(
        int width,
        int height,
        int count = 50,
        int maxLength = 200,
        float lineWidth = 2f,
        SKColor? color = null,
        byte alpha = 180,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var paint = new SKPaint
        {
            Color = color ?? SKColors.White,
            StrokeWidth = lineWidth,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        // 设置透明度
        var c = paint.Color;
        paint.Color = new SKColor(c.Red, c.Green, c.Blue, alpha);

        for (int i = 0; i < count; i++)
        {
            // 随机起点
            float x1 = (float)(rand.NextDouble() * width);
            float y1 = (float)(rand.NextDouble() * height);
            // 随机方向 (角度)
            float angle = (float)(rand.NextDouble() * 2 * Math.PI);
            float length = (float)(rand.NextDouble() * maxLength);
            float x2 = x1 + length * (float)Math.Cos(angle);
            float y2 = y1 + length * (float)Math.Sin(angle);
            // 裁剪到画布内（简化，直接绘制，超出部分会被裁剪）
            canvas.DrawLine(x1, y1, x2, y2, paint);
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成雨滴纹理（随机椭圆点，模拟雨滴下落轨迹）。
    /// 可用于叠加雨景效果。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="count">雨滴数量</param>
    /// <param name="minLength">最小长度（像素）</param>
    /// <param name="maxLength">最大长度（像素）</param>
    /// <param name="lineWidth">线条粗细（像素）</param>
    /// <param name="color">雨滴颜色（默认淡蓝/白）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static SKBitmap GenerateRaindropsBitmap(
        int width,
        int height,
        int count = 300,
        float minLength = 10f,
        float maxLength = 40f,
        float lineWidth = 1.5f,
        SKColor? color = null,
        byte alpha = 180,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var paint = new SKPaint
        {
            Color = color ?? new SKColor(200, 220, 255),
            StrokeWidth = lineWidth,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        var c = paint.Color;
        paint.Color = new SKColor(c.Red, c.Green, c.Blue, alpha);

        for (int i = 0; i < count; i++)
        {
            float x = (float)(rand.NextDouble() * width);
            float y = (float)(rand.NextDouble() * height);
            float length = (float)(rand.NextDouble() * (maxLength - minLength) + minLength);
            // 雨滴几乎垂直，略微倾斜（角度在 -15° ~ 15° 之间）
            float angle = (float)(rand.NextDouble() * 30 - 15) * (float)Math.PI / 180;
            float x2 = x + length * (float)Math.Sin(angle);
            float y2 = y + length * (float)Math.Cos(angle);
            canvas.DrawLine(x, y, x2, y2, paint);
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成雪花纹理（随机小圆点或小星形）。
    /// 适合制作飘雪效果。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="count">雪花数量</param>
    /// <param name="minRadius">最小半径</param>
    /// <param name="maxRadius">最大半径</param>
    /// <param name="color">雪花颜色（默认白色）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static SKBitmap GenerateSnowflakesBitmap(
        int width,
        int height,
        int count = 200,
        float minRadius = 1f,
        float maxRadius = 5f,
        SKColor? color = null,
        byte alpha = 200,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var paint = new SKPaint
        {
            Color = color ?? SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        var c = paint.Color;
        paint.Color = new SKColor(c.Red, c.Green, c.Blue, alpha);

        for (int i = 0; i < count; i++)
        {
            float x = (float)(rand.NextDouble() * width);
            float y = (float)(rand.NextDouble() * height);
            float radius = (float)(rand.NextDouble() * (maxRadius - minRadius) + minRadius);
            // 简单绘制圆形雪花，也可以绘制星形（复杂些可自定义）
            canvas.DrawCircle(x, y, radius, paint);
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成灰尘/粒子纹理（微小随机点）。
    /// 用于模拟空气中漂浮的尘埃或胶片颗粒。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="count">粒子数量</param>
    /// <param name="maxRadius">最大半径</param>
    /// <param name="color">粒子颜色（默认白色）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static SKBitmap GenerateDustBitmap(
        int width,
        int height,
        int count = 1000,
        float maxRadius = 2f,
        SKColor? color = null,
        byte alpha = 100,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var paint = new SKPaint
        {
            Color = color ?? SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        var c = paint.Color;
        paint.Color = new SKColor(c.Red, c.Green, c.Blue, alpha);

        for (int i = 0; i < count; i++)
        {
            float x = (float)(rand.NextDouble() * width);
            float y = (float)(rand.NextDouble() * height);
            float radius = (float)(rand.NextDouble() * maxRadius);
            canvas.DrawCircle(x, y, radius, paint);
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }


    /// <summary>
    /// 生成镜头光晕纹理（径向渐变光晕）。
    /// 用于模拟镜头耀斑或光芒。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="centerX">中心点X（0~1，相对位置）</param>
    /// <param name="centerY">中心点Y</param>
    /// <param name="maxRadius">最大半径（像素）</param>
    /// <param name="color">光晕颜色（默认白色）</param>
    /// <param name="alpha">透明度</param>
    public static SKBitmap GenerateLensFlareBitmap(
        int width,
        int height,
        float centerX = 0.5f,
        float centerY = 0.5f,
        float maxRadius = 200f,
        SKColor? color = null,
        byte alpha = 150)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var c = color ?? SKColors.White;
        // 创建一个径向渐变着色器，从中心彩色到边缘完全透明
        var shader = SKShader.CreateRadialGradient(
            new SKPoint(centerX * width, centerY * height),
            maxRadius,
            new SKColor[] { new SKColor(c.Red, c.Green, c.Blue, alpha), new SKColor(c.Red, c.Green, c.Blue, 0) },
            new float[] { 0, 1 },
            SKShaderTileMode.Clamp
        );

        using var paint = new SKPaint
        {
            Shader = shader,
            BlendMode = SKBlendMode.SrcOver
        };
        canvas.DrawRect(0, 0, width, height, paint);

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成扫描线/网格纹理（水平或垂直条纹）。
    /// 用于模拟老式电视或屏幕效果。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="lineSpacing">线条间距（像素）</param>
    /// <param name="lineThickness">线条粗细（像素）</param>
    /// <param name="horizontal">true 水平扫描线，false 垂直扫描线</param>
    /// <param name="color">线条颜色（默认黑色）</param>
    /// <param name="alpha">透明度</param>
    public static SKBitmap GenerateScanlinesBitmap(
        int width,
        int height,
        int lineSpacing = 4,
        int lineThickness = 2,
        bool horizontal = true,
        SKColor? color = null,
        byte alpha = 100)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var c = color ?? SKColors.Black;
        var paint = new SKPaint
        {
            Color = new SKColor(c.Red, c.Green, c.Blue, alpha),
            Style = SKPaintStyle.Fill
        };

        if (horizontal)
        {
            for (int y = 0; y < height; y += lineSpacing)
            {
                canvas.DrawRect(0, y, width, lineThickness, paint);
            }
        }
        else
        {
            for (int x = 0; x < width; x += lineSpacing)
            {
                canvas.DrawRect(x, 0, lineThickness, height, paint);
            }
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成胶片颗粒噪点纹理（随机像素点）。
    /// 与 Perlin 噪声不同，这是纯随机噪点。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="density">噪点密度（0~1），值越大点越多</param>
    /// <param name="color">噪点颜色（默认灰色）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="seed">随机种子</param>
    public static SKBitmap GenerateGrainBitmap(
        int width,
        int height,
        float density = 0.1f,
        SKColor? color = null,
        byte alpha = 100,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var c = color ?? new SKColor(128, 128, 128);
        // 直接操作像素可能更高效，但这里使用绘制圆点方式
        var paint = new SKPaint
        {
            Color = new SKColor(c.Red, c.Green, c.Blue, alpha),
            Style = SKPaintStyle.Fill
        };

        int totalPixels = width * height;
        int count = (int)(totalPixels * density);
        for (int i = 0; i < count; i++)
        {
            int x = rand.Next(width);
            int y = rand.Next(height);
            // 每个点只有1个像素，用DrawPoint
            canvas.DrawPoint(x, y, paint);
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }
}
