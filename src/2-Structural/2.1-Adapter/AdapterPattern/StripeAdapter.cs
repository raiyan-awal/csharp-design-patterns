using AdapterPattern.ThirdParty;

namespace AdapterPattern;

// ============================================================
// ADAPTER #1 — Stripe
// ============================================================
// Wraps StripeGateway (the adaptee) and translates its interface
// into IPaymentProcessor (the target interface).
//
// Translation responsibilities:
//   • amountInCents (int)  →  decimal dollars  (÷ 100)
//   • StripeCharge         →  PaymentResult
//   • StripeRefund         →  PaymentResult
//
// The rest of the application only ever sees IPaymentProcessor.
// StripeGateway is hidden entirely inside this class.

/// <summary>
/// Adapts <see cref="StripeGateway"/> to the <see cref="IPaymentProcessor"/>
/// interface expected by the application.
/// </summary>
public sealed class StripeAdapter : IPaymentProcessor
{
    private readonly StripeGateway _stripe;

    // We receive the adaptee via constructor injection.
    // The caller configures it; we just wrap it.
    public StripeAdapter(StripeGateway stripe)
    {
        _stripe = stripe;
    }

    public string ProcessorName => "Stripe";

    public PaymentResult Charge(string cardToken, int amountInCents, string currency)
    {
        // ── TRANSLATION: int cents → decimal dollars ──────────
        // Stripe expects dollars as a decimal (e.g. 4999 → 49.99m).
        decimal amountInDollars = amountInCents / 100m;

        // ── DELEGATE to the adaptee ───────────────────────────
        StripeCharge charge = _stripe.CreateCharge(cardToken, amountInDollars, currency);

        // ── TRANSLATE the result back to our type ─────────────
        return charge.Paid
            ? PaymentResult.Ok(charge.Id, $"Stripe charged {currency} {amountInDollars:F2}")
            : PaymentResult.Fail($"Stripe declined: {charge.FailureMessage}");
    }

    public PaymentResult Refund(string transactionId, int amountInCents)
    {
        decimal amountInDollars = amountInCents / 100m;

        StripeRefund refund = _stripe.CreateRefund(transactionId, amountInDollars);

        return refund.Status == "succeeded"
            ? PaymentResult.Ok(refund.Id, $"Stripe refunded {amountInDollars:F2}")
            : PaymentResult.Fail($"Stripe refund failed: {refund.Reason}");
    }
}
