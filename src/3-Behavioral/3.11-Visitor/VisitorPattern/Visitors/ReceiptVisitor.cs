namespace VisitorPattern;

public sealed class ReceiptVisitor : ICartVisitor
{
    private readonly List<string> _lines = [];
    public IReadOnlyList<string> Lines => _lines;

    public void Visit(PhysicalProduct p)
        => _lines.Add($"  {p.Name,-32} ${p.Price,8:F2}");

    public void Visit(DigitalProduct p)
        => _lines.Add($"  {p.Name,-32} ${p.Price,8:F2}  [Digital]");

    public void Visit(FoodItem f)
        => _lines.Add($"  {f.Name,-32} ${f.Price,8:F2}  [Tax exempt]");

    public void Visit(SubscriptionService s)
        => _lines.Add($"  {s.Name,-32} ${s.Price,8:F2}  [{s.Months} × ${s.MonthlyPrice:F2}/mo]");

    public void Print()
    {
        foreach (var line in _lines)
            Console.WriteLine(line);
    }
}
