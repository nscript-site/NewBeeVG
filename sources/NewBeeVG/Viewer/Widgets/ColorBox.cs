using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Viewer.Widgets;

public class ColorBox : BaseView
{
    public SKColor BackgroundColor { get; set; } = SKColors.Transparent;

    protected override void Build(out Control content)
    {
        Border().Size(this.Width,this.Height).Background(() => GetBrush(BackgroundColor))
            .BorderThickness(1).BorderBrush(Brushes.LightGray).CornerRadius(2)
            .Return(out content);
    }

    private IBrush GetBrush(SKColor color)
    {
        if (color.Alpha == 0) return CreateCheckerboardBrush();
        return CreateBrush(color);
    }

    public IBrush CreateBrush(SKColor color)
    {
        return new SolidColorBrush(new Color(color.Alpha,color.Red,color.Green,color.Blue));
    }

    public IBrush CreateCheckerboardBrush()
    {
        // 1. 创建基础几何图形（一个深色方块 + 一个浅色背景，利用平铺形成棋盘）
        // 这里用最简单的方式：一个半透明的深色方块，平铺在浅色背景上
        var darkSquare = new GeometryDrawing
        {
            Brush = new SolidColorBrush(Colors.Gray, 0.3),
            Geometry = new RectangleGeometry(new Rect(0, 0, 3, 3))
        };

        var drawingGroup = new DrawingGroup();
        drawingGroup.Children.Add(darkSquare);

        // 2. 创建 DrawingBrush 并设置平铺
        return new DrawingBrush(drawingGroup)
        {
            TileMode = TileMode.Tile,
            Stretch = Avalonia.Media.Stretch.None,
            DestinationRect = new RelativeRect(0, 0, 5, 5, RelativeUnit.Absolute)
            // 注意：如果只用单个方块平铺，需要结合背景色使用
        };
    }
}
