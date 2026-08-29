namespace RetryPattern.Core;

public sealed class RetryPolicy
{
    private readonly RetryOptions     _options;
    private readonly Action<TimeSpan> _sleep;

    public RetryPolicy(RetryOptions options, Action<TimeSpan>? sleep = null)
    {
        _options = options;
        _sleep   = sleep ?? (ts => Thread.Sleep(ts));
    }

    public T Execute<T>(Func<T> action)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (_options.ShouldRetry(ex))
            {
                lastException = ex;

                if (attempt == _options.MaxAttempts)
                    break;

                var delay = _options.DelayStrategy(attempt);
                _options.OnRetry?.Invoke(ex, attempt, delay);
                _sleep(delay);
            }
            // Non-retryable exceptions propagate naturally — the when clause evaluates false
        }

        throw new RetryExhaustedException(
            $"Operation failed after {_options.MaxAttempts} attempt(s).",
            _options.MaxAttempts,
            lastException!);
    }
}
