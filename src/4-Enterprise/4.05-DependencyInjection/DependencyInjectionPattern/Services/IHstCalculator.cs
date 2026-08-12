namespace DependencyInjectionPattern;

public interface IHstCalculator
{
    Guid    InstanceId                              { get; }
    decimal Rate(string province = "ON");
    decimal Calculate(decimal subtotal, string province = "ON");
}
