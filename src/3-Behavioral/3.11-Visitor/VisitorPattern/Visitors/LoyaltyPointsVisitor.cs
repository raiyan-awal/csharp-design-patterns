namespace VisitorPattern;

public sealed class LoyaltyPointsVisitor : ICartVisitor
{
    public int TotalPoints { get; private set; }

    public void Visit(PhysicalProduct p)
    {
        var pts = (int)p.Price; // 1 point per dollar
        TotalPoints += pts;
        Console.WriteLine($"  Points '{p.Name}': {pts} pts (1 pt/$)");
    }

    public void Visit(DigitalProduct p)
    {
        var pts = (int)(p.Price * 2); // 2 points per dollar
        TotalPoints += pts;
        Console.WriteLine($"  Points '{p.Name}': {pts} pts (2 pts/$)");
    }

    public void Visit(FoodItem f)
        => Console.WriteLine($"  Points '{f.Name}': 0 pts (groceries excluded)");

    public void Visit(SubscriptionService s)
    {
        var pts = s.Months * 50; // 50 flat points per month
        TotalPoints += pts;
        Console.WriteLine($"  Points '{s.Name}': {pts} pts ({s.Months} months × 50)");
    }
}
