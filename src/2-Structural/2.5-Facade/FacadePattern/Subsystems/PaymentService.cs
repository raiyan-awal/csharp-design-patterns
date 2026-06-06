namespace FacadePattern;

// ============================================================
// SUBSYSTEM: Payment
// ============================================================
// Handles charging and refunding cards. Has no knowledge of
// inventory, shipping, or any other subsystem.
//
// Convention: card tokens starting with "fail-" are always
// declined — useful for testing failure paths.

public sealed record PaymentResult(bool Success, string? TransactionId, string Message);

public sealed class PaymentService
{
    public int ChargeCallCount { get; private set; }

    public PaymentResult Charge(string cardToken, decimal amount)
    {
        ChargeCallCount++;
        Console.WriteLine($"[PAYMENT] Charging {amount:C2} to card '{cardToken}'");

        if (cardToken.StartsWith("fail-"))
        {
            Console.WriteLine("[PAYMENT] Card declined.");
            return new PaymentResult(false, null, "Card declined by issuer.");
        }

        string transactionId = $"TXN-{Guid.NewGuid():N}"[..16].ToUpper();
        Console.WriteLine($"[PAYMENT] Charged successfully. Transaction: {transactionId}");
        return new PaymentResult(true, transactionId, "Charged successfully.");
    }

    public void Refund(string transactionId, decimal amount)
        => Console.WriteLine($"[PAYMENT] Refunding {amount:C2} for transaction {transactionId}");
}
