using ReadModelPattern.Engine;
using ReadModelPattern.Events;
using ReadModelPattern.Infrastructure;
using ReadModelPattern.Projections;
using ReadModelPattern.ReadModels;
using ReadModelPattern.Services;
using Xunit;

namespace ReadModelPattern.Tests;

// ─── Helpers ─────────────────────────────────────────────────────────────────

file static class Factory
{
    public static DateTimeOffset Now => DateTimeOffset.UtcNow;

    public static ProductListedEvent Listed(string id = "prod-1", string seller = "seller-a",
        string title = "Test Product", decimal price = 100m, int stock = 10)
        => new(id, seller, title, price, stock, Now);

    public static ProductSoldEvent Sold(string id = "prod-1", string seller = "seller-a",
        int qty = 1, decimal price = 100m)
        => new(id, seller, qty, price, Now);

    public static ProductPriceUpdatedEvent PriceUpdated(string id = "prod-1", decimal price = 80m)
        => new(id, price, Now);

    public static ReviewPostedEvent Reviewed(string id = "prod-1", int rating = 5)
        => new(id, rating, Now);

    public static (InMemoryReadModelStore<string, ProductCatalogueView> Store,
                   ProductCatalogueProjection Projection) CatalogueProjection()
    {
        var store = new InMemoryReadModelStore<string, ProductCatalogueView>();
        return (store, new ProductCatalogueProjection(store));
    }

    public static (InMemoryReadModelStore<string, SellerSummaryView> Store,
                   SellerSummaryProjection Projection) SellerProjection()
    {
        var store = new InMemoryReadModelStore<string, SellerSummaryView>();
        return (store, new SellerSummaryProjection(store));
    }
}

// ─── ProductCatalogueProjection ───────────────────────────────────────────────

public class ProductCatalogueProjection_Tests
{
    [Fact]
    public void ProductListed_CreatesView_WithCorrectFields()
    {
        var (store, proj) = Factory.CatalogueProjection();
        proj.Apply(Factory.Listed("prod-1", title: "Roots Hoodie", price: 89.99m, stock: 50));

        var view = store.Get("prod-1")!;
        Assert.Equal("prod-1",       view.ProductId);
        Assert.Equal("Roots Hoodie", view.Title);
        Assert.Equal(89.99m,         view.PriceCAD);
        Assert.Equal(50,             view.StockRemaining);
        Assert.Equal(0,              view.TotalSold);
    }

    [Fact]
    public void ProductSold_DecrementsStock_AndIncrementsTotalSold()
    {
        var (store, proj) = Factory.CatalogueProjection();
        proj.Apply(Factory.Listed("prod-1", stock: 20));
        proj.Apply(Factory.Sold("prod-1", qty: 3));

        var view = store.Get("prod-1")!;
        Assert.Equal(17, view.StockRemaining);
        Assert.Equal(3,  view.TotalSold);
    }

    [Fact]
    public void MultipleSales_Accumulate_Correctly()
    {
        var (store, proj) = Factory.CatalogueProjection();
        proj.Apply(Factory.Listed("prod-1", stock: 30));
        proj.Apply(Factory.Sold("prod-1", qty: 5));
        proj.Apply(Factory.Sold("prod-1", qty: 8));

        var view = store.Get("prod-1")!;
        Assert.Equal(17, view.StockRemaining);
        Assert.Equal(13, view.TotalSold);
    }

    [Fact]
    public void ProductPriceUpdated_ChangesPrice()
    {
        var (store, proj) = Factory.CatalogueProjection();
        proj.Apply(Factory.Listed("prod-1", price: 100m));
        proj.Apply(Factory.PriceUpdated("prod-1", price: 79.99m));

        Assert.Equal(79.99m, store.Get("prod-1")!.PriceCAD);
    }

