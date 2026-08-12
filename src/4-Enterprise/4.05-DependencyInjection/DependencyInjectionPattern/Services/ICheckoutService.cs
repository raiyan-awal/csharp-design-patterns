namespace DependencyInjectionPattern;

public interface ICheckoutService
{
    Guid          InstanceId { get; }
    OrderSummary? Checkout(string province = "ON");
}
