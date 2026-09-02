using ReadModelPattern.Events;
using ReadModelPattern.Infrastructure;
using ReadModelPattern.ReadModels;

namespace ReadModelPattern.Projections;

// Builds the product catalogue read model.
// Handles four event types; ignores everything else.
public sealed class ProductCatalogueProjection(
    IReadModelStore<string, ProductCatalogueView> store) : IProjection
{
    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProductListedEvent e:
                store.Upsert(e.ProductId, new ProductCatalogueView
                {
                    ProductId      = e.ProductId,
                    Title          = e.Title,
                    PriceCAD       = e.PriceCAD,
                    StockRemaining = e.InitialStock
                });
                break;

            case ProductSoldEvent e:
                var sold = store.Get(e.ProductId);
                if (sold is null) break;
                sold.StockRemaining -= e.Quantity;
                sold.TotalSold      += e.Quantity;
                store.Upsert(e.ProductId, sold);
                break;

            case ProductPriceUpdatedEvent e:
                var priced = store.Get(e.ProductId);
                if (priced is null) break;
                priced.PriceCAD = e.NewPriceCAD;
                store.Upsert(e.ProductId, priced);
                break;

            case ReviewPostedEvent e:
                var reviewed = store.Get(e.ProductId);
                if (reviewed is null) break;
                reviewed.RatingSum   += e.Rating;
                reviewed.ReviewCount += 1;
                store.Upsert(e.ProductId, reviewed);
                break;
        }
    }

    public void Reset() => store.Clear();
}
