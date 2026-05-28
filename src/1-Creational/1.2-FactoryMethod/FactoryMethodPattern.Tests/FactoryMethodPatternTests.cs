using FactoryMethodPattern;

namespace FactoryMethodPattern.Tests;

/// <summary>
/// Unit tests for the Factory Method pattern implementation.
///
/// These tests verify that:
/// 1. Each concrete factory creates the correct processor type
/// 2. Each processor returns the correct name (product identity)
/// 3. Validation passes for well-formed inputs and fails for malformed ones
/// 4. ProcessTransaction() returns a non-empty transaction ID
/// 5. All factories are interchangeable through the base class
/// 6. SimplePaymentProcessorFactory creates the right type per string key
/// 7. SimplePaymentProcessorFactory throws for unknown processor types
/// </summary>
public class FactoryMethodPatternTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // PROCESSOR IDENTITY — GetProcessorName() returns the right label
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreditCardProcessor_GetProcessorName_ReturnsCreditCardLabel()
    {
        var processor = new CreditCardProcessor("1234567890123456", "Alice Smith", "12/27");

        Assert.Equal("Credit Card Processor", processor.GetProcessorName());
    }

    [Fact]
    public void PayPalProcessor_GetProcessorName_ReturnsPayPalLabel()
    {
        var processor = new PayPalProcessor("alice@example.com", "api-key-abc");

        Assert.Equal("PayPal Processor", processor.GetProcessorName());
    }

    [Fact]
    public void CryptoProcessor_GetProcessorName_IncludesCurrencyName()
    {
        var processor = new CryptoProcessor("0xABCDEF123456", "ETH");

        // Name includes the currency so the caller knows which crypto is in use
        Assert.Contains("ETH", processor.GetProcessorName());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VALIDATION — ValidatePaymentMethod() for valid inputs
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreditCardProcessor_ValidatePaymentMethod_ReturnsTrueForValidCard()
    {
        // 16-digit card number + non-empty name
        var processor = new CreditCardProcessor("1234567890123456", "Alice Smith", "12/27");

        Assert.True(processor.ValidatePaymentMethod());
    }

    [Fact]
    public void PayPalProcessor_ValidatePaymentMethod_ReturnsTrueForValidEmail()
    {
        var processor = new PayPalProcessor("alice@example.com", "api-key-abc");

        Assert.True(processor.ValidatePaymentMethod());
    }

    [Theory]
    [InlineData("BTC")]
    [InlineData("ETH")]
    [InlineData("USDT")]
    [InlineData("USDC")]
    public void CryptoProcessor_ValidatePaymentMethod_ReturnsTrueForSupportedCurrencies(string currency)
    {
        var processor = new CryptoProcessor("0xABCDEF123456", currency);

        Assert.True(processor.ValidatePaymentMethod());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VALIDATION — ValidatePaymentMethod() for invalid inputs
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreditCardProcessor_ValidatePaymentMethod_ReturnsFalseForShortCardNumber()
    {
        // Only 15 digits — should fail validation
        var processor = new CreditCardProcessor("123456789012345", "Alice Smith", "12/27");

        Assert.False(processor.ValidatePaymentMethod());
    }

    [Fact]
    public void CreditCardProcessor_ValidatePaymentMethod_ReturnsFalseForEmptyCardHolder()
    {
        var processor = new CreditCardProcessor("1234567890123456", "", "12/27");

        Assert.False(processor.ValidatePaymentMethod());
    }

    [Fact]
    public void PayPalProcessor_ValidatePaymentMethod_ReturnsFalseForEmailWithoutAtSign()
    {
        var processor = new PayPalProcessor("notanemail", "api-key-abc");

        Assert.False(processor.ValidatePaymentMethod());
    }

    [Fact]
    public void PayPalProcessor_ValidatePaymentMethod_ReturnsFalseForEmptyApiKey()
    {
        var processor = new PayPalProcessor("alice@example.com", "");

        Assert.False(processor.ValidatePaymentMethod());
    }

    [Fact]
    public void CryptoProcessor_ValidatePaymentMethod_ReturnsFalseForUnsupportedCurrency()
    {
        var processor = new CryptoProcessor("0xABCDEF123456", "DOGE");

        Assert.False(processor.ValidatePaymentMethod());
    }

    [Fact]
    public void CryptoProcessor_ValidatePaymentMethod_ReturnsFalseForEmptyWalletAddress()
    {
        var processor = new CryptoProcessor("", "BTC");

        Assert.False(processor.ValidatePaymentMethod());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FACTORY METHOD — each factory produces the right product
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreditCardProcessorFactory_ProcessTransaction_ReturnsNonEmptyTransactionId()
    {
        PaymentProcessorFactory factory = new CreditCardProcessorFactory(
            "1234567890123456", "Alice Smith", "12/27");

        var txId = factory.ProcessTransaction(49.99m);

        Assert.False(string.IsNullOrWhiteSpace(txId));
    }

    [Fact]
    public void PayPalProcessorFactory_ProcessTransaction_ReturnsNonEmptyTransactionId()
    {
        PaymentProcessorFactory factory = new PayPalProcessorFactory(
            "alice@example.com", "api-key-abc");

        var txId = factory.ProcessTransaction(99.00m);

        Assert.False(string.IsNullOrWhiteSpace(txId));
    }

    [Fact]
    public void CryptoProcessorFactory_ProcessTransaction_ReturnsNonEmptyTransactionId()
    {
        PaymentProcessorFactory factory = new CryptoProcessorFactory(
            "0xABCDEF123456", "ETH");

        var txId = factory.ProcessTransaction(0.05m);

        Assert.False(string.IsNullOrWhiteSpace(txId));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TRANSACTION ID FORMAT — each processor uses a distinct prefix
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreditCardProcessor_ProcessPayment_TransactionIdStartsWithCC()
    {
        var processor = new CreditCardProcessor("1234567890123456", "Alice Smith", "12/27");

        var txId = processor.ProcessPayment(50m);

        Assert.StartsWith("CC-", txId);
    }

    [Fact]
    public void PayPalProcessor_ProcessPayment_TransactionIdStartsWithPP()
    {
        var processor = new PayPalProcessor("alice@example.com", "api-key-abc");

        var txId = processor.ProcessPayment(50m);

        Assert.StartsWith("PP-", txId);
    }

    [Fact]
    public void CryptoProcessor_ProcessPayment_TransactionIdStartsWith0x()
    {
        var processor = new CryptoProcessor("0xABCDEF123456", "BTC");

        var txId = processor.ProcessPayment(50m);

        Assert.StartsWith("0x", txId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INTERCHANGEABILITY — all factories usable through the base class
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllFactories_AreUsableAsPaymentProcessorFactory()
    {
        // All concrete factories can be assigned to the abstract base type —
        // client code never needs to know the concrete factory type
        PaymentProcessorFactory[] factories =
        [
            new CreditCardProcessorFactory("1234567890123456", "Alice Smith", "12/27"),
            new PayPalProcessorFactory("alice@example.com", "api-key-abc"),
            new CryptoProcessorFactory("0xABCDEF123456", "ETH")
        ];

        foreach (var factory in factories)
        {
            var exception = Record.Exception(() => factory.ProcessTransaction(10m));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void AllFactories_ProcessTransaction_ReturnUniqueTransactionIds()
    {
        PaymentProcessorFactory cc     = new CreditCardProcessorFactory("1234567890123456", "Alice", "12/27");
        PaymentProcessorFactory paypal = new PayPalProcessorFactory("alice@example.com", "key");
        PaymentProcessorFactory crypto = new CryptoProcessorFactory("0xABC123", "USDT");

        var txIds = new[]
        {
            cc.ProcessTransaction(10m),
            paypal.ProcessTransaction(10m),
            crypto.ProcessTransaction(10m)
        };

        // Each processor produces a unique transaction ID
        Assert.Equal(txIds.Length, txIds.Distinct().Count());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SIMPLE FACTORY — creates the correct concrete type per string key
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SimpleFactory_CreditCard_ReturnsIPaymentProcessor()
    {
        var config = new Dictionary<string, string>
        {
            ["cardNumber"]     = "1234567890123456",
            ["cardHolderName"] = "Alice Smith",
            ["expiryDate"]     = "12/27"
        };

        var processor = SimplePaymentProcessorFactory.CreateProcessor("creditcard", config);

        Assert.IsAssignableFrom<IPaymentProcessor>(processor);
        Assert.Equal("Credit Card Processor", processor.GetProcessorName());
    }

    [Fact]
    public void SimpleFactory_PayPal_ReturnsIPaymentProcessor()
    {
        var config = new Dictionary<string, string>
        {
            ["email"]  = "alice@example.com",
            ["apiKey"] = "api-key-abc"
        };

        var processor = SimplePaymentProcessorFactory.CreateProcessor("paypal", config);

        Assert.IsAssignableFrom<IPaymentProcessor>(processor);
        Assert.Equal("PayPal Processor", processor.GetProcessorName());
    }

    [Fact]
    public void SimpleFactory_Crypto_ReturnsIPaymentProcessor()
    {
        var config = new Dictionary<string, string>
        {
            ["walletAddress"]  = "0xABCDEF123456",
            ["cryptoCurrency"] = "BTC"
        };

        var processor = SimplePaymentProcessorFactory.CreateProcessor("crypto", config);

        Assert.IsAssignableFrom<IPaymentProcessor>(processor);
    }

    [Fact]
    public void SimpleFactory_UnknownType_ThrowsArgumentException()
    {
        var config = new Dictionary<string, string>();

        Assert.Throws<ArgumentException>(() =>
            SimplePaymentProcessorFactory.CreateProcessor("bitcoin", config));
    }
}
