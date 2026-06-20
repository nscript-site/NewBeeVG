/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG.Internal;

/// <summary>
/// Holds a column definitions for a <see cref="Grid"/>.
/// </summary>
public class NBColumnDefinition : NBDefinitionBase
{
    public double MaxWidth { get; set; } = double.PositiveInfinity;

    public double MinWidth { get; set; } = 0;

    public GridLength Width { get; set; } = new GridLength(1, GridUnitType.Star);

    /// <summary>
    /// Initializes a new instance of the <see cref="NBColumnDefinition"/> class.
    /// </summary>
    public NBColumnDefinition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NBColumnDefinition"/> class.
    /// </summary>
    /// <param name="value">The width of the column.</param>
    /// <param name="type">The width unit of the column.</param>
    public NBColumnDefinition(double value, GridUnitType type)
        : this(new GridLength(value, type))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NBColumnDefinition"/> class.
    /// </summary>
    /// <param name="width">The width of the column.</param>
    public NBColumnDefinition(GridLength width)
    {
        Width = width;
    }

    /// <summary>
    /// Gets the actual calculated width of the column.
    /// </summary>
    public double ActualWidth => Parent?.GetFinalColumnDefinitionWidth(Index) ?? 0d;

    internal override GridLength UserSizeValueCache => this.Width;
    internal override double UserMinSizeValueCache => this.MinWidth;
    internal override double UserMaxSizeValueCache => this.MaxWidth;
}