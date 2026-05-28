namespace FactoryMethodPattern;

/// <summary>
/// FACTORY METHOD PATTERN - Abstract Creator
///
/// This abstract class defines the "factory method" that subclasses must implement.
/// The factory method returns an IPaymentProcessor, but the concrete type returned
/// depends on which subclass is used.
///
/// KEY CONCEPT:
/// The factory method delegates the instantiation of objects to subclasses.
/// This class knows HOW to use a payment processor, but doesn't know WHICH
/// concrete processor to create - that's decided by the subclass.
///
/// BENEFITS:
/// ✓ Open/Closed Principle - Can add new payment processors without modifying existing code
/// ✓ Single Responsibility - Each factory is responsible for creating one type of processor
/// ✓ Decoupling - Client code depends on IPaymentProcessor interface, not concrete types
/// </summary>
public abstract class PaymentProcessorFactory
{
    /// <summary>
    /// FACTORY METHOD - This is the method that subclasses must implement.
    ///
    /// Each subclass will return a different concrete implementation of IPaymentProcessor.
    /// This is the "hook" that allows subclasses to customize object creation.
    ///
    /// Note: This method is marked 'protected' because it's only called by ProcessTransaction().
    /// External code doesn't call CreateProcessor() directly - they call ProcessTransaction().
    /// </summary>
    protected abstract IPaymentProcessor CreateProcessor();

    /// <summary>
    /// TEMPLATE METHOD - This method uses the factory method to get a processor,
    /// then performs a standard payment processing workflow.
    ///
    /// This is the public interface that client code calls. Notice how it:
    /// 1. Uses the factory method to get a processor (concrete type determined by subclass)
    /// 2. Performs a standardized workflow (same for all payment types)
    ///
    /// This demonstrates how the Factory Method pattern allows you to define
    /// a standard algorithm (template) while letting subclasses customize
    /// specific steps (object creation).
    /// </summary>
    public string ProcessTransaction(decimal amount)
    {
        // Step 1: Get the appropriate payment processor (via factory method)
        // The concrete type is determined by which subclass this is
        var processor = CreateProcessor();

        Console.WriteLine($"\n--- Using {processor.GetProcessorName()} ---");

        // Step 2: Validate the payment method
        if (!processor.ValidatePaymentMethod())
        {
            throw new InvalidOperationException("Payment method validation failed");
        }

        // Step 3: Process the payment
        var transactionId = processor.ProcessPayment(amount);

        // Step 4: Log the transaction (in a real app, you'd save to database)
        LogTransaction(processor.GetProcessorName(), amount, transactionId);

        return transactionId;
    }

    /// <summary>
    /// Helper method to log transaction details.
    /// In a real application, this would save to a database or audit log.
    /// </summary>
    private void LogTransaction(string processorName, decimal amount, string transactionId)
    {
        Console.WriteLine($"[Audit Log] {processorName}: ${amount:F2} - TX: {transactionId}");
    }
}

/// <summary>
/// CONCRETE CREATOR #1: Credit Card Processor Factory
///
/// This factory creates CreditCardProcessor instances.
/// Notice how simple this is - it just implements the factory method
/// and returns the appropriate concrete type.
/// </summary>
public class CreditCardProcessorFactory : PaymentProcessorFactory
{
    private readonly string _cardNumber;
    private readonly string _cardHolderName;
    private readonly string _expiryDate;

    /// <summary>
    /// Constructor accepts the data needed to create a CreditCardProcessor.
    /// This is passed to the factory, not to ProcessTransaction.
    /// </summary>
    public CreditCardProcessorFactory(string cardNumber, string cardHolderName, string expiryDate)
    {
        _cardNumber = cardNumber;
        _cardHolderName = cardHolderName;
        _expiryDate = expiryDate;
    }

    /// <summary>
    /// Factory method implementation - creates and returns a CreditCardProcessor.
    /// This overrides the abstract method in PaymentProcessorFactory.
    /// </summary>
    protected override IPaymentProcessor CreateProcessor()
    {
        return new CreditCardProcessor(_cardNumber, _cardHolderName, _expiryDate);
    }
}

/// <summary>
/// CONCRETE CREATOR #2: PayPal Processor Factory
///
/// This factory creates PayPalProcessor instances.
/// </summary>
public class PayPalProcessorFactory : PaymentProcessorFactory
{
    private readonly string _email;
    private readonly string _apiKey;

    public PayPalProcessorFactory(string email, string apiKey)
    {
        _email = email;
        _apiKey = apiKey;
    }

    /// <summary>
    /// Factory method implementation - creates and returns a PayPalProcessor.
    /// </summary>
    protected override IPaymentProcessor CreateProcessor()
    {
        return new PayPalProcessor(_email, _apiKey);
    }
}

/// <summary>
/// CONCRETE CREATOR #3: Cryptocurrency Processor Factory
///
/// This factory creates CryptoProcessor instances.
/// </summary>
public class CryptoProcessorFactory : PaymentProcessorFactory
{
    private readonly string _walletAddress;
    private readonly string _cryptoCurrency;

    public CryptoProcessorFactory(string walletAddress, string cryptoCurrency)
    {
        _walletAddress = walletAddress;
        _cryptoCurrency = cryptoCurrency;
    }

    /// <summary>
    /// Factory method implementation - creates and returns a CryptoProcessor.
    /// </summary>
    protected override IPaymentProcessor CreateProcessor()
    {
        return new CryptoProcessor(_walletAddress, _cryptoCurrency);
    }
}

/// <summary>
/// ALTERNATIVE APPROACH: Simple Factory (Not Factory Method Pattern)
///
/// This is a simpler approach using a single static method instead of inheritance.
/// It's NOT the Factory Method pattern - it's the "Simple Factory" pattern.
///
/// WHEN TO USE SIMPLE FACTORY INSTEAD:
/// - When you don't need the flexibility of subclassing
/// - When the creation logic is simple
/// - When you just need to centralize object creation
///
/// DRAWBACKS vs Factory Method:
/// ✗ Violates Open/Closed Principle (must modify this class to add new types)
/// ✗ Less flexible (can't override creation logic per subclass)
/// ✗ Creates tight coupling to all concrete types
/// </summary>
public static class SimplePaymentProcessorFactory
{
    /// <summary>
    /// Creates a payment processor based on the type string.
    ///
    /// This is simpler than Factory Method, but:
    /// - Violates Open/Closed: must modify this method to add new types
    /// - All concrete types are referenced here (tight coupling)
    /// </summary>
    public static IPaymentProcessor CreateProcessor(string type, Dictionary<string, string> config)
    {
        return type.ToLower() switch
        {
            "creditcard" => new CreditCardProcessor(
                config["cardNumber"],
                config["cardHolderName"],
                config["expiryDate"]
            ),

            "paypal" => new PayPalProcessor(
                config["email"],
                config["apiKey"]
            ),

            "crypto" => new CryptoProcessor(
                config["walletAddress"],
                config["cryptoCurrency"]
            ),

            _ => throw new ArgumentException($"Unknown payment processor type: {type}")
        };
    }
}
