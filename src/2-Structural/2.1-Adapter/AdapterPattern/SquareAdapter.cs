using AdapterPattern.ThirdParty;

namespace AdapterPattern;

// ============================================================
// ADAPTER #3 — Square
// ============================================================
// Wraps SquareApi (the adaptee) and translates its interface
// into IPaymentProcessor (the target interface).
//
// Translation responsibilities:
//   • amountInCents (int) + currency  →  Money struct
//   • cardToken                        →  sourceId (same value, different name)
//   • idempotency key                  →  generated internally
//   • ApiResponse<SquarePayment>       →  PaymentResult
//   • ApiResponse<SquareRefund>        →  PaymentResult

/// <summary>
/// Adapts <see cref="SquareApi"/> to the <see cref="IPaymentProcessor"/>
/// interface expected by the application.
/// </summary>
public sealed class SquareAdapter : IPaymentProcessor
{
    private readonly SquareApi _square;

    public SquareAdapter(SquareApi square)
    {
        _square = square;
    }

    public string ProcessorName => "Square";

    public PaymentResult Charge(string cardToken, int amountInCents, string currency)
    {
        // ── TRANSLATION: build Square's Money struct ──────────
        // Square keeps cents + currency together in a struct.
        var money = new Money { Amount = amountInCents, Currency = currency.ToUpperInvariant() };

        // Square requires an idempotency key; generate one internally.
        // The caller doesn't need to know about this detail.
        string idempotencyKey = Guid.NewGuid().ToString();

        // ── DELEGATE ─────────────────────────────────────────
        // Square calls the card a "sourceId" — same value, different name.
        ApiResponse<SquarePayment> response = _square.CreatePayment(cardToken, money, idempotencyKey);

        // ── TRANSLATE the result ──────────────────────────────
        return response.IsSuccess && response.Data is not null
            ? PaymentResult.Ok(response.Data.Id,
                $"Square charged {currency} {amountInCents / 100m:F2}")
            : PaymentResult.Fail($"Square declined: {response.ErrorCode} — {response.ErrorMessage}");
    }

    public PaymentResult Refund(string transactionId, int amountInCents)
    {
        var    money          = new Money { Amount = amountInCents, Currency = "USD" };
        string idempotencyKey = Guid.NewGuid().ToString();

        ApiResponse<SquareRefund> response = _square.RefundPayment(transactionId, money, idempotencyKey);

        return response.IsSuccess && response.Data is not null
            ? PaymentResult.Ok(response.Data.Id,
                $"Square refunded {amountInCents / 100m:F2}")
            : PaymentResult.Fail($"Square refund failed: {response.ErrorCode} — {response.ErrorMessage}");
    }
}
