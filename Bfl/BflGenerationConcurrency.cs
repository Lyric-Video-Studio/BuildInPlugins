namespace BflTxtToImgPlugin
{
    /// <summary>
    /// Limits active BFL generations across image and video plugin instances.
    /// A slot is held until polling/download completes, not just until submission.
    /// </summary>
    internal static class BflGenerationConcurrency
    {
        private const int DefaultLimit = 8;
        private const int MaximumLimit = 64;
        private static readonly object Sync = new();
        private static int limit = DefaultLimit;
        private static int running;
        private static TaskCompletionSource<bool> changed = NewSignal();

        public static void Configure(int concurrencyLimit)
        {
            var nextLimit = concurrencyLimit <= 0 ? DefaultLimit : Math.Min(concurrencyLimit, MaximumLimit);
            lock (Sync)
            {
                if (limit == nextLimit)
                {
                    return;
                }

                limit = nextLimit;
                SignalChange();
            }
        }

        public static async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken, Action onWaiting = null)
        {
            var waitingReported = false;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Task signal;
                lock (Sync)
                {
                    if (running < limit)
                    {
                        running++;
                        return new Lease();
                    }

                    // The signal is obtained while holding the same lock as Release,
                    // so no wakeup is lost between checking capacity and waiting.
                    signal = changed.Task;
                }

                if (!waitingReported)
                {
                    onWaiting?.Invoke();
                    waitingReported = true;
                }

                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static void Release()
        {
            lock (Sync)
            {
                running--;
                SignalChange();
            }
        }

        // Caller must hold Sync. RunContinuationsAsynchronously prevents inline
        // re-entrance into the lock when many waiting generations are released.
        private static void SignalChange()
        {
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult(true);
        }

        private static TaskCompletionSource<bool> NewSignal() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private sealed class Lease : IDisposable
        {
            private int disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) == 0)
                {
                    Release();
                }
            }
        }
    }
}
