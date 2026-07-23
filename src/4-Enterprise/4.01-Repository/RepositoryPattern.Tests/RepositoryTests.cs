using Microsoft.Data.Sqlite;
using RepositoryPattern;

namespace RepositoryPattern.Tests;

public class RepositoryTests
{
    private static InMemoryProductRepository EmptyRepo() => new(seedData: false);

    private static Product MakeProduct(string name = "Test Product", string category = "Test",
                                       decimal price = 49.99m, int stock = 10) =>
        new() { Name = name, Category = category, Price = price, StockQuantity = stock };

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProduct()
    {
        var repo    = EmptyRepo();
        var product = MakeProduct("Laptop");
        await repo.AddAsync(product);

        var result = await repo.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal("Laptop", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await EmptyRepo().GetByIdAsync(999);
        Assert.Null(result);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_EmptyRepo_ReturnsEmpty()
    {
        var result = await EmptyRepo().GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_AfterAdding_ReturnsAllProducts()
    {
        var repo = EmptyRepo();
        await repo.AddAsync(MakeProduct("A"));
        await repo.AddAsync(MakeProduct("B"));
        await repo.AddAsync(MakeProduct("C"));

        var result = await repo.GetAllAsync();

        Assert.Equal(3, result.Count());
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_AssignsPositiveId()
    {
        var repo    = EmptyRepo();
        var product = MakeProduct();
        Assert.Equal(0, product.Id);

        await repo.AddAsync(product);

        Assert.True(product.Id > 0);
    }

    [Fact]
    public async Task AddAsync_MultipleProducts_AssignsUniqueIds()
    {
        var repo = EmptyRepo();
        var p1   = MakeProduct("P1");
        var p2   = MakeProduct("P2");

        await repo.AddAsync(p1);
        await repo.AddAsync(p2);

        Assert.NotEqual(p1.Id, p2.Id);
    }

    [Fact]
    public async Task AddAsync_ProductIsRetrievableAfterAdd()
    {
        var repo    = EmptyRepo();
        var product = MakeProduct("Camera", price: 799.99m);
        await repo.AddAsync(product);

        var retrieved = await repo.GetByIdAsync(product.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(799.99m, retrieved.Price);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesAllProperties()
    {
        var repo    = EmptyRepo();
        var product = MakeProduct("Old Name", price: 10m, stock: 5);
        await repo.AddAsync(product);

        product.Name          = "New Name";
        product.Price         = 99m;
        product.StockQuantity = 50;
        await repo.UpdateAsync(product);

        var updated = await repo.GetByIdAsync(product.Id);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal(99m,        updated.Price);
        Assert.Equal(50,         updated.StockQuantity);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentProduct_ThrowsInvalidOperation()
    {
        var ghost = new Product { Id = 999, Name = "Ghost" };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => EmptyRepo().UpdateAsync(ghost));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingProduct_RemovesFromRepo()
    {
        var repo    = EmptyRepo();
        var product = MakeProduct();
        await repo.AddAsync(product);

        await repo.DeleteAsync(product.Id);

        Assert.Null(await repo.GetByIdAsync(product.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => EmptyRepo().DeleteAsync(999));
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        var repo    = EmptyRepo();
        var product = MakeProduct();
        await repo.AddAsync(product);

        Assert.True(await repo.ExistsAsync(product.Id));
    }

    [Fact]
    public async Task ExistsAsync_NonExistentId_ReturnsFalse()
    {
        Assert.False(await EmptyRepo().ExistsAsync(999));
    }

    // ── CountAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAsync_EmptyRepo_ReturnsZero()
    {
        Assert.Equal(0, await EmptyRepo().CountAsync());
    }

    [Fact]
    public async Task CountAsync_AfterAddAndDelete_ReturnsCorrectCount()
    {
        var repo = EmptyRepo();
        var p1   = MakeProduct("P1");
        var p2   = MakeProduct("P2");
        await repo.AddAsync(p1);
        await repo.AddAsync(p2);
        Assert.Equal(2, await repo.CountAsync());

        await repo.DeleteAsync(p1.Id);
        Assert.Equal(1, await repo.CountAsync());
    }

    // ── FindAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FindAsync_ByCategory_ReturnsMatchingProducts()
    {
        var repo = EmptyRepo();
        await repo.AddAsync(MakeProduct("Jacket",  category: "Clothing"));
        await repo.AddAsync(MakeProduct("Laptop",  category: "Electronics"));
        await repo.AddAsync(MakeProduct("T-Shirt", category: "Clothing"));

        var clothing = await repo.FindAsync(p => p.Category == "Clothing");

        Assert.Equal(2, clothing.Count());
        Assert.All(clothing, p => Assert.Equal("Clothing", p.Category));
    }

    [Fact]
    public async Task FindAsync_ByPrice_ReturnsProductsAboveThreshold()
    {
        var repo = EmptyRepo();
        await repo.AddAsync(MakeProduct(price: 50m));
        await repo.AddAsync(MakeProduct(price: 150m));
        await repo.AddAsync(MakeProduct(price: 250m));

        var expensive = await repo.FindAsync(p => p.Price > 100m);

        Assert.Equal(2, expensive.Count());
    }

    [Fact]
    public async Task FindAsync_NoMatches_ReturnsEmpty()
    {
        var repo = EmptyRepo();
        await repo.AddAsync(MakeProduct(category: "Electronics"));

        var result = await repo.FindAsync(p => p.Category == "Furniture");

        Assert.Empty(result);
    }

    // ── Seeded repo ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedData_PopulatesRepo()
    {
        var repo = new InMemoryProductRepository(seedData: true);
        Assert.True(await repo.CountAsync() > 0);
    }
}

// ── SqlProductRepository — same contract, real SQL (SQLite) ───────────────────

public class SqlProductRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqlProductRepository _repo;

    private static Product MakeProduct(string name = "Test", string category = "Test",
                                       decimal price = 49.99m, int stock = 10) =>
        new() { Name = name, Category = category, Price = price, StockQuantity = stock };

    public SqlProductRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _repo = new SqlProductRepository(_connection);
    }

    public void Dispose() => _repo.Dispose();

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        var result = await _repo.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_AssignsPositiveId()
    {
        var product = MakeProduct("Laptop");
        Assert.Equal(0, product.Id);

        await _repo.AddAsync(product);

        Assert.True(product.Id > 0);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProduct()
    {
        var product = MakeProduct("Camera", price: 799.99m);
        await _repo.AddAsync(product);

        var result = await _repo.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal("Camera", result.Name);
        Assert.Equal(799.99m, result.Price, precision: 2);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var product = MakeProduct("Old Name", price: 10m, stock: 5);
        await _repo.AddAsync(product);

        product.Name          = "New Name";
        product.Price         = 99m;
        product.StockQuantity = 50;
        await _repo.UpdateAsync(product);

        var updated = await _repo.GetByIdAsync(product.Id);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal(99m, updated.Price, precision: 2);
        Assert.Equal(50,  updated.StockQuantity);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentProduct_ThrowsInvalidOperation()
    {
        var ghost = new Product { Id = 999, Name = "Ghost", Category = "Test" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.UpdateAsync(ghost));
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_RemovesFromDb()
    {
        var product = MakeProduct();
        await _repo.AddAsync(product);

        await _repo.DeleteAsync(product.Id);

        Assert.Null(await _repo.GetByIdAsync(product.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.DeleteAsync(999));
    }

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        var product = MakeProduct();
        await _repo.AddAsync(product);

        Assert.True(await _repo.ExistsAsync(product.Id));
    }

    [Fact]
    public async Task ExistsAsync_NonExistentId_ReturnsFalse()
    {
        Assert.False(await _repo.ExistsAsync(999));
    }

    [Fact]
    public async Task CountAsync_AfterAddAndDelete_ReturnsCorrectCount()
    {
        var p1 = MakeProduct("P1");
        var p2 = MakeProduct("P2");
        await _repo.AddAsync(p1);
        await _repo.AddAsync(p2);
        Assert.Equal(2, await _repo.CountAsync());

        await _repo.DeleteAsync(p1.Id);
        Assert.Equal(1, await _repo.CountAsync());
    }

    [Fact]
    public async Task FindAsync_ByCategory_ReturnsMatchingProducts()
    {
        await _repo.AddAsync(MakeProduct("Jacket",  category: "Clothing"));
        await _repo.AddAsync(MakeProduct("Laptop",  category: "Electronics"));
        await _repo.AddAsync(MakeProduct("T-Shirt", category: "Clothing"));

        var clothing = await _repo.FindAsync(p => p.Category == "Clothing");

        Assert.Equal(2, clothing.Count());
        Assert.All(clothing, p => Assert.Equal("Clothing", p.Category));
    }

    [Fact]
    public async Task FindAsync_ByPrice_ReturnsProductsAboveThreshold()
    {
        await _repo.AddAsync(MakeProduct(price: 50m));
        await _repo.AddAsync(MakeProduct(price: 150m));
        await _repo.AddAsync(MakeProduct(price: 250m));

        var expensive = await _repo.FindAsync(p => p.Price > 100m);

        Assert.Equal(2, expensive.Count());
    }
}
