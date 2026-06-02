namespace BridgePattern.Reports;

// ============================================================
// REFINED ABSTRACTION #1 — Sales Summary Report
// ============================================================
// Knows what sales data to present (totals, top product, region).
// Has no idea whether the output will be PDF, HTML, or CSV —
// that decision lives entirely in the injected renderer.

/// <summary>
/// A high-level summary of sales performance for a given period.
/// </summary>
public sealed class SalesSummaryReport : Report
{
    private readonly SalesSummaryData _data;

    public SalesSummaryReport(IReportRenderer renderer, SalesSummaryData data)
        : base(renderer)
    {
        _data = data;
    }

    protected override void BuildContent()
    {
        Renderer.RenderTitle($"Sales Summary — {_data.Period}");

        Renderer.RenderSection("Key Metrics");
        Renderer.RenderRow("Total Revenue",    $"${_data.TotalRevenue:N2}");
        Renderer.RenderRow("Total Orders",     _data.TotalOrders.ToString());
        Renderer.RenderRow("Average Order",    $"${_data.AverageOrderValue:N2}");
        Renderer.RenderRow("Conversion Rate",  $"{_data.ConversionRate:P1}");

        Renderer.RenderSection("Highlights");
        Renderer.RenderRow("Top Product",      _data.TopProduct);
        Renderer.RenderRow("Top Region",       _data.TopRegion);

        Renderer.RenderParagraph(_data.ExecutiveSummary);
    }
}

/// <summary>Data container for <see cref="SalesSummaryReport"/>.</summary>
public sealed class SalesSummaryData
{
    public string Period            { get; init; } = string.Empty;
    public decimal TotalRevenue     { get; init; }
    public int TotalOrders          { get; init; }
    public decimal AverageOrderValue { get; init; }
    public double ConversionRate    { get; init; }
    public string TopProduct        { get; init; } = string.Empty;
    public string TopRegion         { get; init; } = string.Empty;
    public string ExecutiveSummary  { get; init; } = string.Empty;
}
