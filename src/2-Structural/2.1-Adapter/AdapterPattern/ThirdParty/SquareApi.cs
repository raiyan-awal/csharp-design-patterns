namespace AdapterPattern.ThirdParty;

// ============================================================
// ADAPTEE #3 — Square API (simulated)
// ============================================================
// Square is different yet again:
//   • Async-style but blocking (returns a result wrapper)
//   • Amount uses a "Money" struct (value + currency together)
//   • Identifies cards by "sourceId" not "cardToken"
//   • Returns a generic ApiResponse<T> wrapper
//   • Refunds are called "refunds" but use a different payload

/// <summary>
/// Simulates the Square .NET SDK. Third incompatible interface.
/// </summary>
public class SquareApi
{
    private readonly string _accessToken;
    private static int _paymentCounter = 9000;

    public SquareApi(string accessToken)
    {
        _accessToken = accessToken;
    }

    // Square uses a "Money" struct and calls the card a "sourceId".
    public ApiResponse<SquarePayment> CreatePayment(string sourceId, Money amount, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return ApiResponse<SquarePayment>.Failure("SOURCE_REQUIRED", "sourceId is required");

        var payment = new SquarePayment
        {
            Id       = $"SQ_{++_paymentCounter}",
            SourceId = sourceId,
            Amount   = amount,
            Status   = "COMPLETED"
        };
        return ApiResponse<SquarePayment>.Success(payment);
    }

    public ApiResponse<SquareRefund> RefundPayment(string paymentId, Money amount, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return ApiResponse<SquareRefund>.Failure("PAYMENT_ID_REQUIRED", "paymentId is required");

        var refund = new SquareRefund
        {
            Id        = $"SQR_{paymentId}",
            PaymentId = paymentId,
            Amount    = amount,
            Status    = "COMPLETED"
        };
        return ApiResponse<SquareRefund>.Success(refund);
    }
}

// ── Square's own types ────────────────────────────────────────

/// <summary>Square uses a Money struct: amount in smallest unit + currency.</summary>
public readonly struct Money
{
    public int    Amount   { get; init; }  // smallest currency unit (cents)
    public string Currency { get; init; }  // ISO 4217
}

public class SquarePayment
{
    public string Id       { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public Money  Amount   { get; init; }
    public string Status   { get; init; } = string.Empty;
}

public class SquareRefund
{
    public string Id        { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public Money  Amount    { get; init; }
    public string Status    { get; init; } = string.Empty;
}

/// <summary>
/// Generic response wrapper Square uses for all API calls.
/// </summary>
public class ApiResponse<T>
{
    public bool    IsSuccess  { get; private init; }
    public T?      Data       { get; private init; }
    public string? ErrorCode  { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ApiResponse<T> Success(T data) => new()
        { IsSuccess = true, Data = data };

    public static ApiResponse<T> Failure(string code, string message) => new()
        { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
}
