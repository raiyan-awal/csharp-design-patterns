namespace RetryPattern.Core;

public sealed class RetryOptions
{
    public int MaxAttempts { get; init; } = 3;
    public Func<int, TimeSpan>             DelayStrategy { get; init; } = Core.DelayStrategy.Fixed(TimeSpan.FromSeconds(1));
    public Func<Exception, bool>           ShouldRetry   { get; init; } = _ => true;
    public Action<Exception, int, TimeSpan>? OnRetry     { get; init; }
}
