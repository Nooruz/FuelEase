namespace KIT.GasStation.FuelDispenser.Services
{
    public sealed class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public AsyncManualResetEvent(bool initialState = false)
        {
            if (initialState) _tcs.TrySetResult(true);
        }

        public Task WaitAsync(CancellationToken ct = default)
        {
            var task = _tcs.Task;
            return task.IsCompleted ? Task.CompletedTask : task.WaitAsync(ct);
        }

        public void Set() => _tcs.TrySetResult(true);

        public void Reset()
        {
            while (true)
            {
                var tcs = _tcs;
                if (!tcs.Task.IsCompleted) return;
                var newTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (Interlocked.CompareExchange(ref _tcs, newTcs, tcs) == tcs) return;
            }
        }
    }
}
