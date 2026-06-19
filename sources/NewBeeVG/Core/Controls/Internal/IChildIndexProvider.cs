using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Internal;

/// <summary>
/// Child's index and total count information provider used by list-controls (ListBox, StackPanel, etc.)
/// </summary>
/// <remarks>
/// Used by nth-child and nth-last-child selectors. 
/// </remarks>
public interface IChildIndexProvider
{
    int GetChildIndex(NBVisual child);

    /// <summary>
    /// Total children count or null if source is infinite.
    /// Some Avalonia features might not work if <see cref="TryGetTotalCount"/> returns false, for instance: nth-last-child selector.
    /// </summary>
    bool TryGetTotalCount(out int count);

    /// <summary>
    /// Notifies subscriber when a child's index was changed.
    /// </summary>
    event EventHandler<ChildIndexChangedEventArgs>? ChildIndexChanged;
}