    [Fact]
    public void ReviewPosted_UpdatesRatingCount_AndRatingSum()
    {
        var (store, proj) = Factory.CatalogueProjection();
        proj.Apply(Factory.Listed("prod-1"));
        proj.Apply(Factory.Reviewed("prod-1", rating: 4));
        proj.Apply(Factory.Reviewed("prod-1", rating: 5));

        var view = store.Get("prod-1")!;
        Assert.Equal(2,   view.ReviewCount);
        Assert.Equal(9,   view.RatingSum);
        Assert.Equal(4.5, view.AverageRating);
    }

    [Fact]
    public void AverageRating_IsZero_WhenNoReviews()
    {
        var (store, proj) = Factory.CatalogueProjection();
        proj.Apply(Factory.Listed("prod-1"));
        Assert.Equal(0.0, store.Get("prod-1")!.AverageRating);
    }

    [Fact]
    public void UnknownEventType_IsIgnored()
    {
        var (store, proj) = Factory.CatalogueProjection();
        // SellerSummaryEvent is not handled by CatalogueProjection
        proj.Apply(Factory.Listed("prod-1"));
        proj.Apply(Factory.Reviewed("prod-2", rating: 5)); // unknown product — no crash
        Assert.Single(store.GetAll());
    }

    [Fact]
    public void Reset_ClearsAllViews()
    {
        var (store, proj) = Factory.CatalogueProjection();
        proj.Apply(Factory.Listed("prod-1"));
        proj.Apply(Factory.Listed("prod-2"));
        proj.Reset();
        Assert.Equal(0, store.Count);
    }
}

// ─── SellerSummaryProjection ──────────────────────────────────────────────────

public class SellerSummaryProjection_Tests
{
    [Fact]
    public void ProductListed_IncrementsActiveListings()
    {
        var (store, proj) = Factory.SellerProjection();
        proj.Apply(Factory.Listed("prod-1", seller: "seller-a"));

        Assert.Equal(1, store.Get("seller-a")!.ActiveListings);
    }

    [Fact]
    public void MultipleListings_SameSeller_Accumulate()
    {
        var (store, proj) = Factory.SellerProjection();
        proj.Apply(Factory.Listed("prod-1", seller: "seller-a"));
        proj.Apply(Factory.Listed("prod-2", seller: "seller-a"));
        proj.Apply(Factory.Listed("prod-3", seller: "seller-b"));

        Assert.Equal(2, store.Get("seller-a")!.ActiveListings);
        Assert.Equal(1, store.Get("seller-b")!.ActiveListings);
    }

    [Fact]
    public void ProductSold_UpdatesUnitsSold_AndRevenue()
    {
        var (store, proj) = Factory.SellerProjection();
        proj.Apply(Factory.Listed("prod-1", seller: "seller-a", price: 100m));
        proj.Apply(Factory.Sold("prod-1",   seller: "seller-a", qty: 3, price: 100m));

        var summary = store.Get("seller-a")!;
        Assert.Equal(3,      summary.TotalUnitsSold);
        Assert.Equal(300.0m, summary.TotalRevenueCAD);
    }

    [Fact]
    public void MultipleSales_AccumulateRevenueAndUnits()
    {
        var (store, proj) = Factory.SellerProjection();
        proj.Apply(Factory.Sold("prod-1", seller: "seller-a", qty: 2, price: 50m));
        proj.Apply(Factory.Sold("prod-2", seller: "seller-a", qty: 5, price: 20m));

        var summary = store.Get("seller-a")!;
        Assert.Equal(7,     summary.TotalUnitsSold);
        Assert.Equal(200m,  summary.TotalRevenueCAD);
    }

    [Fact]
    public void ReviewPosted_IsIgnored_BySellerSummaryProjection()
    {
        var (store, proj) = Factory.SellerProjection();
        proj.Apply(Factory.Listed("prod-1", seller: "seller-a"));
        proj.Apply(Factory.Reviewed("prod-1", rating: 5));

        // Review should not alter seller summary
        Assert.Equal(1, store.Get("seller-a")!.ActiveListings);
        Assert.Equal(0, store.Get("seller-a")!.TotalUnitsSold);
    }

