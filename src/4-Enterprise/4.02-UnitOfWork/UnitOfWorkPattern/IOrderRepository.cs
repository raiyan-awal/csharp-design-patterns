namespace UnitOfWorkPattern;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task         AddAsync(Order order);
}
