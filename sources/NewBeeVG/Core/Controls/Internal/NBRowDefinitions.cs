/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG.Internal;

/// <summary>
/// A collection of <see cref="RowDefinition"/>s.
/// </summary>
public class NBRowDefinitions : NBDefinitionList<NBRowDefinition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
    /// </summary>
    public NBRowDefinitions() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
    /// </summary>
    /// <param name="s">A string representation of the row definitions.</param>
    public NBRowDefinitions(string s)
        : this()
    {
        AddRange(GridLength.ParseLengths(s).Select(x => new NBRowDefinition(x)));
    }

    public override string ToString()
    {
        return string.Join(",", this.Select(x => x.Height));
    }

    /// <summary>
    /// Parses a string representation of row definitions collection.
    /// </summary>
    /// <param name="s">The row definitions string.</param>
    /// <returns>The <see cref="RowDefinitions"/>.</returns>
    public static NBRowDefinitions Parse(string s) => new NBRowDefinitions(s);
}