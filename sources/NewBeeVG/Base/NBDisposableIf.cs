namespace NewBeeVG;

public class NBDisposableIf : IDisposable
{
    private IDisposable? _owner;

    private Func<bool>? _func;

    public NBDisposableIf(IDisposable? owner, Func<bool>? predicate = null)
    {
        this._owner = owner;
        _func = predicate;
    }

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if(_func == null || _func() == true)
                {
                    _owner?.Dispose();
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class NBDefer : IDisposable
{
    private bool disposedValue;
    private Action action;
    public NBDefer(Action action)
    {
        this.action = action;
    }

    public void Dispose()
    {
        if (!disposedValue)
        {
            disposedValue = true;
            action();
        }
    }
}
