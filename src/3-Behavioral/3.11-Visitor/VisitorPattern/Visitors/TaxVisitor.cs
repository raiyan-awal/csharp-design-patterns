namespace VisitorPattern;

public sealed class TaxVisitor : ICartVisitor
{
    private const decimal HstRate = 0.13m;

    public decimal TotalTax { get; private set; }

    public void Visit(PhysicalProduct p)
    {
        var tax = Math.Round(p.Price * HstRate, 2);
        TotalTax += tax;
        Console.WriteLine($"  Tax  '{p.Name}': ${tax:F2} (HST 13%)");
    }

    public void Visit(DigitalProduct p)
    {
        var tax = Math.Round(p.Price * HstRate, 2);
        TotalTax += tax;
        Console.WriteLine($"  Tax  '{p.Name}': ${tax:F2} (HST 13%)");
    }

    public void Visit(FoodItem f)
        => Console.WriteLine($"  Tax  '{f.Name}': $0.00 (basic groceries — tax exempt)");

    public void Visit(SubscriptionService s)
    {
        var tax = Math.Round(s.Price * HstRate, 2);
        TotalTax += tax;
        Console.WriteLine($"  Tax  '{s.Name}': ${tax:F2} (HST 13% on ${s.Price:F2})");
    }
}
