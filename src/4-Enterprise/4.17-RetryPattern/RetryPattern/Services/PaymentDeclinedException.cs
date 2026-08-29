namespace RetryPattern.Services;

public sealed class PaymentDeclinedException : Exception
{
    public PaymentDeclinedException(string reason) : base(reason) { }
}
