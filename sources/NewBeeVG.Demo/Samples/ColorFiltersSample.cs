using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class ColorFiltersSample
{
    public static void Run()
    {
        // ===================== 定义所有颜色滤镜 =====================
        var filters = new List<(string name, NBColorFilter filter)>();

        // 1. 灰度
        filters.Add(("Gray", ColorFilters.Gray()));

        // 2. 亮度 +0.3（变亮）
        filters.Add(("Brightness +0.3", ColorFilters.Brightness(0.3f)));

        // 3. 亮度 -0.3（变暗）
        filters.Add(("Brightness -0.3", ColorFilters.Brightness(-0.3f)));

        // 4. 对比度 1.5（增强对比度）
        filters.Add(("Contrast 1.5", ColorFilters.Contrast(1.5f)));

        // 5. 对比度 0.5（降低对比度）
        filters.Add(("Contrast 0.5", ColorFilters.Contrast(0.5f)));

        // 6. 亮度+0.2 对比度1.3（组合）
        filters.Add(("BrightnessContrast", ColorFilters.BrightnessContrast(0.2f, 1.3f)));

        // 7. 饱和度 0（完全灰度）
        filters.Add(("Saturation 0", ColorFilters.Saturation(0f)));

        // 8. 饱和度 2.0（高饱和）
        filters.Add(("Saturation 2.0", ColorFilters.Saturation(2.0f)));

        // 9. 色相旋转 90°
        filters.Add(("HueRotation 90°", ColorFilters.HueRotation(90f)));

        // 10. 色相旋转 180°
        filters.Add(("HueRotation 180°", ColorFilters.HueRotation(180f)));

        // 11. 颜色平衡：红增1.5，绿1.0，蓝0.5（偏暖）
        filters.Add(("ColorBalance Warm", ColorFilters.ColorBalance(1.5f, 1.0f, 0.5f)));

        // 12. 颜色平衡：红0.5，绿1.0，蓝1.5（偏冷）
        filters.Add(("ColorBalance Cool", ColorFilters.ColorBalance(0.5f, 1.0f, 1.5f)));

        // 13. 色调（Tint）红色，强度0.5
        filters.Add(("Tint Red 0.5", ColorFilters.Tint(SKColors.Red, 0.5f)));

        // 14. 色调（Tint）蓝色，强度0.8
        filters.Add(("Tint Blue 0.8", ColorFilters.Tint(SKColors.Blue, 0.8f)));

        // 15. 反转（负片）
        filters.Add(("Invert", ColorFilters.Invert()));

        // 16. 复古 Sepia（完全）
        filters.Add(("Sepia 1.0", ColorFilters.Sepia(1.0f)));

        // 17. 复古 Sepia（强度0.5，混合原图）
        filters.Add(("Sepia 0.5", ColorFilters.Sepia(0.5f)));

        // 18. 曝光 +1 EV（变亮）
        filters.Add(("Exposure +1", ColorFilters.Exposure(1.0f)));

        // 19. 曝光 -1 EV（变暗）
        filters.Add(("Exposure -1", ColorFilters.Exposure(-1.0f)));

        // 20. Gamma 0.8（变亮）
        filters.Add(("Gamma 0.8", ColorFilters.Gamma(0.8f)));

        // 21. Gamma 2.2（变暗）
        filters.Add(("Gamma 2.2", ColorFilters.Gamma(2.2f)));

        // 22. 颜色叠加：Multiply 蓝色
        filters.Add(("Overlay Blue (Multiply)", ColorFilters.ColorOverlay(SKColors.Blue, SKBlendMode.Multiply)));

        // 23. 颜色叠加：Screen 红色
        filters.Add(("Overlay Red (Screen)", ColorFilters.ColorOverlay(SKColors.Red, SKBlendMode.Screen)));

        // 24. 光照：乘法 (1.2,0.8,0.6) 加法 (0.1,0,0)
        var mul = new SKColor((byte)(0.2f * 255), (byte)(0.8f * 255), (byte)(0.6f * 255));
        var add = new SKColor(25, 0, 0);
        filters.Add(("Lighting", ColorFilters.Lighting(mul, add)));

        // 25. 色调分离（Posterize）4级
        filters.Add(("Posterize 4", ColorFilters.Posterize(4)));

        // 26. 色调分离（Posterize）8级
        filters.Add(("Posterize 8", ColorFilters.Posterize(8)));

        // 27. 黑白（高对比度）
        filters.Add(("BlackAndWhite", ColorFilters.BlackAndWhite()));

        // 28. 自定义 Table（反转亮度，相当于底片效果）
        byte[] invertTable = new byte[256];
        for (int i = 0; i < 256; i++) invertTable[i] = (byte)(255 - i);
        filters.Add(("Table Invert", ColorFilters.Table(invertTable)));

        // 29. 自定义 Table（增加对比度曲线）
        byte[] contrastTable = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            float v = i / 255f;
            v = (v - 0.5f) * 1.5f + 0.5f;
            v = Math.Clamp(v, 0f, 1f);
            contrastTable[i] = (byte)(v * 255);
        }
        filters.Add(("Table Contrast", ColorFilters.Table(contrastTable)));

        // ===================== 构建预览 Clips =====================
        NBDrawingClip[] BuildClips()
        {
            var list = new List<NBDrawingClip>();
            foreach (var (name, filter) in filters)
            {
                var image = Image("./Assets/snows.jpg")
                    .Size(600, 300)
                    .ColorFilters(filter)   // 应用颜色滤镜
                    .Stretch(Stretch.Fill)
                    .Align(0, 0);

                var panel = Panel([
                    image,
                    TextBlock(name).Font(40, SKColors.White).Margin(50).Align(-1, -1)
                ]);

                panel.AsClip(out var clip, frames: 20, name: name);
                list.Add(clip);
            }
            return list.ToArray();
        }

        run(stage(1920, 1080, bg: SKColors.Orange), BuildClips());
    }
}
