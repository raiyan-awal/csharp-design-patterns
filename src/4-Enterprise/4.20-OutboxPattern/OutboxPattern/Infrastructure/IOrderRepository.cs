using OutboxPattern.Domain;

namespace OutboxPattern.Infrastructure;

public interface IOrderRepository
{
    void Save(Order order);
    Order? FindById(Guid id);
    IReadOnlyList<Order> GetAll();
}
