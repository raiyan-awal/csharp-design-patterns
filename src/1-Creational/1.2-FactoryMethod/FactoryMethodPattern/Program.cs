namespace FactoryMethodPattern;

/// <summary>
/// FACTORY METHOD PATTERN DEMONSTRATION
///
/// This program demonstrates the Factory Method pattern by showing:
/// 1. How subclasses decide which concrete class to instantiate
/// 2. How client code depends on abstractions, not concrete types
/// 3. How to add new payment methods without modifying existing code
/// 4. The difference between Factory Method and Simple Factory
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== FACTORY METHOD PATTERN DEMO ===");
        Console.WriteLine("Press any key after each section to continue.\n");

        // DEMONSTRATION 1: Using Factory Method Pattern
        Console.WriteLine("--- Demonstration 1: Factory Method Pattern ---");
        Console.WriteLine("Creating three factories — each knows how to build its own processor.\n");

        var creditCardFactory = new CreditCardProcessorFactory(
            cardNumber: "4532123456789012",
            cardHolderName: "John Doe",
            expiryDate: "12/2027"
        );

        var paypalFactory = new PayPalProcessorFactory(
            email: "john.doe@example.com",
            apiKey: "sk_live_abc123xyz"
        );

        var cryptoFactory = new CryptoProcessorFactory(
            walletAddress: "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
            cryptoCurrency: "BTC"
        );

        // ProcessTransaction() is identical on all three — the factory method
        // handles creating the right processor internally.
        try
        {
            var tx1 = creditCardFactory.ProcessTransaction(99.99m);
            var tx2 = paypalFactory.ProcessTransaction(249.50m);
            var tx3 = cryptoFactory.ProcessTransaction(1500.00m);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\n→ Same method call (ProcessTransaction) on every factory — different processor used each time.");

        Pause();

        // DEMONSTRATION 2: Polymorphism with Factories
        Console.WriteLine("--- Demonstration 2: Polymorphism ---");
        Console.WriteLine("All factories share the same base type — they can be stored in a list and called uniformly.\n");

        var factories = new List<PaymentProcessorFactory>
        {
            new CreditCardProcessorFactory("5555555555554444", "Jane Smith", "06/2026"),
            new PayPalProcessorFactory("jane@example.com", "sk_live_xyz789"),
            new CryptoProcessorFactory("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb", "ETH")
        };

        foreach (var factory in factories)
            factory.ProcessTransaction(100.00m);

        Console.WriteLine("\n→ The loop doesn't know or care which payment method is used — that's the power of the abstraction.");

        Pause();

        // DEMONSTRATION 3: Simple Factory (Alternative Approach)
        Console.WriteLine("--- Demonstration 3: Simple Factory (Not Factory Method) ---");
        Console.WriteLine("A single static method with a switch — simpler, but requires modification to add new types.\n");

        var ccConfig = new Dictionary<string, string>
        {
            { "cardNumber", "4111111111111111" },
            { "cardHolderName", "Bob Johnson" },
            { "expiryDate", "03/2028" }
        };

        var processor = SimplePaymentProcessorFactory.CreateProcessor("creditcard", ccConfig);
        Console.WriteLine($"Created: {processor.GetProcessorName()}");
        processor.ValidatePaymentMethod();

        Console.WriteLine("\n→ Works fine, but adding Apple Pay means editing this class (violates Open/Closed Principle).");

        Pause();

        // DEMONSTRATION 4: Extensibility Comparison
        Console.WriteLine("--- Demonstration 4: Extensibility ---");
        Console.WriteLine("How easy is it to add a new payment method?\n");

        Console.WriteLine("Factory Method Pattern:");
        Console.WriteLine("  ✓ Create new ConcreteProduct  (e.g., ApplePayProcessor : IPaymentProcessor)");
        Console.WriteLine("  ✓ Create new ConcreteCreator  (e.g., ApplePayProcessorFactory : PaymentProcessorFactory)");
        Console.WriteLine("  ✓ Zero changes to existing code  (Open/Closed Principle)");

        Console.WriteLine("\nSimple Factory:");
        Console.WriteLine("  ✗ Must open and modify SimplePaymentProcessorFactory.CreateProcessor()");
        Console.WriteLine("  ✗ Violates Open/Closed Principle");
        Console.WriteLine("  ✓ Acceptable when the type list is small and stable");

        Pause();

        // DEMONSTRATION 5: When to Use Each
        Console.WriteLine("--- Demonstration 5: When to Use Each ---");

        Console.WriteLine("Use FACTORY METHOD when:");
        Console.WriteLine("  • You need to delegate object creation to subclasses");
        Console.WriteLine("  • You want to follow Open/Closed Principle");
        Console.WriteLine("  • Creation logic varies significantly between types");
        Console.WriteLine("  • You're building a framework or library others will extend");

        Console.WriteLine("\nUse SIMPLE FACTORY when:");
        Console.WriteLine("  • You just need to centralize object creation");
        Console.WriteLine("  • Creation logic is simple and unlikely to grow");
        Console.WriteLine("  • You don't need subclass customization");

        Pause();

        // SUMMARY
        Console.WriteLine("=== KEY TAKEAWAYS ===");
        Console.WriteLine("✓ Factory Method delegates object creation to subclasses via an overridable method");
        Console.WriteLine("✓ Client code depends on the interface (IPaymentProcessor), never on concrete types");
        Console.WriteLine("✓ Follows Open/Closed Principle — add new types without touching existing code");
        Console.WriteLine("✓ Each factory has one responsibility: creating one type of processor");
        Console.WriteLine("✓ Simple Factory is not the same pattern — useful but less flexible");

        Console.WriteLine("\n=== REAL-WORLD EXAMPLES ===");
        Console.WriteLine("• Payment processors (as shown)");
        Console.WriteLine("• Document exporters (PDF, Word, Excel)");
        Console.WriteLine("• Logger factories (FileLogger, DatabaseLogger, CloudLogger)");
        Console.WriteLine("• Notification senders (EmailFactory, SMSFactory, PushFactory)");

        Console.WriteLine("\nDemo complete.");
    }

    static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
