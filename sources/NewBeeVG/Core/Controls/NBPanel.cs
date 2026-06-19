using NewBeeVG.Internal;
using SkiaSharp;
using System.Collections.Specialized;

namespace NewBeeVG;

public class NBPanel : NBLayoutable, IChildIndexProvider
{
    public NBWidgets Children { get; } = new NBWidgets();

    private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;

    public NBBrush? Background { get; set; }

    public NBPanel()
    {
        Children.CollectionChanged += ChildrenChanged;
    }

    protected sealed override void RenderBackground(SKCanvas context)
    {
        var paint = Background?.GetPaint();
        if(paint != null)
        {
            context.DrawRect(Bounds, paint);
        }

        base.RenderBackground(context);
    }

    /// <summary>
    /// Called when the <see cref="Children"/> collection changes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event args.</param>
    protected virtual void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                VisualChildren.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<NBVisual>());
                break;

            case NotifyCollectionChangedAction.Move:
                VisualChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Remove:
                VisualChildren.RemoveAll(e.OldItems!.OfType<NBVisual>());
                break;

            case NotifyCollectionChangedAction.Replace:
                for (var i = 0; i < e.OldItems!.Count; ++i)
                {
                    var index = i + e.OldStartingIndex;
                    var child = (NBVisual)e.NewItems![i]!;
                    VisualChildren[index] = child;
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                throw new NotSupportedException();
        }

        _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.ChildIndexesReset);
        InvalidateMeasureOnChildrenChanged();
    }

    private protected virtual void InvalidateMeasureOnChildrenChanged()
    {
        InvalidateMeasure();
    }

    event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
    {
        add
        {
            _childIndexChanged += value;
        }

        remove
        {
            _childIndexChanged -= value;
        }
    }

    int IChildIndexProvider.GetChildIndex(NBVisual child)
    {
        return child is NBVisual control ? Children.IndexOf(child) : -1;
    }

    /// <inheritdoc />
    bool IChildIndexProvider.TryGetTotalCount(out int count)
    {
        count = Children.Count;
        return true;
    }
}

public static partial class NBExtentions
{
    /// <summary>
    /// Sets the background color of the panel.
    /// </summary>
    /// <param name="panel">The panel to set the background color for.</param>
    /// <param name="color">The color to set as the background.</param>
    public static T Background<T>(this T panel, SKColor color) where T : NBPanel
    {
        panel.Background = color;
        return panel;
    }

    public static T Childs<T>(this T panel, NBVisual[] children) where T : NBPanel
    {
        panel.Children.AddRange(children);
        return panel;
    }
}
