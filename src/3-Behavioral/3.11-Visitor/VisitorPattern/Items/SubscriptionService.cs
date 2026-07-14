namespace VisitorPattern;

public sealed class SubscriptionService : ICartItem
{
    public string  Name         { get; }
    public decimal MonthlyPrice { get; }
    public int     Months       { get; }
    public decimal Price        => MonthlyPrice * Months;

    public SubscriptionService(string name, decimal monthlyPrice, int months)
    {
        Name = name; MonthlyPrice = monthlyPrice; Months = months;
    }

    public void Accept(ICartVisitor visitor) => visitor.Visit(this);
}
