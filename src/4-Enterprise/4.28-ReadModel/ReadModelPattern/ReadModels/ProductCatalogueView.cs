namespace ReadModelPattern.ReadModels;

// Denormalized read model for product browsing and search.
// RatingSum + ReviewCount are stored separately so AverageRating
// is always computed from exact integers, avoiding floating-point drift.
public sealed class ProductCatalogueView
{
    public string ProductId { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal PriceCAD { get; set; }
    public int StockRemaining { get; set; }
    public int TotalSold { get; set; }
    public int ReviewCount { get; set; }
    public int RatingSum { get; set; }
    public double AverageRating => ReviewCount > 0 ? (double)RatingSum / ReviewCount : 0.0;
}
