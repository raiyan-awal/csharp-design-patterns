namespace CircuitBreakerPattern.Core;

public sealed class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly Func<DateTime>        _utcNow;
    private readonly Lock                  _lock = new();

    private CircuitState _state        = CircuitState.Closed;
    private int          _failureCount = 0;
    private int          _successCount = 0;
    private DateTime?    _openedAt     = null;

    public CircuitState State        => _state;
    public int          FailureCount => _failureCount;
    public int          SuccessCount => _successCount;

    public CircuitBreaker(CircuitBreakerOptions options, Func<DateTime>? utcNow = null)
    {
        _options = options;
        _utcNow  = utcNow ?? (() => DateTime.UtcNow);
    }

    public T Execute<T>(Func<T> action)
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                if (_utcNow() - _openedAt!.Value >= _options.ResetTimeout)
                    TransitionTo(CircuitState.HalfOpen);
                else
                    throw new CircuitBreakerOpenException(
                        $"Circuit is Open — service unavailable. " +
                        $"Retry after {_options.ResetTimeout.TotalSeconds}s.");
            }
        }

        try
        {
            var result = action();
            lock (_lock) { OnSuccess(); }
            return result;
        }
        catch (Exception)
        {
            lock (_lock) { OnFailure(); }
            throw;
        }
    }

    private void OnSuccess()
    {
        _failureCount = 0;

        if (_state == CircuitState.HalfOpen)
        {
            _successCount++;
            if (_successCount >= _options.SuccessThreshold)
                TransitionTo(CircuitState.Closed);
        }
    }

    private void OnFailure()
    {
        _failureCount++;

        if (_state == CircuitState.HalfOpen || _failureCount >= _options.FailureThreshold)
            TransitionTo(CircuitState.Open);
    }

    private void TransitionTo(CircuitState newState)
    {
        _state = newState;
        switch (newState)
        {
            case CircuitState.Open:
                _openedAt     = _utcNow();
                _successCount = 0;
                break;
            case CircuitState.HalfOpen:
                _successCount = 0;
                break;
            case CircuitState.Closed:
                _failureCount = 0;
                _successCount = 0;
                _openedAt     = null;
                break;
        }
    }
}
