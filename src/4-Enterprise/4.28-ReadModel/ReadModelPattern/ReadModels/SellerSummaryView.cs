namespace ReadModelPattern.ReadModels;

// Denormalized read model for the seller dashboard.
// Built from the same events as ProductCatalogueView but shaped
// entirely differently — aggregated by seller rather than by product.
public sealed class SellerSummaryView
{
    public string SellerId { get; set; } = "";
    public int ActiveListings { get; set; }
    public int TotalUnitsSold { get; set; }
    public decimal TotalRevenueCAD { get; set; }
}
