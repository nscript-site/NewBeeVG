using Avalonia.Collections;

namespace NewBeeVG;

public class NBWidgets : AvaloniaList<NBVisual>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Controls"/> class.
    /// </summary>
    public NBWidgets()
    {
        Configure();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Controls"/> class.
    /// </summary>
    /// <param name="items">The initial items in the collection.</param>
    public NBWidgets(IEnumerable<NBVisual> items)
    {
        Configure();
        AddRange(items); // virtual member call in ctor, ok for our current implementation
    }

    private void Configure()
    {
        ResetBehavior = ResetBehavior.Remove;
    }
}
