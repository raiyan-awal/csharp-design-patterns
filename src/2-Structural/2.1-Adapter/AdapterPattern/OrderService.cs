namespace AdapterPattern;

// ============================================================
// CLIENT
// ============================================================
// OrderService is the "client" in Adapter pattern terminology.
// It depends ONLY on IPaymentProcessor — the target interface.
//
// It has absolutely no knowledge of Stripe, PayPal, or Square.
// You could swap the underlying gateway at runtime (e.g. A/B test,
// failover, region-based routing) and OrderService wouldn't change
// a single line.
//
// This is the key benefit of the Adapter pattern:
//   Your application code stays stable. Third-party changes are
//   absorbed entirely inside the adapter class.

/// <summary>
/// Application-level service that processes customer orders.
/// Depends only on <see cref="IPaymentProcessor"/> — completely
/// unaware of any specific payment gateway.
/// </summary>
public sealed class OrderService
{
    private readonly IPaymentProcessor _processor;

    // Dependency injected — the adapter is provided from outside.
    // OrderService doesn't know or care which gateway is injected.
    public OrderService(IPaymentProcessor processor)
    {
        _processor = processor;
    }

    /// <summary>
    /// Processes a purchase by charging the given card.
    /// </summary>
    public string PlaceOrder(string orderId, string cardToken, int amountInCents, string currency)
    {
        Console.WriteLine($"  [OrderService] Placing order {orderId} via {_processor.ProcessorName}...");

        PaymentResult result = _processor.Charge(cardToken, amountInCents, currency);

        if (result.Success)
        {
            Console.WriteLine($"  [OrderService] {result}");
            return result.TransactionId;
        }

        Console.WriteLine($"  [OrderService] {result}");
        throw new InvalidOperationException($"Payment failed for order {orderId}: {result.Message}");
    }

    /// <summary>
    /// Cancels a previously placed order and refunds the charge.
    /// </summary>
    public void CancelOrder(string orderId, string transactionId, int amountInCents)
    {
        Console.WriteLine($"  [OrderService] Cancelling order {orderId} via {_processor.ProcessorName}...");

        PaymentResult result = _processor.Refund(transactionId, amountInCents);

        Console.WriteLine($"  [OrderService] {result}");

        if (!result.Success)
            throw new InvalidOperationException($"Refund failed for order {orderId}: {result.Message}");
    }
}
