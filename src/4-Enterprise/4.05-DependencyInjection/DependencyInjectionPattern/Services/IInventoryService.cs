namespace DependencyInjectionPattern;

public interface IInventoryService
{
    Guid                   InstanceId { get; }
    IReadOnlyList<Product> GetAll();
    Product?               GetById(int id);
    bool                   IsInStock(int productId, int quantity = 1);
    bool                   Reserve(int productId, int quantity = 1);
}
