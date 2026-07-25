using System.Collections.Generic;

namespace NewBeeVG.Demo.Samples;

internal class FiltersSample
{
    public static void Run()
    {
        // ===================== 定义所有滤镜 =====================
        var filters = new List<(string name, NBImageFilter filter)>();

        // 1. 模糊
        filters.Add(("Blur", Filters.Blur(5, 5)));

        // 2. 投影阴影
        filters.Add(("DropShadow", Filters.DropShadow(SKColors.Black, 10, 10, 5, 5)));

        // 3. 锐化
        filters.Add(("Sharpen", Filters.Sharpen(1.5f)));

        // 4. 边缘检测
        filters.Add(("EdgeDetect", Filters.EdgeDetect()));

        // 5. 浮雕
        filters.Add(("Emboss", Filters.Emboss()));

        // 6. 膨胀（半径 3）
        filters.Add(("Dilate", Filters.Dilate(3, 3)));

        // 7. 腐蚀（半径 3）
        filters.Add(("Erode", Filters.Erode(3, 3)));

        // 8. 偏移（向右下偏移 50px）
        filters.Add(("Offset", Filters.Offset(50, 50)));

        // 9. 矩阵变换（旋转 15°）
        var matrix = SKMatrix.CreateRotationDegrees(15, 300, 150); // 假设图片宽600高300，中心旋转
        filters.Add(("MatrixTransform", Filters.MatrixTransform(matrix, SKSamplingOptions.Default)));

        // 10. 远距离漫反射光照（光源从左上前方）
        filters.Add(("DistantLitDiffuse", Filters.DistantLitDiffuse(
            new SKPoint3(0.5f, -0.5f, 1), SKColors.White, 0.5f, 1.0f)));

        // 11. 远距离镜面光照
        filters.Add(("DistantLitSpecular", Filters.DistantLitSpecular(
            new SKPoint3(0.5f, -0.5f, 1), SKColors.White, 0.5f, 1.0f, 30f)));

        // 12. 点光源漫反射（位置在右上角）
        filters.Add(("PointLitDiffuse", Filters.PointLitDiffuse(
            new SKPoint3(500, 0, 200), SKColors.White, 0.5f, 1.0f)));

        // 13. 点光源镜面反射
        filters.Add(("PointLitSpecular", Filters.PointLitSpecular(
            new SKPoint3(500, 0, 200), SKColors.White, 0.5f, 1.0f, 30f)));

        // 14. 聚光灯漫反射（位置在左上，目标中心）
        filters.Add(("SpotLitDiffuse", Filters.SpotLitDiffuse(
            new SKPoint3(0, 0, 300), new SKPoint3(300, 150, 0),
            20f, (float)(Math.PI / 4), SKColors.White, 0.5f, 1.0f)));

        // 15. 聚光灯镜面反射
        filters.Add(("SpotLitSpecular", Filters.SpotLitSpecular(
            new SKPoint3(0, 0, 300), new SKPoint3(300, 150, 0),
            20f, (float)(Math.PI / 4), SKColors.White, 0.5f, 1.0f, 30f)));

        // 16. 放大镜（在图像中央区域放大2倍）
        var lensRect = SKRect.Create(150, 50, 300, 200); // 假设图片600x300
        filters.Add(("Magnifier", Filters.Magnifier(lensRect, 2.0f, 20f, SKSamplingOptions.Default)));

        // 17. 平铺（将左上角1/4区域平铺到整个图像）
        var src = SKRect.Create(0, 0, 300, 150);
        var dst = SKRect.Create(0, 0, 600, 300);
        var identity = Filters.Blur(0, 0);
        filters.Add(("Tile", Filters.Tile(src, dst, identity)));

        // 18. 颜色滤镜（使用 Lighting 增加红色调）
        var colorFilter = SKColorFilter.CreateLighting(SKColors.White, SKColors.Red);
        filters.Add(("ColorFilter", Filters.ColorFilter(colorFilter)));

        // 19. 内发光（白色，模糊半径10）
        filters.Add(("InnerGlow", Filters.InnerGlow(SKColors.White, 10, 0)));

        NBDrawingClip[] BuildClips()
        {
            var list = new List<NBDrawingClip>();
            foreach (var filter in filters)
            {
                Console.WriteLine(filter.Item1);
                Panel([
                    Image("./Assets/snows.jpg").Size(600,300).Filters(filter.Item2).Stretch(Stretch.Fill).Align(0,0),
                    TextBlock(filter.Item1??"").Font(40, SKColors.White).Margin(50).Align(-1,-1)
                ]).AsClip(out var clip1, frames: 20, name: filter.Item1 ?? "filter");
                list.Add(clip1);
            }
            return list.ToArray();
        }

        run(stage(1920, 1080, bg: SKColors.Orange), BuildClips());
    }
}
