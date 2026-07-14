namespace VisitorPattern;

public interface ICartVisitor
{
    void Visit(PhysicalProduct product);
    void Visit(DigitalProduct product);
    void Visit(FoodItem item);
    void Visit(SubscriptionService subscription);
}
