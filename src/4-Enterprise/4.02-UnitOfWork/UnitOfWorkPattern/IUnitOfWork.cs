namespace UnitOfWorkPattern;

// Coordinates every repository touched by one business transaction so that
// all of their writes succeed or fail together. Repositories alone commit
// each call immediately; Unit of Work defers every write until CommitAsync,
// and discards them all on RollbackAsync (or if Commit is simply never reached).
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IOrderRepository   Orders   { get; }

    Task CommitAsync();
    Task RollbackAsync();
}
