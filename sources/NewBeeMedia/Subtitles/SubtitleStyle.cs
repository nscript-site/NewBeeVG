using System.Drawing;

namespace NewBeeMedia.Subtitles;

/// <summary>
/// 字幕格式
/// </summary>
public class SubtitleStyle
{
    public static SubtitleStyle DefaultStyle = new SubtitleStyle();

    public String Id { get; set; }

    public float FontSize { get; set; }

    public String FontName { get; set; }

    public String FontAlign { get; set; }

    public int FontColor { get; set; } = Color.White.ToArgb();

    public String Title { get; set; }

    /// <summary>
    /// 水平安全距离
    /// </summary>
    public double HSafeMargin { get; set; }

    /// <summary>
    /// 垂直安全距离
    /// </summary>
    public double VSafeMargin { get; set; }

    public float BorderThickness { get; set; }

    public int BorderColor { get; set; }

    public SubtitleStyle Clone()
    {
        SubtitleStyle setting = new SubtitleStyle();
        setting.Id = this.Id;
        setting.FontSize = this.FontSize;
        setting.FontName = this.FontName;
        setting.FontAlign = this.FontAlign;
        setting.FontColor = this.FontColor;
        setting.Title = this.Title;
        setting.HSafeMargin = this.HSafeMargin;
        setting.VSafeMargin = this.VSafeMargin;
        setting.BorderThickness = this.BorderThickness;
        setting.BorderColor = this.BorderColor;
        return setting;
    }

    public SubtitleStyle()
    {
        FontAlign = "center"; FontName = "微软雅黑"; FontColor = Color.White.ToArgb(); FontSize = 20.0f;
        BorderColor = Color.Black.ToArgb();
        HSafeMargin = 10;
        VSafeMargin = 10;
        Id = Guid.NewGuid().ToString().Replace("-", "");
    }

    public Rectangle GetSafeRegion(int frameWidth, int frameHeight)
    {
        int safeMarginX = (int)(HSafeMargin);
        int safeMarginY = (int)(VSafeMargin);

        Rectangle rectSafe = new Rectangle(safeMarginX, safeMarginY,
            frameWidth - safeMarginX * 2,
            frameHeight - safeMarginY * 2);
        return rectSafe;
    }
}
