/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using Avalonia.Logging;
using Avalonia.Threading;
using System.Collections;
using System.Diagnostics;

namespace NewBeeVG.Layout;

/// <summary>
/// Defines the root of a layoutable tree.
/// </summary>
internal interface ILayoutRoot
{
    /// <summary>
    /// The scaling factor to use in layout.
    /// </summary>
    public double LayoutScaling { get; }

    /// <summary>
    /// Associated instance of layout manager
    /// </summary>
    public INBLayoutManager LayoutManager { get; }

    public NBLayoutable RootVisual { get; }
}

internal class LayoutQueue<T> : IReadOnlyCollection<T>, IDisposable
        where T : notnull
{
    private struct Info
    {
        public bool Active;
        public int Count;
    }

    public LayoutQueue(Func<T, bool> shouldEnqueue)
    {
        _shouldEnqueue = shouldEnqueue;
    }

    private readonly Func<T, bool> _shouldEnqueue;
    private readonly Queue<T> _inner = new Queue<T>();
    private readonly Dictionary<T, Info> _loopQueueInfo = new Dictionary<T, Info>();
    private readonly List<KeyValuePair<T, Info>> _notFinalizedBuffer = new List<KeyValuePair<T, Info>>();

    private int _maxEnqueueCountPerLoop = 1;

    public int Count => _inner.Count;

    public IEnumerator<T> GetEnumerator() => (_inner as IEnumerable<T>).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

    public T Dequeue()
    {
        var result = _inner.Dequeue();

        if (_loopQueueInfo.TryGetValue(result, out var info))
        {
            info.Active = false;
            _loopQueueInfo[result] = info;
        }

        return result;
    }

    public void Enqueue(T item)
    {
        _loopQueueInfo.TryGetValue(item, out var info);

        if (!info.Active)
        {
            if (info.Count < _maxEnqueueCountPerLoop)
            {
                _inner.Enqueue(item);
                _loopQueueInfo[item] = new Info() { Active = true, Count = info.Count + 1 };
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Layout)?.Log(
                    this,
                    "Layout cycle detected. Item {Item} was enqueued {Count} times.",
                    item,
                    info.Count);
            }
        }
    }

    public void BeginLoop(int maxEnqueueCountPerLoop)
    {
        _maxEnqueueCountPerLoop = maxEnqueueCountPerLoop;
    }

    public void EndLoop()
    {
        foreach (KeyValuePair<T, Info> info in _loopQueueInfo)
        {
            if (info.Value.Count >= _maxEnqueueCountPerLoop)
            {
                _notFinalizedBuffer.Add(info);
            }
        }

        _loopQueueInfo.Clear();

        // Prevent layout cycle but add to next layout the non arranged/measured items that might have caused cycle
        // one more time as a final attempt.
        foreach (var item in _notFinalizedBuffer)
        {
            if (_shouldEnqueue(item.Key))
            {
                _loopQueueInfo[item.Key] = new Info() { Active = true, Count = 0 };
                _inner.Enqueue(item.Key);
            }
        }

        _notFinalizedBuffer.Clear();
    }

    public void Dispose()
    {
        _inner.Clear();
        _loopQueueInfo.Clear();
        _notFinalizedBuffer.Clear();
    }
}

/// <summary>
/// Represents a single layout pass timing.
/// </summary>
/// <param name="PassCounter">The number of the layout pass.</param>
/// <param name="Elapsed">The elapsed time during the layout pass.</param>
internal readonly record struct LayoutPassTiming(int PassCounter, TimeSpan Elapsed);

