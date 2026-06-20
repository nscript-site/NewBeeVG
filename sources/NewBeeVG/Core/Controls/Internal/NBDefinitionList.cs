/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/


using Avalonia.Collections;
using System.Collections;
using System.Collections.Specialized;

namespace NewBeeVG.Internal;

public abstract class NBDefinitionList<T> : AvaloniaList<T> where T : NBDefinitionBase
{
    public NBDefinitionList()
    {
        ResetBehavior = ResetBehavior.Remove;
        CollectionChanged += OnCollectionChanged;
    }

    internal bool IsDirty = true;
    private NBGrid? _parent;

    internal NBGrid? Parent
    {
        get => _parent;
        set => SetParent(value);
    }

    private void SetParent(NBGrid? value)
    {
        _parent = value;

        var idx = 0;

        foreach (T definition in this)
        {
            definition.Parent = value;
            definition.Index = idx++;
        }
    }

    internal void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var idx = 0;

        foreach (T definition in this)
        {
            definition.Index = idx++;
        }

        UpdateDefinitionParent(e.NewItems, false);
        UpdateDefinitionParent(e.OldItems, true);

        IsDirty = true;
    }

    private void UpdateDefinitionParent(IList? items, bool wasRemoved)
    {
        if (items is null)
        {
            return;
        }

        var count = items.Count;

        for (var i = 0; i < count; i++)
        {
            var definition = (NBDefinitionBase)items[i]!;

            if (wasRemoved)
            {
            }
            else
            {
                definition.Parent = Parent;
            }
        }
    }
}