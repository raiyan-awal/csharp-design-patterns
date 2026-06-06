namespace FacadePattern;

public sealed record OrderResult(
    bool Success,
    string OrderId,
    string? TrackingNumber,
    string? TransactionId,
    string Message)
{
    public static OrderResult Ok(string orderId, string tracking, string transactionId, string message)
        => new(true, orderId, tracking, transactionId, message);

    public static OrderResult Fail(string orderId, string message)
        => new(false, orderId, null, null, message);
}
