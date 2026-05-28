namespace FactoryMethodPattern;

/// <summary>
/// FACTORY METHOD PATTERN - Product Interface
///
/// This is the common interface that all payment processors must implement.
/// The Factory Method pattern returns objects that implement this interface,
/// but the concrete type returned depends on which factory is used.
///
/// In this example:
/// - Product = IPaymentProcessor (the interface)
/// - ConcreteProducts = CreditCardProcessor, PayPalProcessor, CryptoProcessor
/// - Creator = PaymentProcessorFactory (abstract)
/// - ConcreteCreators = CreditCardFactory, PayPalFactory, CryptoFactory
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Process a payment of the specified amount.
    /// Each concrete implementation will handle this differently.
    /// </summary>
    /// <param name="amount">Amount to process</param>
    /// <returns>Transaction ID if successful</returns>
    string ProcessPayment(decimal amount);

    /// <summary>
    /// Validate that the payment method is properly configured and can process payments.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidatePaymentMethod();

    /// <summary>
    /// Get the name of this payment processor (for display/logging purposes).
    /// </summary>
    string GetProcessorName();
}
