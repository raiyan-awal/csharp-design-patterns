namespace AdapterPattern;

// ============================================================
// TARGET INTERFACE
// ============================================================
// This is YOUR application's payment abstraction — the interface
// your code depends on. Every payment gateway in your system must
// conform to this contract, regardless of how the underlying
// third-party SDK actually works.
//
// The Adapter pattern's job is to make incompatible external APIs
// look exactly like this interface to the rest of your application.

/// <summary>
/// The target interface your application code depends on.
/// All payment processors — regardless of their underlying SDK —
/// must satisfy this contract.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Charges the given amount (in cents) to the specified card token.
    /// </summary>
    /// <param name="cardToken">Tokenised card reference from the payment gateway.</param>
    /// <param name="amountInCents">Amount to charge, e.g. 4999 = $49.99.</param>
    /// <param name="currency">ISO 4217 currency code, e.g. "USD".</param>
    /// <returns>A <see cref="PaymentResult"/> indicating success or failure.</returns>
    PaymentResult Charge(string cardToken, int amountInCents, string currency);

    /// <summary>
    /// Refunds a previously successful charge.
    /// </summary>
    /// <param name="transactionId">The transaction ID returned by the original charge.</param>
    /// <param name="amountInCents">Amount to refund. Must be ≤ original charge amount.</param>
    /// <returns>A <see cref="PaymentResult"/> indicating success or failure.</returns>
    PaymentResult Refund(string transactionId, int amountInCents);

    /// <summary>
    /// Returns the human-readable name of this payment processor.
    /// Useful for logging and diagnostics.
    /// </summary>
    string ProcessorName { get; }
}
