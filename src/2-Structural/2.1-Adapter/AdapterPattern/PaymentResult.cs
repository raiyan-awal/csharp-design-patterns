namespace AdapterPattern;

// ============================================================
// SHARED RESULT TYPE
// ============================================================
// A simple value object returned by every IPaymentProcessor call.
// Keeps the target interface clean — callers get a uniform result
// shape no matter which adapter/gateway is underneath.

/// <summary>
/// Represents the outcome of a charge or refund operation.
/// </summary>
public sealed class PaymentResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; }

    /// <summary>
    /// Gateway-assigned transaction identifier on success,
    /// or an empty string on failure.
    /// </summary>
    public string TransactionId { get; }

    /// <summary>
    /// Human-readable message describing the outcome.
    /// On failure this contains the error reason.
    /// </summary>
    public string Message { get; }

    private PaymentResult(bool success, string transactionId, string message)
    {
        Success       = success;
        TransactionId = transactionId;
        Message       = message;
    }

    // ── Factory helpers keep construction readable ────────────

    public static PaymentResult Ok(string transactionId, string message)
        => new(true, transactionId, message);

    public static PaymentResult Fail(string message)
        => new(false, string.Empty, message);

    public override string ToString()
        => Success
            ? $"✅  Success | TxID: {TransactionId} | {Message}"
            : $"❌  Failed  | {Message}";
}
