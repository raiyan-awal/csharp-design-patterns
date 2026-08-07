using SpecificationPattern;

namespace SpecificationPattern.Tests;

public class SpecificationTests
{
    private static Product Active(decimal price = 100m, int stock = 10, string cat = "Electronics",
                                   double rating = 4.0, bool active = true)
        => new() { Id = 1, Name = "Test", Category = cat, Price = price,
                   StockQuantity = stock, IsActive = active, Rating = rating };

    // ── ActiveSpecification ───────────────────────────────────────────────────

    [Fact]
    public void Active_ActiveProduct_ReturnsTrue()
        => Assert.True(new ActiveSpecification().IsSatisfiedBy(Active()));

    [Fact]
    public void Active_InactiveProduct_ReturnsFalse()
        => Assert.False(new ActiveSpecification().IsSatisfiedBy(Active(active: false)));

    // ── InStockSpecification ──────────────────────────────────────────────────

    [Fact]
    public void InStock_StockAboveZero_ReturnsTrue()
        => Assert.True(new InStockSpecification().IsSatisfiedBy(Active(stock: 1)));

    [Fact]
    public void InStock_ZeroStock_ReturnsFalse()
        => Assert.False(new InStockSpecification().IsSatisfiedBy(Active(stock: 0)));

    // ── CategorySpecification ─────────────────────────────────────────────────

    [Fact]
    public void Category_MatchingCategory_ReturnsTrue()
        => Assert.True(new CategorySpecification("Electronics").IsSatisfiedBy(Active(cat: "Electronics")));

    [Fact]
    public void Category_DifferentCategory_ReturnsFalse()
        => Assert.False(new CategorySpecification("Clothing").IsSatisfiedBy(Active(cat: "Electronics")));

    // ── PriceRangeSpecification ───────────────────────────────────────────────

    [Fact]
    public void PriceRange_PriceWithinRange_ReturnsTrue()
        => Assert.True(new PriceRangeSpecification(50m, 200m).IsSatisfiedBy(Active(price: 100m)));

    [Fact]
    public void PriceRange_PriceBelowMin_ReturnsFalse()
        => Assert.False(new PriceRangeSpecification(200m, 500m).IsSatisfiedBy(Active(price: 100m)));

    [Fact]
    public void PriceRange_PriceAboveMax_ReturnsFalse()
        => Assert.False(new PriceRangeSpecification(0m, 50m).IsSatisfiedBy(Active(price: 100m)));

    [Fact]
    public void PriceRange_BoundaryValues_ReturnTrue()
    {
        var spec = new PriceRangeSpecification(100m, 200m);
        Assert.True(spec.IsSatisfiedBy(Active(price: 100m)));
        Assert.True(spec.IsSatisfiedBy(Active(price: 200m)));
    }

    // ── MinRatingSpecification ────────────────────────────────────────────────

    [Fact]
    public void MinRating_RatingAtThreshold_ReturnsTrue()
        => Assert.True(new MinRatingSpecification(4.5).IsSatisfiedBy(Active(rating: 4.5)));

    [Fact]
    public void MinRating_RatingBelowThreshold_ReturnsFalse()
        => Assert.False(new MinRatingSpecification(4.5).IsSatisfiedBy(Active(rating: 4.4)));

    // ── LowStockSpecification ─────────────────────────────────────────────────

    [Fact]
    public void LowStock_StockWithinThreshold_ReturnsTrue()
        => Assert.True(new LowStockSpecification(10).IsSatisfiedBy(Active(stock: 5)));

    [Fact]
    public void LowStock_ZeroStock_ReturnsFalse()
        => Assert.False(new LowStockSpecification(10).IsSatisfiedBy(Active(stock: 0)));

    [Fact]
    public void LowStock_StockAboveThreshold_ReturnsFalse()
        => Assert.False(new LowStockSpecification(10).IsSatisfiedBy(Active(stock: 11)));

    // ── And ───────────────────────────────────────────────────────────────────

    [Fact]
    public void And_BothSatisfied_ReturnsTrue()
    {
        var spec = new ActiveSpecification().And(new InStockSpecification());
        Assert.True(spec.IsSatisfiedBy(Active(active: true, stock: 5)));
    }

    [Fact]
    public void And_OneFails_ReturnsFalse()
    {
        var spec = new ActiveSpecification().And(new InStockSpecification());
        Assert.False(spec.IsSatisfiedBy(Active(active: true, stock: 0)));
    }

    [Fact]
    public void And_ChainedThree_AllMustPass()
    {
        var spec = new ActiveSpecification()
            .And(new InStockSpecification())
            .And(new CategorySpecification("Electronics"));

        Assert.True (spec.IsSatisfiedBy(Active(active: true, stock: 5, cat: "Electronics")));
        Assert.False(spec.IsSatisfiedBy(Active(active: true, stock: 5, cat: "Clothing")));
    }

    // ── Or ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Or_EitherSatisfied_ReturnsTrue()
    {
        var spec = new CategorySpecification("Electronics").Or(new CategorySpecification("Fitness"));
        Assert.True(spec.IsSatisfiedBy(Active(cat: "Electronics")));
        Assert.True(spec.IsSatisfiedBy(Active(cat: "Fitness")));
    }

    [Fact]
    public void Or_NeitherSatisfied_ReturnsFalse()
    {
        var spec = new CategorySpecification("Electronics").Or(new CategorySpecification("Fitness"));
        Assert.False(spec.IsSatisfiedBy(Active(cat: "Clothing")));
    }

    // ── Not ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Not_Satisfied_ReturnsFalse()
        => Assert.False(new ActiveSpecification().Not().IsSatisfiedBy(Active(active: true)));

    [Fact]
    public void Not_NotSatisfied_ReturnsTrue()
        => Assert.True(new ActiveSpecification().Not().IsSatisfiedBy(Active(active: false)));

    // ── ToExpression ──────────────────────────────────────────────────────────

    [Fact]
    public void ToExpression_CompiledMatchesIsSatisfiedBy()
    {
        var spec     = new ActiveSpecification().And(new InStockSpecification());
        var compiled = spec.ToExpression().Compile();
        var product  = Active(active: true, stock: 5);

        Assert.Equal(spec.IsSatisfiedBy(product), compiled(product));
    }

    // ── Repository integration ────────────────────────────────────────────────

    [Fact]
    public void Repository_Find_ReturnsMatchingProducts()
    {
        var repo = new ProductRepository(
        [
            Active(cat: "Electronics", active: true,  stock: 10),
            Active(cat: "Clothing",    active: true,  stock: 10),
            Active(cat: "Electronics", active: false, stock: 10),
        ]);

        var results = repo.Find(new ActiveSpecification().And(new CategorySpecification("Electronics")));
        Assert.Single(results);
    }

    [Fact]
    public void Repository_Any_ReturnsTrueWhenMatch()
    {
        var repo = new ProductRepository([Active(active: true)]);
        Assert.True(repo.Any(new ActiveSpecification()));
    }

    [Fact]
    public void Repository_Count_ReturnsCorrectCount()
    {
        var repo = new ProductRepository(
        [
            Active(stock: 5),
            Active(stock: 0),
            Active(stock: 3),
        ]);
        Assert.Equal(2, repo.Count(new LowStockSpecification(10)));
    }
}