    [Fact]
    public void Reset_ClearsAllSellerViews()
    {
        var (store, proj) = Factory.SellerProjection();
        proj.Apply(Factory.Listed("prod-1", seller: "seller-a"));
        proj.Reset();
        Assert.Equal(0, store.Count);
    }
}

// ─── ProjectionEngine ─────────────────────────────────────────────────────────

public class ProjectionEngine_Tests
{
    private static ProjectionEngine BuildEngine(out InMemoryEventStore eventStore)
    {
        eventStore = new InMemoryEventStore();
        return new ProjectionEngine(eventStore);
    }

    [Fact]
    public void Append_StoresEvent_InEventStore()
    {
        var engine = BuildEngine(out var store);
        engine.Append(Factory.Listed("prod-1"));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Append_DispatchesToAllRegisteredProjections()
    {
        var engine = BuildEngine(out _);
        var (cStore, cProj) = Factory.CatalogueProjection();
        var (sStore, sProj) = Factory.SellerProjection();
        engine.Register(cProj);
        engine.Register(sProj);

        engine.Append(Factory.Listed("prod-1", seller: "seller-a"));

        Assert.Equal(1, cStore.Count);
        Assert.Equal(1, sStore.Count);
    }

    [Fact]
    public void Rebuild_ReproducesTheSameReadModel()
    {
        var engine = BuildEngine(out _);
        var (store, proj) = Factory.CatalogueProjection();
        engine.Register(proj);

        engine.Append(Factory.Listed("prod-1", stock: 20));
        engine.Append(Factory.Sold("prod-1", qty: 5));
        engine.Append(Factory.PriceUpdated("prod-1", price: 79m));

        var beforeRebuild = store.Get("prod-1")!;

        engine.Rebuild();

        var afterRebuild = store.Get("prod-1")!;
        Assert.Equal(beforeRebuild.StockRemaining, afterRebuild.StockRemaining);
        Assert.Equal(beforeRebuild.TotalSold,      afterRebuild.TotalSold);
        Assert.Equal(beforeRebuild.PriceCAD,       afterRebuild.PriceCAD);
    }

    [Fact]
    public void Rebuild_AllowsNewProjection_ToCatchUp()
    {
        var engine = BuildEngine(out _);
        var (cStore, cProj) = Factory.CatalogueProjection();
        engine.Register(cProj);

        // Fire events before the new projection is registered
        engine.Append(Factory.Listed("prod-1", stock: 10));
        engine.Append(Factory.Sold("prod-1", qty: 3));

        // Add a second catalogue projection late
        var (lateStore, lateProj) = Factory.CatalogueProjection();
        engine.Register(lateProj);
        Assert.Equal(0, lateStore.Count); // empty before rebuild

        engine.Rebuild();

        var view = lateStore.Get("prod-1")!;
        Assert.Equal(7, view.StockRemaining);
        Assert.Equal(3, view.TotalSold);
    }

    [Fact]
    public void GetStoredEvents_ReturnsEventsInOrder()
    {
        var engine = BuildEngine(out _);
        engine.Append(Factory.Listed("prod-1"));
        engine.Append(Factory.Sold("prod-1"));
        engine.Append(Factory.PriceUpdated("prod-1"));

        var events = engine.GetStoredEvents();
        Assert.Equal(3, events.Count);
        Assert.IsType<ProductListedEvent>(events[0]);
        Assert.IsType<ProductSoldEvent>(events[1]);
        Assert.IsType<ProductPriceUpdatedEvent>(events[2]);
    }

    [Fact]
    public void Rebuild_ResetsProjections_BeforeReplaying()
    {
        var engine = BuildEngine(out _);
        var (store, proj) = Factory.CatalogueProjection();
        engine.Register(proj);

        engine.Append(Factory.Listed("prod-1", stock: 10));
        engine.Rebuild();
        engine.Rebuild(); // second rebuild must not double-count

        Assert.Equal(10, store.Get("prod-1")!.StockRemaining);
    }
}

// ─── MarketplaceService ───────────────────────────────────────────────────────

public class MarketplaceService_Tests
{
    private static MarketplaceService BuildService()
    {
        var catalogueStore = new InMemoryReadModelStore<string, ProductCatalogueView>();
        var sellerStore    = new InMemoryReadModelStore<string, SellerSummaryView>();
        var engine         = new ProjectionEngine(new InMemoryEventStore());
        engine.Register(new ProductCatalogueProjection(catalogueStore));
        engine.Register(new SellerSummaryProjection(sellerStore));
        return new MarketplaceService(engine, catalogueStore, sellerStore);
    }

