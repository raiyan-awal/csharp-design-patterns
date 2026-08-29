namespace RetryPattern.Services;

public sealed record PaymentResult(string TransactionId, decimal AmountCAD, string Status);
