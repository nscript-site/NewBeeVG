/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG.Internal;

/// <summary>
/// Holds a row definitions for a <see cref="Grid"/>.
/// </summary>
public class NBRowDefinition : NBDefinitionBase
{
    public double MaxHeight { get; set; } = double.PositiveInfinity;

    public double MinHeight { get; set; } = 0;

    public GridLength Height { get; set; } = new GridLength(1, GridUnitType.Star);

    /// <summary>
    /// Initializes a new instance of the <see cref="NBRowDefinition"/> class.
    /// </summary>
    public NBRowDefinition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NBRowDefinition"/> class.
    /// </summary>
    /// <param name="value">The height of the row.</param>
    /// <param name="type">The height unit of the column.</param>
    public NBRowDefinition(double value, GridUnitType type)
        : this(new GridLength(value, type))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NBRowDefinition"/> class.
    /// </summary>
    /// <param name="height">The height of the column.</param>
    public NBRowDefinition(GridLength height)
    {
        Height = height;
    }

    /// <summary>
    /// Gets the actual calculated height of the row.
    /// </summary>
    public double ActualHeight => Parent?.GetFinalRowDefinitionHeight(Index) ?? 0d;

    internal override GridLength UserSizeValueCache => this.Height;
    internal override double UserMinSizeValueCache => this.MinHeight;
    internal override double UserMaxSizeValueCache => this.MaxHeight;
}