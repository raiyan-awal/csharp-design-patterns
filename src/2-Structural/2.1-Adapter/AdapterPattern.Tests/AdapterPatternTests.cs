using AdapterPattern;
using AdapterPattern.ThirdParty;

namespace AdapterPattern.Tests;

// ============================================================
// ADAPTER PATTERN TESTS
// ============================================================
// Tests focus on the adapter translation layer:
//   • Does a successful adaptee response produce PaymentResult.Ok?
//   • Does a failed adaptee response produce PaymentResult.Fail?
//   • Are cents correctly converted to dollars for Stripe/PayPal?
//   • Are transaction IDs passed through correctly?
//   • Does OrderService stay decoupled (only IPaymentProcessor used)?
// ============================================================

public class StripeAdapterTests
{
    private static StripeAdapter CreateAdapter() =>
        new(new StripeGateway("sk_test_key"));

    // ── ProcessorName ─────────────────────────────────────────

    [Fact]
    public void ProcessorName_ReturnsStripe()
    {
        var adapter = CreateAdapter();
        Assert.Equal("Stripe", adapter.ProcessorName);
    }

    // ── Charge — success ──────────────────────────────────────

    [Fact]
    public void Charge_ValidToken_ReturnsSuccess()
    {
        var adapter = CreateAdapter();
        PaymentResult result = adapter.Charge("tok_visa", 4999, "USD");

        Assert.True(result.Success);
    }

    [Fact]
    public void Charge_ValidToken_TransactionIdStartsWithStripePrefix()
    {
        var adapter = CreateAdapter();
        PaymentResult result = adapter.Charge("tok_visa", 4999, "USD");

        Assert.StartsWith("ch_stripe_", result.TransactionId);
    }

    [Fact]
    public void Charge_AmountConvertedCorrectly_MessageContainsDollarAmount()
    {
        var adapter = CreateAdapter();
        // 4999 cents = $49.99
        PaymentResult result = adapter.Charge("tok_visa", 4999, "USD");

        Assert.Contains("49.99", result.Message);
    }

    [Fact]
    public void Charge_ZeroCents_ConvertsToZeroDollars()
    {
        var adapter = CreateAdapter();
        PaymentResult result = adapter.Charge("tok_visa", 0, "USD");

        // 0 cents = $0.00 — should still succeed
        Assert.True(result.Success);
        Assert.Contains("0.00", result.Message);
    }

    // ── Charge — failure ──────────────────────────────────────

    [Fact]
    public void Charge_EmptyToken_ReturnsFail()
    {
        var adapter = CreateAdapter();
        PaymentResult result = adapter.Charge(string.Empty, 4999, "USD");

        Assert.False(result.Success);
    }

    [Fact]
    public void Charge_EmptyToken_TransactionIdIsEmpty()
    {
        var adapter = CreateAdapter();
        PaymentResult result = adapter.Charge(string.Empty, 4999, "USD");

        Assert.Equal(string.Empty, result.TransactionId);
    }

    // ── Refund — success ──────────────────────────────────────

    [Fact]
    public void Refund_ValidTransactionId_ReturnsSuccess()
    {
        var adapter = CreateAdapter();
        PaymentResult chargeResult = adapter.Charge("tok_visa", 4999, "USD");

        PaymentResult refundResult = adapter.Refund(chargeResult.TransactionId, 4999);

        Assert.True(refundResult.Success);
    }

    [Fact]
    public void Refund_ValidTransactionId_RefundIdStartsWithStripePrefix()
    {
        var adapter = CreateAdapter();
        PaymentResult chargeResult = adapter.Charge("tok_visa", 4999, "USD");

        PaymentResult refundResult = adapter.Refund(chargeResult.TransactionId, 4999);

        Assert.StartsWith("re_stripe_", refundResult.TransactionId);
    }

    // ── Refund — failure ──────────────────────────────────────

    [Fact]
    public void Refund_EmptyTransactionId_ReturnsFail()
    {
        var adapter = CreateAdapter();
        PaymentResult result = adapter.Refund(string.Empty, 4999);

        Assert.False(result.Success);
    }
}

