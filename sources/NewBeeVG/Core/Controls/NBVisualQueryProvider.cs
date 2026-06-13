/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG;

/// <summary>
/// Defines how a container is queried.
/// </summary>
public enum NBContainerSizing
{
    /// <summary>
    /// The container is not included in any size queries.
    /// </summary>
    Normal,

    /// <summary>
    /// The container size can be queried for width.
    /// </summary>
    Width,

    /// <summary>
    /// The container size can be queried for height.
    /// </summary>
    Height,

    /// <summary>
    /// The container size can be queried for width and height.
    /// </summary>
    WidthAndHeight
}

internal class NBVisualQueryProvider
{
    private readonly NBVisual _visual;

    public double Width { get; private set; } = double.PositiveInfinity;

    public double Height { get; private set; } = double.PositiveInfinity;

    public NBVisualQueryProvider(NBVisual visual)
    {
        _visual = visual;
    }

    public event EventHandler? WidthChanged;
    public event EventHandler? HeightChanged;

    public virtual void SetSize(double width, double height, NBContainerSizing containerType)
    {
        var currentWidth = Width;
        var currentHeight = Height;
        
        Width = width;
        Height = height;

        if (currentWidth != Width && (containerType == NBContainerSizing.Width || containerType == NBContainerSizing.WidthAndHeight))
            WidthChanged?.Invoke(this, EventArgs.Empty);
        if (currentHeight != Height && (containerType == NBContainerSizing.Height || containerType == NBContainerSizing.WidthAndHeight))
            HeightChanged?.Invoke(this, EventArgs.Empty);
    }
}
