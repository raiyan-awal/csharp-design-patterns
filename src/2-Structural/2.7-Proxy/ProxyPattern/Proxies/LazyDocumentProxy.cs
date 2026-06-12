namespace ProxyPattern;

public sealed class LazyDocumentProxy : IDocumentRepository
{
    private readonly Func<IDocumentRepository> _factory;
    private IDocumentRepository? _inner;

    public bool IsInitialized => _inner is not null;

    public LazyDocumentProxy(Func<IDocumentRepository>? factory = null)
        => _factory = factory ?? (() => new DocumentRepository());

    private IDocumentRepository Inner => _inner ??= _factory();

    public Document? Load(string id)   => Inner.Load(id);
    public IReadOnlyList<string> ListIds() => Inner.ListIds();
}
