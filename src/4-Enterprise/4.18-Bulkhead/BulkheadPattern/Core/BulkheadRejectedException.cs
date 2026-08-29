namespace BulkheadPattern.Core;

public sealed class BulkheadRejectedException : Exception
{
    public BulkheadRejectedException(string message) : base(message) { }
}
