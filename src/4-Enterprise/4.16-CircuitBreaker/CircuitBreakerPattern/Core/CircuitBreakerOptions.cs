namespace CircuitBreakerPattern.Core;

public sealed class CircuitBreakerOptions
{
    public int      FailureThreshold { get; init; } = 3;
    public int      SuccessThreshold { get; init; } = 2;
    public TimeSpan ResetTimeout     { get; init; } = TimeSpan.FromSeconds(30);
}