/// <summary>
/// Manages measuring and arranging of controls.
/// </summary>
internal class NBLayoutManager : INBLayoutManager, IDisposable
{
    private const int MaxPasses = 10;
    private readonly ILayoutRoot _owner;
    private readonly LayoutQueue<NBLayoutable> _toMeasure = new LayoutQueue<NBLayoutable>(v => !v.IsMeasureValid);
    private readonly LayoutQueue<NBLayoutable> _toArrange = new LayoutQueue<NBLayoutable>(v => !v.IsArrangeValid);
    private readonly List<NBLayoutable> _toArrangeAfterMeasure = new();
    private bool _disposed;
    private bool _queued;
    private bool _running;
    private int _totalPassCount;
    private readonly Action _invokeOnRender;

    public NBLayoutManager(ILayoutRoot owner)
    {
        _owner = owner;
        _invokeOnRender = ExecuteQueuedLayoutPass;
    }

    public virtual event EventHandler? LayoutUpdated;

    internal Action<LayoutPassTiming>? LayoutPassTimed { get; set; }

    /// <inheritdoc/>
    public virtual void InvalidateMeasure(NBLayoutable control)
    {
        control = control ?? throw new ArgumentNullException(nameof(control));
        Dispatcher.UIThread.VerifyAccess();

        if (_disposed)
        {
            return;
        }

        if (!control.IsAttachedToVisualTree)
        {
#if DEBUG
            throw new AvaloniaInternalException(
                "LayoutManager.InvalidateMeasure called on a control that is detached from the visual tree.");
#else
                return;
#endif
        }

        if (control.GetLayoutRoot() != _owner)
        {
            throw new ArgumentException("Attempt to call InvalidateMeasure on wrong LayoutManager.");
        }

        _toMeasure.Enqueue(control);
        QueueLayoutPass();
    }

    /// <inheritdoc/>
    public virtual void InvalidateArrange(NBLayoutable control)
    {
        control = control ?? throw new ArgumentNullException(nameof(control));
        Dispatcher.UIThread.VerifyAccess();

        if (_disposed)
        {
            return;
        }

        if (!control.IsAttachedToVisualTree)
        {
#if DEBUG
            throw new AvaloniaInternalException(
                "LayoutManager.InvalidateArrange called on a control that is detached from the visual tree.");
#else
                return;
#endif
        }

        if (control.GetLayoutRoot() != _owner)
        {
            throw new ArgumentException("Attempt to call InvalidateArrange on wrong LayoutManager.");
        }

        _toArrange.Enqueue(control);
        QueueLayoutPass();
    }

    internal void ExecuteQueuedLayoutPass()
    {
        if (!_queued)
        {
            return;
        }

        ExecuteLayoutPass();
    }

