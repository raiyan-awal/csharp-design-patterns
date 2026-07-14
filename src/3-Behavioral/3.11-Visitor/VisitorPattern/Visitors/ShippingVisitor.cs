namespace VisitorPattern;

public sealed class ShippingVisitor : ICartVisitor
{
    private const decimal BaseRate        = 4.99m;
    private const decimal PerKgRate       = 1.50m;
    private const decimal RefrigeratedFee = 5.00m;

    public decimal TotalShipping { get; private set; }

    public void Visit(PhysicalProduct p)
    {
        var cost = Math.Round(BaseRate + (decimal)p.WeightKg * PerKgRate, 2);
        TotalShipping += cost;
        Console.WriteLine($"  Ship '{p.Name}': ${cost:F2} ({p.WeightKg:F1} kg × $1.50 + $4.99 base)");
    }

    public void Visit(DigitalProduct p)
        => Console.WriteLine($"  Ship '{p.Name}': $0.00 (digital — no shipping)");

    public void Visit(FoodItem f)
    {
        var cost = f.IsRefrigerated ? BaseRate + RefrigeratedFee : BaseRate;
        TotalShipping += cost;
        var note = f.IsRefrigerated ? " (refrigerated +$5.00)" : "";
        Console.WriteLine($"  Ship '{f.Name}': ${cost:F2}{note}");
    }

    public void Visit(SubscriptionService s)
        => Console.WriteLine($"  Ship '{s.Name}': $0.00 (digital service — no shipping)");
}
