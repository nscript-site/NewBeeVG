/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG.Internal;

/// <summary>
/// A collection of <see cref="NBColumnDefinition"/>s.
/// </summary>
public class NBColumnDefinitions : NBDefinitionList<NBColumnDefinition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NBColumnDefinitions"/> class.
    /// </summary>
    public NBColumnDefinitions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NBColumnDefinitions"/> class.
    /// </summary>
    /// <param name="s">A string representation of the column definitions.</param>
    public NBColumnDefinitions(string s)
        : this()
    {
        AddRange(GridLength.ParseLengths(s).Select(x => new NBColumnDefinition(x)));
    }

    public override string ToString()
    {
        return string.Join(",", this.Select(x => x.Width));
    }

    /// <summary>
    /// Parses a string representation of column definitions collection.
    /// </summary>
    /// <param name="s">The column definitions string.</param>
    /// <returns>The <see cref="NBColumnDefinitions"/>.</returns>
    public static NBColumnDefinitions Parse(string s) => new NBColumnDefinitions(s);
}
