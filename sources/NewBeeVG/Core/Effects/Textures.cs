using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// Textures 提供了一系列静态方法，用于生成各种噪声纹理、烟雾、划痕、雨滴、雪花、灰尘、光晕等效果的位图。
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

    /// <summary>
    /// 生成棋盘格纹理（使用平铺位图着色器）。
    /// 适用于背景、网格辅助线或图案填充。
    /// </summary>
    public static SKBitmap GenerateCheckerboardBitmap(
        int width,
        int height,
        int cellSize = 40,
        SKColor? color1 = null,
        SKColor? color2 = null,
        byte alpha = 255)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var c1 = color1 ?? SKColors.Black;
        var c2 = color2 ?? SKColors.White;
        c1 = new SKColor(c1.Red, c1.Green, c1.Blue, alpha);
        c2 = new SKColor(c2.Red, c2.Green, c2.Blue, alpha);

        // 1. 创建一个小的棋盘格位图（2x2 格子）
        using var tileBitmap = new SKBitmap(cellSize * 2, cellSize * 2);
        using var tileCanvas = new SKCanvas(tileBitmap);
        // 绘制四个格子
        using var paint = new SKPaint();
        // 左上
        paint.Color = c1;
        tileCanvas.DrawRect(0, 0, cellSize, cellSize, paint);
        // 右上
        paint.Color = c2;
        tileCanvas.DrawRect(cellSize, 0, cellSize, cellSize, paint);
        // 左下
        tileCanvas.DrawRect(0, cellSize, cellSize, cellSize, paint);
        // 右下
        paint.Color = c1;
        tileCanvas.DrawRect(cellSize, cellSize, cellSize, cellSize, paint);

        // 2. 创建平铺着色器
        using var shader = SKShader.CreateBitmap(
            tileBitmap,
            SKShaderTileMode.Repeat,  // 水平重复
            SKShaderTileMode.Repeat   // 垂直重复
        );

        using var fillPaint = new SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, width, height, fillPaint);

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成斜条纹纹理（对角线方向）。
    /// 可用于背景装饰、警告标志或屏幕扫描效果。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="stripeWidth">条纹宽度（像素）</param>
    /// <param name="gapWidth">间隙宽度（像素）</param>
    /// <param name="angle">倾斜角度（度），默认 45°</param>
    /// <param name="color">条纹颜色（默认黑色）</param>
    /// <param name="gapColor">间隙颜色（默认透明）</param>
    /// <param name="alpha">条纹透明度</param>
    /// <param name="seed">随机种子（仅用于颜色偏移，未使用）</param>
    /// <returns>斜条纹纹理位图</returns>
    public static SKBitmap GenerateDiagonalStripesBitmap(
        int width,
        int height,
        int stripeWidth = 30,
        int gapWidth = 20,
        float angle = 45f,
        SKColor? color = null,
        SKColor? gapColor = null,
        byte alpha = 255,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var stripeCol = color ?? SKColors.Black;
        stripeCol = new SKColor(stripeCol.Red, stripeCol.Green, stripeCol.Blue, alpha);

        // 先填充间隙颜色（若有）
        if (gapColor.HasValue)
        {
            using var gapPaint = new SKPaint { Color = gapColor.Value };
            canvas.DrawRect(0, 0, width, height, gapPaint);
        }

        // 绘制斜条纹：旋转画布，绘制垂直条纹
        canvas.Save();
        canvas.Translate(width / 2f, height / 2f);
        canvas.RotateDegrees(angle);
        // 计算需要覆盖的宽度：对角线长度
        float diagonal = (float)Math.Sqrt(width * width + height * height);
        float totalWidth = stripeWidth + gapWidth;
        int count = (int)(diagonal / totalWidth) + 2;
        using var paint = new SKPaint { Color = stripeCol, Style = SKPaintStyle.Fill };
        for (int i = -count / 2; i < count / 2; i++)
        {
            float x = i * totalWidth - diagonal / 2;
            canvas.DrawRect(x, -diagonal / 2, stripeWidth, diagonal, paint);
        }
        canvas.Restore();

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成暗角纹理（Vignette）。
    /// 从中心向边缘渐变透明到指定颜色（通常为黑色），用于模拟镜头暗角效果。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="color">暗角颜色（默认黑色）</param>
    /// <param name="centerX">中心点X（0~1）</param>
    /// <param name="centerY">中心点Y（0~1）</param>
    /// <param name="radius">暗角半径（像素），默认取宽高的最小值</param>
    /// <param name="alpha">中心透明度（0~255），0 完全透明，255 不透明</param>
    /// <param name="edgeAlpha">边缘透明度（0~255），0 完全透明，255 不透明</param>
    /// <returns>暗角纹理位图</returns>
    public static SKBitmap GenerateVignetteBitmap(
        int width,
        int height,
        SKColor? color = null,
        float centerX = 0.5f,
        float centerY = 0.5f,
        float? radius = null,
        byte alpha = 0,
        byte edgeAlpha = 200)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var c = color ?? SKColors.Black;
        float rad = radius ?? Math.Min(width, height) * 0.7f;
        // 从中心到边缘：中心透明（alpha），边缘不透明（edgeAlpha）
        var colors = new SKColor[]
        {
        new SKColor(c.Red, c.Green, c.Blue, alpha),
        new SKColor(c.Red, c.Green, c.Blue, edgeAlpha)
        };
        var shader = SKShader.CreateRadialGradient(
            new SKPoint(centerX * width, centerY * height),
            rad,
            colors,
            null,
            SKShaderTileMode.Clamp
        );
        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, width, height, paint);

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成云彩纹理（使用分形噪声并映射为彩色云朵）。
    /// 云彩为半透明，背景透明，颜色可自定义。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="cloudColor">云朵颜色（默认白色）</param>
    /// <param name="baseFrequencyX">X 频率</param>
    /// <param name="baseFrequencyY">Y 频率</param>
    /// <param name="numOctaves">八度</param>
    /// <param name="seed">随机种子</param>
    /// <param name="maxAlpha">最大不透明度（0~255）</param>
    /// <returns>云彩纹理位图</returns>
    public static SKBitmap GenerateCloudBitmap(
        int width,
        int height,
        SKColor? cloudColor = null,
        float baseFrequencyX = 0.02f,
        float baseFrequencyY = 0.02f,
        int numOctaves = 5,
        float seed = 0,
        byte maxAlpha = 200)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var c = cloudColor ?? SKColors.White;
        // 创建分形噪声着色器
        var noiseShader = SKShader.CreatePerlinNoiseFractalNoise(
            baseFrequencyX, baseFrequencyY, numOctaves, seed);

        // 颜色矩阵：将灰度值映射为目标颜色，并设置透明度 = 灰度值 * (maxAlpha/255)
        // 输入 R=G=B=灰度值 (0-255)
        float r = c.Red / 255f;
        float g = c.Green / 255f;
        float b = c.Blue / 255f;
        float a = maxAlpha / 255f;
        // 矩阵：R' = r * R_in, G' = g * R_in, B' = b * R_in, A' = a * R_in
        float[] matrix = {
        r, 0, 0, 0, 0,
        g, 0, 0, 0, 0,
        b, 0, 0, 0, 0,
        a, 0, 0, 0, 0
    };
        using var colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        using var paint = new SKPaint
        {
            Shader = noiseShader,
            ColorFilter = colorFilter,
            BlendMode = SKBlendMode.SrcOver
        };
        canvas.DrawRect(0, 0, width, height, paint);

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成木纹纹理（模拟木材的年轮和纹理）。
    /// 使用噪声和正弦波组合产生自然木纹效果。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="woodColor">木材主色（默认棕色）</param>
    /// <param name="ringColor">年轮颜色（默认深棕色）</param>
    /// <param name="frequency">年轮密度（值越大年轮越密集）</param>
    /// <param name="seed">随机种子</param>
    /// <param name="alpha">不透明度（默认255）</param>
    /// <returns>木纹纹理位图</returns>
    public static SKBitmap GenerateWoodBitmap(
        int width,
        int height,
        SKColor? woodColor = null,
        SKColor? ringColor = null,
        float frequency = 0.03f,
        float seed = 0,
        byte alpha = 255)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var wc = woodColor ?? new SKColor(160, 120, 80);
        var rc = ringColor ?? new SKColor(100, 70, 40);
        // 应用透明度
        wc = new SKColor(wc.Red, wc.Green, wc.Blue, alpha);
        rc = new SKColor(rc.Red, rc.Green, rc.Blue, alpha);

        // 使用像素操作生成木纹
        var bitmap = new SKBitmap(info);
        var rand = new Random((int)seed);

        // 预生成噪声数组（用于扰动年轮位置）
        float[] noise = new float[width];
        for (int x = 0; x < width; x++)
        {
            noise[x] = (float)(rand.NextDouble() * 2 - 1); // -1 ~ 1
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 模拟年轮：使用正弦波 + 噪声扰动
                float pos = y + noise[x] * 10; // 垂直方向为主
                float value = (float)(0.5 + 0.5 * Math.Sin(pos * frequency * 2 * Math.PI));
                // 添加一些噪点增加自然感
                float grain = (float)(rand.NextDouble() * 0.1 - 0.05);
                value = Math.Clamp(value + grain, 0, 1);
                // 插值颜色
                var color = InterpolateColor(wc, rc, value);
                bitmap.SetPixel(x, y, color);
            }
        }

        return bitmap;
    }

    // 辅助：颜色插值
    private static SKColor InterpolateColor(SKColor c1, SKColor c2, float t)
    {
        byte r = (byte)(c1.Red + (c2.Red - c1.Red) * t);
        byte g = (byte)(c1.Green + (c2.Green - c1.Green) * t);
        byte b = (byte)(c1.Blue + (c2.Blue - c1.Blue) * t);
        byte a = (byte)(c1.Alpha + (c2.Alpha - c1.Alpha) * t);
        return new SKColor(r, g, b, a);
    }

    /// <summary>
    /// 生成点阵纹理（规则排列的圆点）。
    /// 适用于印刷半色调、屏幕网格或装饰性背景。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="dotRadius">圆点半径（像素）</param>
    /// <param name="spacingX">水平间距</param>
    /// <param name="spacingY">垂直间距</param>
    /// <param name="color">圆点颜色（默认黑色）</param>
    /// <param name="alpha">不透明度</param>
    /// <param name="offsetX">水平偏移量</param>
    /// <param name="offsetY">垂直偏移量</param>
    /// <returns>点阵纹理位图</returns>
    public static SKBitmap GenerateDotMatrixBitmap(
        int width,
        int height,
        float dotRadius = 4f,
        float spacingX = 20f,
        float spacingY = 20f,
        SKColor? color = null,
        byte alpha = 255,
        float offsetX = 0f,
        float offsetY = 0f)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var c = color ?? SKColors.Black;
        c = new SKColor(c.Red, c.Green, c.Blue, alpha);
        using var paint = new SKPaint { Color = c, IsAntialias = true };

        for (float y = offsetY % spacingY - spacingY; y < height + spacingY; y += spacingY)
        {
            for (float x = offsetX % spacingX - spacingX; x < width + spacingX; x += spacingX)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    canvas.DrawCircle(x, y, dotRadius, paint);
            }
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成线性渐变纹理（从起始颜色到结束颜色过渡）。
    /// 可用于背景渐变、遮罩或颜色映射。
    /// </summary>
    /// <param name="width">位图宽度</param>
    /// <param name="height">位图高度</param>
    /// <param name="startColor">起始颜色</param>
    /// <param name="endColor">结束颜色</param>
    /// <param name="startX">起始点X（0~1）</param>
    /// <param name="startY">起始点Y（0~1）</param>
    /// <param name="endX">结束点X（0~1）</param>
    /// <param name="endY">结束点Y（0~1）</param>
    /// <param name="tileMode">平铺模式</param>
    /// <returns>线性渐变纹理位图</returns>
    public static SKBitmap GenerateLinearGradientBitmap(
        int width,
        int height,
        SKColor startColor,
        SKColor endColor,
        float startX = 0f,
        float startY = 0f,
        float endX = 1f,
        float endY = 1f,
        SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var shader = SKShader.CreateLinearGradient(
            new SKPoint(startX * width, startY * height),
            new SKPoint(endX * width, endY * height),
            new SKColor[] { startColor, endColor },
            null,
            tileMode
        );
        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, width, height, paint);

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成胶片颗粒纹理。
    /// 通过添加细微噪点来模拟老电影的质感。
    /// </summary>
    public static SKBitmap GenerateFilmGrainBitmap(
        int width,
        int height,
        float density = 0.15f,
        SKColor? color = null,
        byte alpha = 80,
        int seed = 0)
    {
        // 复用现有的 GenerateGrainBitmap 方法，调整默认参数即可
        return GenerateGrainBitmap(width, height, density, color ?? new SKColor(128, 128, 128), alpha, seed);
    }

    /// <summary>
    /// 生成胶片灼烧/漏光纹理。
    /// 模拟胶片因意外曝光而产生的暖色光晕。
    /// </summary>
    public static SKBitmap GenerateFilmBurnBitmap(
        int width,
        int height,
        SKColor? primaryColor = null,
        SKColor? secondaryColor = null,
        float seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random((int)seed);
        var c1 = primaryColor ?? new SKColor(255, 140, 50); // 暖橙色
        var c2 = secondaryColor ?? new SKColor(255, 80, 20); // 深橙色/红色

        // 生成2-3个随机光晕
        int count = rand.Next(2, 4);
        for (int i = 0; i < count; i++)
        {
            float cx = (float)(rand.NextDouble() * width);
            float cy = (float)(rand.NextDouble() * height);
            float radius = (float)(rand.NextDouble() * Math.Max(width, height) * 0.4f + 100);
            byte alpha = (byte)rand.Next(60, 180);

            // 随机选择颜色
            var color = rand.Next(2) == 0 ? c1 : c2;
            var colors = new SKColor[]
            {
            new SKColor(color.Red, color.Green, color.Blue, alpha),
            new SKColor(color.Red, color.Green, color.Blue, 0)
            };

            using var shader = SKShader.CreateRadialGradient(
                new SKPoint(cx, cy), radius, colors, null, SKShaderTileMode.Clamp);
            using var paint = new SKPaint { Shader = shader, BlendMode = SKBlendMode.Plus };
            canvas.DrawRect(0, 0, width, height, paint);
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成 VHS 磁带纹理。
    /// 模拟老式录像带的播放效果，包含时间码、噪点和跟踪条。
    /// </summary>
    public static SKBitmap GenerateVHSTextureBitmap(
        int width,
        int height,
        SKColor? color = null,
        byte alpha = 100,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var c = color ?? new SKColor(200, 200, 255);

        // 1. 绘制随机噪点（模拟磁带噪点）
        using var noisePaint = new SKPaint
        {
            Color = new SKColor(c.Red, c.Green, c.Blue, (byte)(alpha * 0.6f)),
            Style = SKPaintStyle.Fill
        };
        int totalPixels = width * height;
        int count = (int)(totalPixels * 0.02f);
        for (int i = 0; i < count; i++)
        {
            canvas.DrawPoint(rand.Next(width), rand.Next(height), noisePaint);
        }

        // 2. 绘制水平跟踪条
        using var trackPaint = new SKPaint
        {
            Color = new SKColor(c.Red, c.Green, c.Blue, alpha),
            Style = SKPaintStyle.Fill
        };
        int trackCount = rand.Next(3, 8);
        for (int i = 0; i < trackCount; i++)
        {
            float y = (float)(rand.NextDouble() * height);
            float trackWidth = (float)(rand.NextDouble() * width * 0.3f + 20);
            float x = (float)(rand.NextDouble() * (width - trackWidth));
            float heightTrack = (float)(rand.NextDouble() * 3 + 1);
            canvas.DrawRect(x, y, trackWidth, heightTrack, trackPaint);
        }

        // 3. 绘制时间码（模拟数字闪烁）
        using var timecodePaint = new SKPaint
        {
            Color = new SKColor(c.Red, c.Green, c.Blue, (byte)(alpha * 0.8f)),
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Courier New", SKFontStyle.Bold)
        };
        string timecode = $"{rand.Next(0, 99):D2}:{rand.Next(0, 59):D2}:{rand.Next(0, 59):D2}:{rand.Next(0, 24):D2}";
        canvas.DrawText(timecode, 20, 40, timecodePaint);

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成灰尘与污渍纹理。
    /// 随机散布的微小斑点，模拟胶片或镜头上的灰尘。
    /// </summary>
    public static SKBitmap GenerateDustAndDirtBitmap(
        int width,
        int height,
        int count = 300,
        float maxRadius = 3f,
        SKColor? color = null,
        byte alpha = 80,
        int seed = 0)
    {
        // 复用现有的 GenerateDustBitmap 方法
        return GenerateDustBitmap(width, height, count, maxRadius, color ?? SKColors.White, alpha, seed);
    }

    /// <summary>
    /// 生成故障艺术纹理。
    /// 模拟数字信号的画面撕裂、色彩错位和像素块。
    /// </summary>
    public static SKBitmap GenerateGlitchBitmap(
        int width,
        int height,
        SKColor? color = null,
        byte alpha = 200,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var c = color ?? SKColors.White;

        // 1. 绘制随机彩色块（模拟像素块）
        int blockCount = rand.Next(15, 40);
        for (int i = 0; i < blockCount; i++)
        {
            float x = (float)(rand.NextDouble() * width);
            float y = (float)(rand.NextDouble() * height);
            float w = (float)(rand.NextDouble() * 60 + 10);
            float h = (float)(rand.NextDouble() * 30 + 5);
            byte r = (byte)(rand.Next(100, 256));
            byte g = (byte)(rand.Next(100, 256));
            byte b = (byte)(rand.Next(100, 256));
            using var blockPaint = new SKPaint
            {
                Color = new SKColor(r, g, b, (byte)(alpha * 0.7f)),
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(x, y, w, h, blockPaint);
        }

        // 2. 绘制水平撕裂线
        int lineCount = rand.Next(5, 15);
        using var linePaint = new SKPaint
        {
            Color = new SKColor(c.Red, c.Green, c.Blue, alpha),
            StrokeWidth = (float)(rand.NextDouble() * 4 + 1),
            Style = SKPaintStyle.Stroke
        };
        for (int i = 0; i < lineCount; i++)
        {
            float y = (float)(rand.NextDouble() * height);
            float x1 = (float)(rand.NextDouble() * width * 0.3f);
            float x2 = x1 + (float)(rand.NextDouble() * width * 0.7f);
            canvas.DrawLine(x1, y, x2, y, linePaint);
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成半色调纹理。
    /// 通过不同大小的点阵来表现明暗和颜色。
    /// </summary>
    public static SKBitmap GenerateHalftoneBitmap(
        int width,
        int height,
        SKColor? color = null,
        float dotSize = 4f,
        float spacing = 12f,
        byte alpha = 200,
        int seed = 0)
    {
        // 复用现有的 GenerateDotMatrixBitmap 方法
        return GenerateDotMatrixBitmap(width, height, dotSize, spacing, spacing, color ?? SKColors.Black, alpha, seed % 100, seed % 50);
    }

    /// <summary>
    /// 生成像素化纹理（马赛克）。
    /// 将图像处理成马赛克效果，可用于转场。
    /// </summary>
    public static SKBitmap GeneratePixelateBitmap(
        int width,
        int height,
        int blockSize = 20,
        SKColor? color1 = null,
        SKColor? color2 = null,
        int seed = 0)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rand = new Random(seed);
        var c1 = color1 ?? new SKColor(50, 50, 80);
        var c2 = color2 ?? new SKColor(150, 150, 200);

        // 使用棋盘格但每个格子更大，并添加随机颜色变化
        int cols = (int)Math.Ceiling((float)width / blockSize);
        int rows = (int)Math.Ceiling((float)height / blockSize);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                // 随机选择颜色，并加入一些变化
                var baseColor = (x + y) % 2 == 0 ? c1 : c2;
                byte r = (byte)Math.Clamp(baseColor.Red + rand.Next(-20, 20), 0, 255);
                byte g = (byte)Math.Clamp(baseColor.Green + rand.Next(-20, 20), 0, 255);
                byte b = (byte)Math.Clamp(baseColor.Blue + rand.Next(-20, 20), 0, 255);
                using var paint = new SKPaint
                {
                    Color = new SKColor(r, g, b, 200),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(x * blockSize, y * blockSize, blockSize, blockSize, paint);
            }
        }

        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    /// <summary>
    /// 生成动态转场纹理（墨迹/水流效果）。
    /// 使用噪声生成动态纹理，可用于转场遮罩。
    /// </summary>
    public static SKBitmap GenerateDynamicTransitionBitmap(
        int width,
        int height,
        SKColor? color = null,
        float baseFrequencyX = 0.02f,
        float baseFrequencyY = 0.02f,
        int numOctaves = 6,
        float seed = 0,
        byte alpha = 200)
    {
        // 复用 GenerateCloudBitmap 方法，调整参数获得更动态的效果
        return GenerateCloudBitmap(width, height, color ?? SKColors.White, baseFrequencyX, baseFrequencyY, numOctaves, seed, alpha);
    }
}
