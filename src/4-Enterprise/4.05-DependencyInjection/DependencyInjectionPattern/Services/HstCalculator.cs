namespace DependencyInjectionPattern;

// Registered as Transient — stateless, so a new instance per consumer is fine.
// Creating it is cheap; there is no shared state that would benefit from reuse.
public sealed class HstCalculator : IHstCalculator
{
    public Guid InstanceId { get; } = Guid.NewGuid();

    private static readonly Dictionary<string, decimal> _rates = new()
    {
        ["ON"] = 0.13m,    // Ontario HST
        ["BC"] = 0.12m,    // British Columbia GST + PST
        ["AB"] = 0.05m,    // Alberta GST only
        ["QC"] = 0.14975m, // Quebec GST + QST
        ["NS"] = 0.15m,    // Nova Scotia HST
    };

    public decimal Rate(string province = "ON")
        => _rates.TryGetValue(province, out var r) ? r : 0.05m;

    public decimal Calculate(decimal subtotal, string province = "ON")
        => Math.Round(subtotal * Rate(province), 2);
}
