namespace FactoryMethodPattern;

/// <summary>
/// CONCRETE PRODUCT #1: Credit Card Payment Processor
///
/// This class implements IPaymentProcessor for credit card payments.
/// It encapsulates all the logic specific to processing credit card transactions.
/// </summary>
public class CreditCardProcessor : IPaymentProcessor
{
    private readonly string _cardNumber;
    private readonly string _cardHolderName;
    private readonly string _expiryDate;

    /// <summary>
    /// Constructor for CreditCardProcessor.
    /// In a real application, you'd also include CVV, billing address, etc.
    /// </summary>
    public CreditCardProcessor(string cardNumber, string cardHolderName, string expiryDate)
    {
        _cardNumber = cardNumber;
        _cardHolderName = cardHolderName;
        _expiryDate = expiryDate;
    }

    public string ProcessPayment(decimal amount)
    {
        // In a real application, this would:
        // 1. Connect to a payment gateway (Stripe, Square, etc.)
        // 2. Tokenize the card number
        // 3. Submit the charge
        // 4. Handle 3D Secure if required
        // 5. Return the transaction ID

        Console.WriteLine($"[CreditCard] Processing ${amount:F2} charge...");
        Console.WriteLine($"[CreditCard] Card: ***{_cardNumber[^4..]} | Holder: {_cardHolderName}");

        // Simulate processing delay
        Thread.Sleep(500);

        // Generate a fake transaction ID
        var transactionId = $"CC-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        Console.WriteLine($"[CreditCard] ✓ Payment successful! Transaction ID: {transactionId}");

        return transactionId;
    }

    public bool ValidatePaymentMethod()
    {
        // Basic validation: card number should be 16 digits, expiry in future, etc.
        // This is simplified - real validation is much more complex (Luhn algorithm, etc.)

        if (string.IsNullOrWhiteSpace(_cardNumber) || _cardNumber.Length != 16)
        {
            Console.WriteLine("[CreditCard] ✗ Invalid card number");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_cardHolderName))
        {
            Console.WriteLine("[CreditCard] ✗ Invalid card holder name");
            return false;
        }

        Console.WriteLine("[CreditCard] ✓ Payment method validated");
        return true;
    }

    public string GetProcessorName() => "Credit Card Processor";
}

/// <summary>
/// CONCRETE PRODUCT #2: PayPal Payment Processor
///
/// This class implements IPaymentProcessor for PayPal payments.
/// It encapsulates all the logic specific to processing PayPal transactions.
/// </summary>
public class PayPalProcessor : IPaymentProcessor
{
    private readonly string _email;
    private readonly string _apiKey;

    /// <summary>
    /// Constructor for PayPalProcessor.
    /// In a real application, you'd use OAuth tokens instead of API keys.
    /// </summary>
    public PayPalProcessor(string email, string apiKey)
    {
        _email = email;
        _apiKey = apiKey;
    }

    public string ProcessPayment(decimal amount)
    {
        // In a real application, this would:
        // 1. Authenticate with PayPal API
        // 2. Create a payment request
        // 3. Redirect user to PayPal for approval (if not pre-approved)
        // 4. Capture the payment
        // 5. Return the transaction ID

        Console.WriteLine($"[PayPal] Processing ${amount:F2} payment...");
        Console.WriteLine($"[PayPal] Account: {_email}");

        // Simulate API call delay
        Thread.Sleep(700);

        // Generate a fake transaction ID
        var transactionId = $"PP-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        Console.WriteLine($"[PayPal] ✓ Payment successful! Transaction ID: {transactionId}");

        return transactionId;
    }

    public bool ValidatePaymentMethod()
    {
        // Validate email format and API key
        if (string.IsNullOrWhiteSpace(_email) || !_email.Contains("@"))
        {
            Console.WriteLine("[PayPal] ✗ Invalid email address");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            Console.WriteLine("[PayPal] ✗ Invalid API key");
            return false;
        }

        Console.WriteLine("[PayPal] ✓ Payment method validated");
        return true;
    }

    public string GetProcessorName() => "PayPal Processor";
}

/// <summary>
/// CONCRETE PRODUCT #3: Cryptocurrency Payment Processor
///
/// This class implements IPaymentProcessor for cryptocurrency payments.
/// It encapsulates all the logic specific to processing crypto transactions.
/// </summary>
public class CryptoProcessor : IPaymentProcessor
{
    private readonly string _walletAddress;
    private readonly string _cryptoCurrency; // BTC, ETH, USDT, etc.

    /// <summary>
    /// Constructor for CryptoProcessor.
    /// In a real application, you'd integrate with blockchain APIs.
    /// </summary>
    public CryptoProcessor(string walletAddress, string cryptoCurrency)
    {
        _walletAddress = walletAddress;
        _cryptoCurrency = cryptoCurrency;
    }

    public string ProcessPayment(decimal amount)
    {
        // In a real application, this would:
        // 1. Convert USD amount to crypto amount (based on current exchange rate)
        // 2. Generate a payment address or QR code
        // 3. Wait for blockchain confirmation
        // 4. Verify the transaction on the blockchain
        // 5. Return the transaction hash

        Console.WriteLine($"[Crypto] Processing ${amount:F2} payment in {_cryptoCurrency}...");
        Console.WriteLine($"[Crypto] Destination wallet: {_walletAddress}");

        // Simulate blockchain confirmation delay (crypto is slower!)
        Thread.Sleep(1000);

        // Generate a fake transaction hash (blockchain transaction ID)
        var transactionHash = $"0x{Guid.NewGuid().ToString().Replace("-", "")}";

        Console.WriteLine($"[Crypto] ✓ Payment confirmed on blockchain! TX Hash: {transactionHash}");

        return transactionHash;
    }

    public bool ValidatePaymentMethod()
    {
        // Validate wallet address format (simplified - real validation depends on the crypto)
        if (string.IsNullOrWhiteSpace(_walletAddress))
        {
            Console.WriteLine("[Crypto] ✗ Invalid wallet address");
            return false;
        }

        // Check that we support this cryptocurrency
        var supportedCurrencies = new[] { "BTC", "ETH", "USDT", "USDC" };
        if (!supportedCurrencies.Contains(_cryptoCurrency.ToUpper()))
        {
            Console.WriteLine($"[Crypto] ✗ Unsupported cryptocurrency: {_cryptoCurrency}");
            return false;
        }

        Console.WriteLine("[Crypto] ✓ Payment method validated");
        return true;
    }

    public string GetProcessorName() => $"Cryptocurrency Processor ({_cryptoCurrency})";
}
