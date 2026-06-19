namespace NewBeeVG;

public interface IPaddingable
{
    Thickness Padding { get; set; }
}

public static partial class NBExtentions
{
    public static T Padding<T>(this T widget, double left, double top, double right, double bottom) where T : IPaddingable
    {
        widget.Padding = new Thickness(left, top, right, bottom);
        return widget;
    }

    public static T Padding<T>(this T widget, double uniform) where T : IPaddingable
    {
        widget.Padding = new Thickness(uniform);
        return widget;
    }

    public static T Padding<T>(this T widget, double horizontal, double vertical) where T : IPaddingable
    {
        widget.Padding = new Thickness(horizontal, vertical);
        return widget;
    }
}