namespace NewBeeVG;

public readonly struct NBDisposableIf : IDisposable
{
    private readonly IDisposable? _owner;

    private readonly Func<bool>? _func;

    public NBDisposableIf(IDisposable? owner, Func<bool>? predicate = null)
    {
        this._owner = owner;
        _func = predicate;
    }

    public void Dispose()
    {
        if (_func == null || _func() == true)
        {
            _owner?.Dispose();
        }
    }
}

public readonly struct NBDefer : IDisposable
{
    private readonly Action action;
    public NBDefer(Action action)
    {
        this.action = action;
    }

    public void Dispose()
    {
        action();
    }
}