// ─────────────────────────────────────────────────────────────

public class PayPalAdapterTests
{
    private static PayPalAdapter CreateAdapter() =>
        new(new PayPalSdk("client_id", "client_secret"));

    [Fact]
    public void ProcessorName_ReturnsPayPal()
    {
        Assert.Equal("PayPal", CreateAdapter().ProcessorName);
    }

    [Fact]
    public void Charge_ValidToken_ReturnsSuccess()
    {
        PaymentResult result = CreateAdapter().Charge("PP_CARD_TOKEN", 12500, "USD");

        Assert.True(result.Success);
    }

    [Fact]
    public void Charge_ValidToken_TransactionIdStartsWithPayPalPrefix()
    {
        PaymentResult result = CreateAdapter().Charge("PP_CARD_TOKEN", 12500, "USD");

        Assert.StartsWith("PAYPAL-", result.TransactionId);
    }

    [Fact]
    public void Charge_AmountConvertedCorrectly_MessageContainsDollarAmount()
    {
        // 12500 cents = $125.00
        PaymentResult result = CreateAdapter().Charge("PP_TOKEN", 12500, "USD");

        Assert.Contains("125.00", result.Message);
    }

    [Fact]
    public void Charge_EmptyToken_ReturnsFail()
    {
        PaymentResult result = CreateAdapter().Charge(string.Empty, 5000, "USD");

        Assert.False(result.Success);
    }

    [Fact]
    public void Refund_ValidTransactionId_ReturnsSuccess()
    {
        var adapter = CreateAdapter();
        PaymentResult charge = adapter.Charge("PP_TOKEN", 5000, "USD");

        PaymentResult refund = adapter.Refund(charge.TransactionId, 5000);

        Assert.True(refund.Success);
    }

    [Fact]
    public void Refund_ValidTransactionId_RefundIdStartsWithRefPrefix()
    {
        var adapter = CreateAdapter();
        PaymentResult charge = adapter.Charge("PP_TOKEN", 5000, "USD");

        PaymentResult refund = adapter.Refund(charge.TransactionId, 5000);

        Assert.StartsWith("REF-", refund.TransactionId);
    }

    [Fact]
    public void Refund_EmptyTransactionId_ReturnsFail()
    {
        PaymentResult result = CreateAdapter().Refund(string.Empty, 5000);

        Assert.False(result.Success);
    }
}

// ─────────────────────────────────────────────────────────────

public class SquareAdapterTests
{
    private static SquareAdapter CreateAdapter() =>
        new(new SquareApi("sq0atp-token"));

    [Fact]
    public void ProcessorName_ReturnsSquare()
    {
        Assert.Equal("Square", CreateAdapter().ProcessorName);
    }

    [Fact]
    public void Charge_ValidSourceId_ReturnsSuccess()
    {
        PaymentResult result = CreateAdapter().Charge("cnon:card-nonce-ok", 7500, "USD");

        Assert.True(result.Success);
    }

    [Fact]
    public void Charge_ValidSourceId_TransactionIdStartsWithSquarePrefix()
    {
        PaymentResult result = CreateAdapter().Charge("cnon:card-nonce-ok", 7500, "USD");

        Assert.StartsWith("SQ_", result.TransactionId);
    }

    [Fact]
    public void Charge_AmountConvertedCorrectly_MessageContainsDollarAmount()
    {
        // 7500 cents = $75.00
        PaymentResult result = CreateAdapter().Charge("cnon:card-nonce-ok", 7500, "USD");

        Assert.Contains("75.00", result.Message);
    }

    [Fact]
    public void Charge_EmptySourceId_ReturnsFail()
    {
        PaymentResult result = CreateAdapter().Charge(string.Empty, 7500, "USD");

        Assert.False(result.Success);
    }

    [Fact]
    public void Refund_ValidTransactionId_ReturnsSuccess()
    {
        var adapter = CreateAdapter();
        PaymentResult charge = adapter.Charge("cnon:card-nonce-ok", 7500, "USD");

        PaymentResult refund = adapter.Refund(charge.TransactionId, 7500);

        Assert.True(refund.Success);
    }

