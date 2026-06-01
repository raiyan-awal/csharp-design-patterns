namespace AdapterPattern.ThirdParty;

// ============================================================
// ADAPTEE #1 — Stripe SDK (simulated)
// ============================================================
// Imagine this class comes from the NuGet package "Stripe.net".
// You cannot modify it. Its interface is completely different
// from IPaymentProcessor:
//   • Uses decimal amounts (dollars, not cents)
//   • Splits currency out of the amount struct
//   • Returns its own StripeCharge / StripeRefund objects
//   • Method names follow Stripe's own naming conventions
//
// Your Adapter's job: translate between this and IPaymentProcessor.

/// <summary>
/// Simulates the Stripe .NET SDK. Pretend this is a sealed class
/// from a NuGet package — you cannot change it.
/// </summary>
public class StripeGateway
{
    private readonly string _secretKey;
    private static int _txCounter = 1000;

    public StripeGateway(string secretKey)
    {
        _secretKey = secretKey;
    }

    // Stripe charges in decimal dollars, not integer cents.
    // It returns its own result type, not yours.
    public StripeCharge CreateCharge(string source, decimal amount, string currency)
    {
        // Simulate Stripe's API call
        if (string.IsNullOrWhiteSpace(source))
            return new StripeCharge { Paid = false, FailureMessage = "No card source provided" };

        var txId = $"ch_stripe_{++_txCounter}";
        return new StripeCharge
        {
            Id             = txId,
            Paid           = true,
            Amount         = amount,
            Currency       = currency.ToLowerInvariant(),
            FailureMessage = null
        };
    }

    // Stripe refunds use a different method name and return type.
    public StripeRefund CreateRefund(string chargeId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
            return new StripeRefund { Status = "failed", Reason = "Invalid charge ID" };

        return new StripeRefund
        {
            Id     = $"re_stripe_{chargeId}",
            Status = "succeeded",
            Amount = amount,
            Reason = null
        };
    }
}

/// <summary>Stripe's own charge result object.</summary>
public class StripeCharge
{
    public string Id             { get; init; } = string.Empty;
    public bool   Paid           { get; init; }
    public decimal Amount        { get; init; }
    public string Currency       { get; init; } = string.Empty;
    public string? FailureMessage { get; init; }
}

/// <summary>Stripe's own refund result object.</summary>
public class StripeRefund
{
    public string  Id     { get; init; } = string.Empty;
    public string  Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Reason { get; init; }
}
