using AdapterPattern.ThirdParty;

namespace AdapterPattern;

// ============================================================
// ADAPTER #2 — PayPal
// ============================================================
// Wraps PayPalSdk (the adaptee) and translates its interface
// into IPaymentProcessor (the target interface).
//
// Translation responsibilities:
//   • amountInCents (int)           →  string "49.99" (÷ 100, formatted)
//   • cardToken + amount + currency →  PayPalPaymentOrder (request object)
//   • PayPalOrderResult             →  PaymentResult
//   • PayPalRefundResult            →  PaymentResult

/// <summary>
/// Adapts <see cref="PayPalSdk"/> to the <see cref="IPaymentProcessor"/>
/// interface expected by the application.
/// </summary>
public sealed class PayPalAdapter : IPaymentProcessor
{
    private readonly PayPalSdk _paypal;

    public PayPalAdapter(PayPalSdk paypal)
    {
        _paypal = paypal;
    }

    public string ProcessorName => "PayPal";

    public PaymentResult Charge(string cardToken, int amountInCents, string currency)
    {
        // ── TRANSLATION: build PayPal's request object ────────
        // PayPal requires a PayPalPaymentOrder — we construct it
        // from the flat parameters the target interface provides.
        decimal amountInDollars = amountInCents / 100m;

        var order = new PayPalPaymentOrder
        {
            CardToken    = cardToken,
            Amount       = amountInDollars.ToString("F2"),  // "49.99"
            CurrencyCode = currency.ToUpperInvariant()
        };

        // ── DELEGATE ─────────────────────────────────────────
        PayPalOrderResult result = _paypal.ExecutePayment(order);

        // ── TRANSLATE the result ──────────────────────────────
        return result.Status == PayPalStatus.Completed
            ? PaymentResult.Ok(result.OrderId, $"PayPal charged {currency} {amountInDollars:F2}")
            : PaymentResult.Fail($"PayPal declined — error code: {result.ErrorCode}");
    }

    public PaymentResult Refund(string transactionId, int amountInCents)
    {
        decimal amountInDollars = amountInCents / 100m;
        string  amountStr       = amountInDollars.ToString("F2");

        // PayPal calls refunds "ReverseCapture" — pure naming translation.
        PayPalRefundResult result = _paypal.ReverseCapture(transactionId, amountStr, "USD");

        return result.Reversed
            ? PaymentResult.Ok(result.RefundId, $"PayPal refunded {amountInDollars:F2}")
            : PaymentResult.Fail($"PayPal refund failed: {result.Message}");
    }
}
