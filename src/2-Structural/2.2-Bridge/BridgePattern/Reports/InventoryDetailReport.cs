namespace BridgePattern.Reports;

// ============================================================
// REFINED ABSTRACTION #2 — Inventory Detail Report
// ============================================================
// Knows about inventory data: stock levels, low-stock warnings,
// warehouse breakdown. Again — zero knowledge of output format.

/// <summary>
/// A detailed breakdown of current inventory levels per product.
/// </summary>
public sealed class InventoryDetailReport : Report
{
    private readonly InventoryData _data;

    public InventoryDetailReport(IReportRenderer renderer, InventoryData data)
        : base(renderer)
    {
        _data = data;
    }

    protected override void BuildContent()
    {
        Renderer.RenderTitle("Inventory Status Report");

        Renderer.RenderSection("Overview");
        Renderer.RenderRow("Report Date",       _data.AsOfDate.ToString("yyyy-MM-dd"));
        Renderer.RenderRow("Total SKUs",         _data.Items.Count.ToString());
        Renderer.RenderRow("Low Stock Items",    _data.Items.Count(i => i.IsLowStock).ToString());
        Renderer.RenderRow("Out of Stock",       _data.Items.Count(i => i.Quantity == 0).ToString());

        Renderer.RenderSection("Product Breakdown");
        foreach (var item in _data.Items)
        {
            string status = item.Quantity == 0     ? "OUT OF STOCK"
                          : item.IsLowStock        ? "LOW STOCK"
                                                   : "OK";
            Renderer.RenderRow(item.ProductName, $"Qty: {item.Quantity,5}  |  {status}");
        }

        if (_data.Items.Any(i => i.IsLowStock || i.Quantity == 0))
            Renderer.RenderParagraph("Action required: reorder flagged items before next cycle.");
    }
}

/// <summary>Data containers for <see cref="InventoryDetailReport"/>.</summary>
public sealed class InventoryData
{
    public DateTime AsOfDate      { get; init; }
    public List<InventoryItem> Items { get; init; } = [];
}

public sealed class InventoryItem
{
    public string ProductName  { get; init; } = string.Empty;
    public int    Quantity     { get; init; }
    public int    ReorderLevel { get; init; }
    public bool   IsLowStock   => Quantity > 0 && Quantity <= ReorderLevel;
}
