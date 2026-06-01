namespace AdapterPattern.ThirdParty;

// ============================================================
// ADAPTEE #2 — PayPal SDK (simulated)
// ============================================================
// PayPal's SDK looks completely different again:
//   • Amounts are strings formatted as "49.99" (not int cents)
//   • Uses a "PaymentOrder" request object
//   • Returns a status code enum instead of a success/fail object
//   • Refunds are called "Disputes" in PayPal's model
//
// Another sealed third-party class you cannot modify.

/// <summary>
/// Simulates the PayPal .NET SDK. Another third-party class
/// with its own incompatible interface.
/// </summary>
public class PayPalSdk
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private static int _orderCounter = 5000;

    public PayPalSdk(string clientId, string clientSecret)
    {
        _clientId     = clientId;
        _clientSecret = clientSecret;
    }

    // PayPal uses a request object and amount as a string.
    public PayPalOrderResult ExecutePayment(PayPalPaymentOrder order)
    {
        if (order is null || string.IsNullOrWhiteSpace(order.CardToken))
            return new PayPalOrderResult
            {
                Status    = PayPalStatus.Declined,
                OrderId   = string.Empty,
                ErrorCode = "INVALID_REQUEST"
            };

        return new PayPalOrderResult
        {
            Status  = PayPalStatus.Completed,
            OrderId = $"PAYPAL-{++_orderCounter}",
            ErrorCode = null
        };
    }

    // PayPal calls refunds "disputes" / "captures reversal".
    public PayPalRefundResult ReverseCapture(string orderId, string amount, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return new PayPalRefundResult { Reversed = false, Message = "Order not found" };

        return new PayPalRefundResult
        {
            Reversed  = true,
            RefundId  = $"REF-{orderId}",
            Message   = "Capture reversed successfully"
        };
    }
}

// ── PayPal's own request/response types ──────────────────────

public class PayPalPaymentOrder
{
    public string CardToken    { get; set; } = string.Empty;
    public string Amount       { get; set; } = string.Empty; // e.g. "49.99"
    public string CurrencyCode { get; set; } = string.Empty;
}

public enum PayPalStatus { Completed, Declined, Pending }

public class PayPalOrderResult
{
    public PayPalStatus Status    { get; init; }
    public string       OrderId   { get; init; } = string.Empty;
    public string?      ErrorCode { get; init; }
}

public class PayPalRefundResult
{
    public bool    Reversed { get; init; }
    public string  RefundId { get; init; } = string.Empty;
    public string  Message  { get; init; } = string.Empty;
}
