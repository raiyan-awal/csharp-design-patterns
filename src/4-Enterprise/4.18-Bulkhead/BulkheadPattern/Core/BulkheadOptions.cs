namespace BulkheadPattern.Core;

public sealed class BulkheadOptions
{
    public int      MaxConcurrency { get; init; } = 10;
    public int      MaxQueueSize   { get; init; } = 0;
    public TimeSpan QueueTimeout   { get; init; } = TimeSpan.FromSeconds(5);
}