    /// <inheritdoc/>
    public virtual void ExecuteLayoutPass()
    {
        Dispatcher.UIThread.VerifyAccess();

        if (_disposed)
        {
            return;
        }

        if (!_running)
        {
            const LogEventLevel timingLogLevel = LogEventLevel.Information;
            var captureTiming = LayoutPassTimed is not null || Logger.IsEnabled(timingLogLevel, LogArea.Layout);
            var startingTimestamp = 0L;

            if (captureTiming)
            {
                Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(
                    this,
                    "Started layout pass. To measure: {Measure} To arrange: {Arrange}",
                    _toMeasure.Count,
                    _toArrange.Count);

                startingTimestamp = Stopwatch.GetTimestamp();
            }

            _toMeasure.BeginLoop(MaxPasses);
            _toArrange.BeginLoop(MaxPasses);

            try
            {
                _running = true;
                ++_totalPassCount;

                for (var pass = 0; pass < MaxPasses; ++pass)
                {
                    InnerLayoutPass();
                }
            }
            finally
            {
                _running = false;
            }

            _toMeasure.EndLoop();
            _toArrange.EndLoop();

            if (captureTiming)
            {
                var elapsed = StopwatchHelper.GetElapsedTime(startingTimestamp);
                LayoutPassTimed?.Invoke(new LayoutPassTiming(_totalPassCount, elapsed));

                Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(this, "Layout pass finished in {Time}", elapsed);
            }
        }

        _queued = false;
        LayoutUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public virtual void ExecuteInitialLayoutPass()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_owner?.RootVisual == null)
                return;
            var root = _owner.RootVisual;
            _running = true;
            Measure(root);
            Arrange(root);
        }
        finally
        {
            _running = false;
        }

        // Running the initial layout pass may have caused some control to be invalidated
        // so run a full layout pass now (this usually due to scrollbars; its not known
        // whether they will need to be shown until the layout pass has run and if the
        // first guess was incorrect the layout will need to be updated).
        ExecuteLayoutPass();
    }

    public void Dispose()
    {
        _disposed = true;
        _toMeasure.Dispose();
        _toArrange.Dispose();
    }

    private void InnerLayoutPass()
    {
        for (var pass = 0; pass < MaxPasses; ++pass)
        {
            ExecuteMeasurePass();
            ExecuteArrangePass();

            if (_toMeasure.Count == 0)
            {
                break;
            }
        }
    }

    private void ExecuteMeasurePass()
    {
        while (_toMeasure.Count > 0)
        {
            var control = _toMeasure.Dequeue();

            if (!control.IsMeasureValid)
            {
                Measure(control);
            }

            _toArrange.Enqueue(control);
        }
    }

    private void ExecuteArrangePass()
    {
        while (_toArrange.Count > 0)
        {
            var control = _toArrange.Dequeue();

            if (!control.IsArrangeValid)
            {
                if (Arrange(control) == ArrangeResult.AncestorMeasureInvalid)
                    _toArrangeAfterMeasure.Add(control);
            }
        }

        foreach (var i in _toArrangeAfterMeasure)
            InvalidateArrange(i);
        _toArrangeAfterMeasure.Clear();
    }

    private bool Measure(NBLayoutable control)
    {
        if (!control.IsVisible || !control.IsAttachedToVisualTree)
            return false;

        // Controls closest to the visual root need to be arranged first. We don't try to store
        // ordered invalidation lists, instead we traverse the tree upwards, measuring the
        // controls closest to the root first. This has been shown by benchmarks to be the
        // fastest and most memory-efficient algorithm.
        if (control.VisualParent is NBLayoutable parent)
        {
            if (!Measure(parent))
                return false;
        }

        // If the control being measured has IsMeasureValid == true here then its measure was
        // handed by an ancestor and can be ignored. The measure may have also caused the
        // control to be removed.
        if (!control.IsMeasureValid)
        {
            if (control.GetLayoutRoot()?.RootVisual == control)
            {
                control.Measure(Size.Infinity);
            }
            else if (control.PreviousMeasure.HasValue)
            {
                control.Measure(control.PreviousMeasure.Value);
            }
        }

        return true;
    }

    private ArrangeResult Arrange(NBLayoutable control)
    {
        if (!control.IsVisible || !control.IsAttachedToVisualTree)
            return ArrangeResult.NotVisible;

        if (control.VisualParent is NBLayoutable parent)
        {
            if (Arrange(parent) is var parentResult && parentResult != ArrangeResult.Arranged)
                return parentResult;
        }

        if (!control.IsMeasureValid)
            return ArrangeResult.AncestorMeasureInvalid;

        if (!control.IsArrangeValid)
        {
            if (control.GetLayoutRoot()?.RootVisual == control)
                control.Arrange(new Rect(control.DesiredSize));
            else if (control.PreviousArrange != null)
            {
                // Has been observed that PreviousArrange sometimes is null, probably a bug somewhere else.
                // Condition observed: control.VisualParent is Scrollbar, control is Border.
                control.Arrange(control.PreviousArrange.Value);
            }
        }

        return ArrangeResult.Arranged;
    }

    private void QueueLayoutPass()
    {
        if (!_queued && !_running)
        {
            _queued = true;
            //MediaContext.Instance.BeginInvokeOnRender(_invokeOnRender);
        }
    }

    private enum ArrangeResult
    {
        Arranged,
        NotVisible,
        AncestorMeasureInvalid,
    }
}
