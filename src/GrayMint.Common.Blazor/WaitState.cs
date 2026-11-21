namespace GrayMint.Common.Blazor;
public class WaitState
{
    private int _counter;
    public bool IsBusy => _counter > 0;
    public event EventHandler? IsBusyChanged;

    public IDisposable Start(string? message = null)
    {
        Message = message;
        return new Worker(this);
    }

    public string? Message { get; set; }

    private class Worker : IDisposable
    {
        private readonly WaitState _workState;

        public Worker(WaitState workState)
        {
            _workState = workState;
            Interlocked.Increment(ref workState._counter);
            if (workState._counter == 1) workState.IsBusyChanged?.Invoke(workState, EventArgs.Empty);
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Interlocked.Decrement(ref _workState._counter);
            if (_workState._counter == 0) _workState.IsBusyChanged?.Invoke(_workState, EventArgs.Empty);
        }
    }
}