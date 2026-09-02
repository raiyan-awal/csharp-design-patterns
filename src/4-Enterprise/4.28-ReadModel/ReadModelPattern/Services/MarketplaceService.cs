using ReadModelPattern.Engine;
using ReadModelPattern.Events;
using ReadModelPattern.Infrastructure;
using ReadModelPattern.ReadModels;

namespace ReadModelPattern.Services;

// Thin application service. Converts intent into events and delegates
// all queries to the read model stores — never touches the event store directly.
public sealed class MarketplaceService(
    ProjectionEngine engine,
    IReadModelStore<string, ProductCatalogueView> catalogue,
    IReadModelStore<string, SellerSummaryView> sellerStore)
{
    public void ListProduct(string productId, string sellerId, string title,
                            decimal priceCAD, int stock)
        => engine.Append(new ProductListedEvent(productId, sellerId, title, priceCAD, stock,
                                                DateTimeOffset.UtcNow));

    public void RecordSale(string productId, string sellerId, int quantity, decimal priceCAD)
        => engine.Append(new ProductSoldEvent(productId, sellerId, quantity, priceCAD,
                                              DateTimeOffset.UtcNow));

    public void UpdatePrice(string productId, decimal newPriceCAD)
        => engine.Append(new ProductPriceUpdatedEvent(productId, newPriceCAD,
                                                      DateTimeOffset.UtcNow));

    public void PostReview(string productId, int rating)
        => engine.Append(new ReviewPostedEvent(productId, rating, DateTimeOffset.UtcNow));

    public ProductCatalogueView? GetProduct(string productId) => catalogue.Get(productId);
    public IReadOnlyList<ProductCatalogueView> GetAllProducts() => catalogue.GetAll();

    public IReadOnlyList<ProductCatalogueView> GetTopSelling(int count) =>
        catalogue.GetAll()
                 .OrderByDescending(p => p.TotalSold)
                 .Take(count)
                 .ToList();

    public SellerSummaryView? GetSellerSummary(string sellerId) => sellerStore.Get(sellerId);
    public IReadOnlyList<SellerSummaryView> GetAllSellerSummaries() => sellerStore.GetAll();
}
