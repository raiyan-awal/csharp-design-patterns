namespace ProxyPattern;

public sealed class AuthorizationDocumentProxy : IDocumentRepository
{
    private readonly IDocumentRepository _inner;
    private readonly string _currentUser;
    private readonly HashSet<string> _allowedIds;

    public AuthorizationDocumentProxy(
        IDocumentRepository inner,
        string currentUser,
        IEnumerable<string>? allowedIds = null)
    {
        _inner       = inner;
        _currentUser = currentUser;
        _allowedIds  = allowedIds is not null ? [.. allowedIds] : [];
    }

    public Document? Load(string id)
    {
        if (!_allowedIds.Contains(id))
        {
            Console.WriteLine($"  [AUTH] DENIED  — '{_currentUser}' cannot read '{id}'");
            throw new UnauthorizedAccessException(
                $"User '{_currentUser}' does not have access to document '{id}'.");
        }

        Console.WriteLine($"  [AUTH] ALLOWED — '{_currentUser}' reading '{id}'");
        return _inner.Load(id);
    }

    public IReadOnlyList<string> ListIds()
        => _inner.ListIds().Where(_allowedIds.Contains).ToList();
}
