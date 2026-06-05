namespace DecoratorPattern;

// Retries the inner Send() up to maxAttempts times on exception.
// Useful when the inner channel is a real email/SMS service that
// can fail transiently. Throws InvalidOperationException if all
// attempts are exhausted, wrapping the last exception as InnerException.
public sealed class RetryDecorator : NotifierDecorator
{
    private readonly int _maxAttempts;
    private readonly Action<string> _retryLog;

    public RetryDecorator(INotifier inner, int maxAttempts = 3, Action<string>? retryLog = null)
        : base(inner)
    {
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be at least 1.");
        _maxAttempts = maxAttempts;
        _retryLog    = retryLog ?? Console.WriteLine;
    }

    public override void Send(string recipient, string subject, string body)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                _inner.Send(recipient, subject, body);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _retryLog($"[RETRY] Attempt {attempt}/{_maxAttempts} failed: {ex.Message}");
            }
        }

        throw new InvalidOperationException(
            $"Notification failed after {_maxAttempts} attempt(s).", lastException);
    }
}
