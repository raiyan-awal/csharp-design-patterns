namespace BulkheadPattern.Core;

public sealed class BulkheadPolicy
{
    private readonly BulkheadOptions _options;
    private readonly SemaphoreSlim   _semaphore;
    private int _queuedCount = 0;

    public int Available => _semaphore.CurrentCount;
    public int Queued    => Volatile.Read(ref _queuedCount);

    public BulkheadPolicy(BulkheadOptions options)
    {
        _options   = options;
        _semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
    }

    public T Execute<T>(Func<T> action)
    {
        if (_options.MaxQueueSize == 0)
        {
            // No queue: reject immediately if all slots are busy
            if (!_semaphore.Wait(TimeSpan.Zero))
                throw new BulkheadRejectedException(
                    $"Bulkhead saturated — all {_options.MaxConcurrency} execution slot(s) are busy.");
        }
        else
        {
            // Fast path: acquire without queuing if a slot is available
            if (!_semaphore.Wait(TimeSpan.Zero))
            {
                // No slot available — try to enter the queue
                var position = Interlocked.Increment(ref _queuedCount);
                if (position > _options.MaxQueueSize)
                {
                    Interlocked.Decrement(ref _queuedCount);
                    throw new BulkheadRejectedException(
                        $"Bulkhead queue full — {_options.MaxQueueSize} caller(s) already waiting.");
                }

                try
                {
                    if (!_semaphore.Wait(_options.QueueTimeout))
                        throw new BulkheadRejectedException(
                            "Bulkhead queue timeout — caller waited too long.");
                }
                finally
                {
                    Interlocked.Decrement(ref _queuedCount);
                }
            }
        }

        try   { return action(); }
        finally { _semaphore.Release(); }
    }
}
