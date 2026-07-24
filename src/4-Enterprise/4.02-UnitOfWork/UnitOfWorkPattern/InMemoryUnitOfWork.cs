namespace UnitOfWorkPattern;

// In-memory Unit of Work. Every Products.UpdateAsync / Orders.AddAsync call
// only stages a change — nothing touches the shared InMemoryDataStore until
// CommitAsync applies every staged change under one lock. If the caller
// never reaches Commit (an exception mid-transaction, or an explicit
// RollbackAsync), the staged changes are simply discarded and the store is
// left exactly as it was.
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly InMemoryDataStore _store;
    private readonly Dictionary<int, Product> _stagedProducts = [];
    private readonly List<Order> _stagedOrders = [];
    private bool _completed;

    public InMemoryUnitOfWork(InMemoryDataStore store)
    {
        _store = store;
        Products = new StagedProductRepository(_store, _stagedProducts);
        Orders   = new StagedOrderRepository(_store, _stagedOrders);
    }

    public IProductRepository Products { get; }
    public IOrderRepository   Orders   { get; }

    public Task CommitAsync()
    {
        lock (_store.Gate)
        {
            foreach (var staged in _stagedProducts.Values)
            {
                var existing = _store.Products.FirstOrDefault(p => p.Id == staged.Id)
                    ?? throw new InvalidOperationException($"Product #{staged.Id} not found");
                existing.Name          = staged.Name;
                existing.Price         = staged.Price;
                existing.StockQuantity = staged.StockQuantity;
            }

            foreach (var order in _stagedOrders)
            {
                order.Id = _store.NextOrderId++;
                _store.Orders.Add(order);
            }

            Console.WriteLine($"  [UoW]  Committed → {_stagedOrders.Count} order(s), {_stagedProducts.Count} product update(s)");
        }

        _stagedProducts.Clear();
        _stagedOrders.Clear();
        _completed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        Console.WriteLine($"  [UoW]  Rolled back → discarded {_stagedOrders.Count} order(s), {_stagedProducts.Count} product update(s)");
        _stagedProducts.Clear();
        _stagedOrders.Clear();
        _completed = true;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_completed && (_stagedProducts.Count > 0 || _stagedOrders.Count > 0))
            Console.WriteLine("  [UoW]  Disposed without Commit/Rollback — staged changes discarded");
    }

    // Reads fall through to the store; writes are staged in the dictionary/list
    // owned by the enclosing Unit of Work, never applied directly.
    private sealed class StagedProductRepository(InMemoryDataStore store, Dictionary<int, Product> staged) : IProductRepository
    {
        public Task<Product?> GetByIdAsync(int id)
        {
            if (staged.TryGetValue(id, out var pending))
                return Task.FromResult<Product?>(pending.Clone());

            var product = store.Products.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(product?.Clone());
        }

        public Task UpdateAsync(Product product)
        {
            staged[product.Id] = product.Clone();
            return Task.CompletedTask;
        }
    }

    private sealed class StagedOrderRepository(InMemoryDataStore store, List<Order> staged) : IOrderRepository
    {
        public Task<Order?> GetByIdAsync(int id)
        {
            var pending = staged.FirstOrDefault(o => o.Id == id);
            if (pending != null) return Task.FromResult<Order?>(pending);

            return Task.FromResult(store.Orders.FirstOrDefault(o => o.Id == id));
        }

        public Task AddAsync(Order order)
        {
            staged.Add(order);
            return Task.CompletedTask;
        }
    }
}
