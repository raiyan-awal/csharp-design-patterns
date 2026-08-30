using OutboxPattern.Domain;

namespace OutboxPattern.Infrastructure;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _store     = [];
    private bool                             _failNext;

    public int Count => _store.Count;

    public void FailOnNextSave() => _failNext = true;

    public void Save(Order order)
    {
        if (_failNext)
        {
            _failNext = false;
            throw new InvalidOperationException("Database write failed.");
        }
        _store[order.Id] = order;
    }

    public Order? FindById(Guid id)          => _store.GetValueOrDefault(id);
    public IReadOnlyList<Order> GetAll()     => [.. _store.Values];
}
