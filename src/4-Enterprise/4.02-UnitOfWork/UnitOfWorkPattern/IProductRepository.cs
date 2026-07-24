namespace UnitOfWorkPattern;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task           UpdateAsync(Product product);
}
