namespace RetryPattern.Core;

public static class DelayStrategy
{
    public static Func<int, TimeSpan> Fixed(TimeSpan delay) => _ => delay;

    public static Func<int, TimeSpan> Exponential(TimeSpan baseDelay) =>
        attempt => TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

    public static Func<int, TimeSpan> ExponentialWithJitter(TimeSpan baseDelay, Random? random = null)
    {
        var rng = random ?? Random.Shared;
        return attempt =>
        {
            var exponential = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
            var jitter      = (rng.NextDouble() - 0.5) * exponential * 0.5;
            return TimeSpan.FromMilliseconds(Math.Max(0, exponential + jitter));
        };
    }
}
