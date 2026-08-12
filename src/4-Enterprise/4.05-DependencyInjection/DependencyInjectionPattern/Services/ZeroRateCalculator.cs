namespace DependencyInjectionPattern;

// Stub implementation used in Demo 6 and tests to show that swapping
// a registered service requires no changes to any consumer class.
public sealed class ZeroRateCalculator : IHstCalculator
{
    public Guid    InstanceId                              { get; } = Guid.NewGuid();
    public decimal Rate(string province = "ON")            => 0m;
    public decimal Calculate(decimal subtotal, string province = "ON") => 0m;
}