    [Fact]
    public void ListProduct_CreatesProductInCatalogue()
    {
        var svc = BuildService();
        svc.ListProduct("prod-boots", "seller-mec", "MEC Hiking Boots", 149.95m, 30);

        var view = svc.GetProduct("prod-boots")!;
        Assert.Equal("MEC Hiking Boots", view.Title);
        Assert.Equal(149.95m,            view.PriceCAD);
        Assert.Equal(30,                 view.StockRemaining);
    }

    [Fact]
    public void RecordSale_UpdatesCatalogueAndSellerSummary()
    {
        var svc = BuildService();
        svc.ListProduct("prod-1", "seller-a", "Widget", 50m, 20);
        svc.RecordSale("prod-1", "seller-a", 4, 50m);

        Assert.Equal(16,   svc.GetProduct("prod-1")!.StockRemaining);
        Assert.Equal(200m, svc.GetSellerSummary("seller-a")!.TotalRevenueCAD);
    }

    [Fact]
    public void UpdatePrice_ReflectsInCatalogue()
    {
        var svc = BuildService();
        svc.ListProduct("prod-1", "seller-a", "Widget", 100m, 10);
        svc.UpdatePrice("prod-1", 79.99m);
        Assert.Equal(79.99m, svc.GetProduct("prod-1")!.PriceCAD);
    }

    [Fact]
    public void PostReview_UpdatesAverageRating()
    {
        var svc = BuildService();
        svc.ListProduct("prod-1", "seller-a", "Widget", 100m, 10);
        svc.PostReview("prod-1", 5);
        svc.PostReview("prod-1", 3);

        var view = svc.GetProduct("prod-1")!;
        Assert.Equal(2,   view.ReviewCount);
        Assert.Equal(4.0, view.AverageRating);
    }

    [Fact]
    public void GetProduct_ReturnsNull_ForUnknownId()
    {
        var svc = BuildService();
        Assert.Null(svc.GetProduct("does-not-exist"));
    }

    [Fact]
    public void GetTopSelling_ReturnsByDescendingUnitsSold()
    {
        var svc = BuildService();
        svc.ListProduct("prod-1", "seller-a", "A", 10m, 100);
        svc.ListProduct("prod-2", "seller-a", "B", 10m, 100);
        svc.ListProduct("prod-3", "seller-a", "C", 10m, 100);
        svc.RecordSale("prod-2", "seller-a", 15, 10m);
        svc.RecordSale("prod-3", "seller-a", 5,  10m);
        svc.RecordSale("prod-1", "seller-a", 30, 10m);

        var top = svc.GetTopSelling(2);
        Assert.Equal(2,        top.Count);
        Assert.Equal("prod-1", top[0].ProductId);
        Assert.Equal("prod-2", top[1].ProductId);
    }

    [Fact]
    public void SellerSummary_AggregatesAcrossMultipleProducts()
    {
        var svc = BuildService();
        svc.ListProduct("prod-1", "seller-a", "Widget A", 100m, 50);
        svc.ListProduct("prod-2", "seller-a", "Widget B", 200m, 50);
        svc.RecordSale("prod-1", "seller-a", 3, 100m);
        svc.RecordSale("prod-2", "seller-a", 2, 200m);

        var summary = svc.GetSellerSummary("seller-a")!;
        Assert.Equal(2,      summary.ActiveListings);
        Assert.Equal(5,      summary.TotalUnitsSold);
        Assert.Equal(700.0m, summary.TotalRevenueCAD);
    }
}
