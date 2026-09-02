using ReadModelPattern.Events;
using ReadModelPattern.Infrastructure;
using ReadModelPattern.ReadModels;

namespace ReadModelPattern.Projections;

// Builds the seller dashboard read model — aggregated per seller,
// using the same event stream as ProductCatalogueProjection.
public sealed class SellerSummaryProjection(
    IReadModelStore<string, SellerSummaryView> store) : IProjection
{
    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProductListedEvent e:
                var listed = store.Get(e.SellerId) ?? new SellerSummaryView { SellerId = e.SellerId };
                listed.ActiveListings++;
                store.Upsert(e.SellerId, listed);
                break;

            case ProductSoldEvent e:
                var sold = store.Get(e.SellerId) ?? new SellerSummaryView { SellerId = e.SellerId };
                sold.TotalUnitsSold  += e.Quantity;
                sold.TotalRevenueCAD += e.Quantity * e.PriceCAD;
                store.Upsert(e.SellerId, sold);
                break;
        }
    }

    public void Reset() => store.Clear();
}
