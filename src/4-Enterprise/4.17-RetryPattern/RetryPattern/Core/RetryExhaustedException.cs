namespace RetryPattern.Core;

public sealed class RetryExhaustedException : Exception
{
    public int Attempts { get; }

    public RetryExhaustedException(string message, int attempts, Exception innerException)
        : base(message, innerException)
    {
        Attempts = attempts;
    }
}