    [Fact]
    public void Refund_ValidTransactionId_RefundIdStartsWithSquareRefundPrefix()
    {
        var adapter = CreateAdapter();
        PaymentResult charge = adapter.Charge("cnon:card-nonce-ok", 7500, "USD");

        PaymentResult refund = adapter.Refund(charge.TransactionId, 7500);

        Assert.StartsWith("SQR_", refund.TransactionId);
    }

    [Fact]
    public void Refund_EmptyTransactionId_ReturnsFail()
    {
        PaymentResult result = CreateAdapter().Refund(string.Empty, 7500);

        Assert.False(result.Success);
    }
}

// ─────────────────────────────────────────────────────────────

public class OrderServiceTests
{
    // ── PlaceOrder ────────────────────────────────────────────

    [Fact]
    public void PlaceOrder_WithStripe_ReturnsTransactionId()
    {
        var service = new OrderService(new StripeAdapter(new StripeGateway("key")));
        string txId = service.PlaceOrder("ORD-001", "tok_visa", 4999, "USD");

        Assert.False(string.IsNullOrWhiteSpace(txId));
    }

    [Fact]
    public void PlaceOrder_WithPayPal_ReturnsTransactionId()
    {
        var service = new OrderService(new PayPalAdapter(new PayPalSdk("id", "secret")));
        string txId = service.PlaceOrder("ORD-002", "PP_TOKEN", 9900, "USD");

        Assert.False(string.IsNullOrWhiteSpace(txId));
    }

    [Fact]
    public void PlaceOrder_WithSquare_ReturnsTransactionId()
    {
        var service = new OrderService(new SquareAdapter(new SquareApi("token")));
        string txId = service.PlaceOrder("ORD-003", "cnon:card-nonce-ok", 5000, "USD");

        Assert.False(string.IsNullOrWhiteSpace(txId));
    }

    [Fact]
    public void PlaceOrder_InvalidCard_ThrowsInvalidOperationException()
    {
        var service = new OrderService(new StripeAdapter(new StripeGateway("key")));

        Assert.Throws<InvalidOperationException>(() =>
            service.PlaceOrder("ORD-004", string.Empty, 4999, "USD"));
    }

    // ── CancelOrder ───────────────────────────────────────────

    [Fact]
    public void CancelOrder_AfterSuccessfulCharge_DoesNotThrow()
    {
        var adapter = new StripeAdapter(new StripeGateway("key"));
        var service = new OrderService(adapter);

        string txId = service.PlaceOrder("ORD-005", "tok_visa", 4999, "USD");

        // Should not throw
        service.CancelOrder("ORD-005", txId, 4999);
    }

    // ── Interchangeability ────────────────────────────────────

    [Fact]
    public void OrderService_AllThreeAdapters_ProduceNonEmptyTransactionIds()
    {
        IPaymentProcessor[] processors =
        [
            new StripeAdapter(new StripeGateway("key")),
            new PayPalAdapter(new PayPalSdk("id", "secret")),
            new SquareAdapter(new SquareApi("token")),
        ];

        int orderId = 1;
        foreach (var processor in processors)
        {
            var service = new OrderService(processor);
            string txId = service.PlaceOrder($"ORD-{orderId++}", "valid_token", 1000, "USD");

            Assert.False(string.IsNullOrWhiteSpace(txId),
                $"{processor.ProcessorName} should return a non-empty transaction ID");
        }
    }

    [Fact]
    public void PaymentResult_Ok_ToStringContainsSuccessMarker()
    {
        var result = PaymentResult.Ok("TX123", "Charged successfully");

        Assert.Contains("✅", result.ToString());
        Assert.Contains("TX123", result.ToString());
    }

    [Fact]
    public void PaymentResult_Fail_ToStringContainsFailMarker()
    {
        var result = PaymentResult.Fail("Card declined");

        Assert.Contains("❌", result.ToString());
        Assert.Contains("Card declined", result.ToString());
    }
}
