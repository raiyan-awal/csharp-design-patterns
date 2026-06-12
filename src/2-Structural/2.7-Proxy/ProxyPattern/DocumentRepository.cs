namespace ProxyPattern;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly Dictionary<string, Document> _store;

    public int LoadCallCount { get; private set; }

    public DocumentRepository(IEnumerable<Document>? seed = null)
    {
        _store = (seed ?? DefaultDocuments()).ToDictionary(d => d.Id);
        Console.WriteLine("  [REPO] DocumentRepository initialised");
    }

    public Document? Load(string id)
    {
        LoadCallCount++;
        Console.WriteLine($"  [REPO] Loading '{id}' from storage...");
        return _store.TryGetValue(id, out var doc) ? doc : null;
    }

    public IReadOnlyList<string> ListIds() => [.. _store.Keys];

    private static IEnumerable<Document> DefaultDocuments() =>
    [
        new("doc-1", "Q1 Report",     "Revenue: $1.2M, EBITDA: $340K",        "alice"),
        new("doc-2", "HR Policy",     "Annual leave: 20 days per year",        "hr"),
        new("doc-3", "Tech Roadmap",  "Q3: migrate to .NET 10, add telemetry", "bob"),
        new("doc-4", "Board Minutes", "Confidential: acquisition target X",    "board"),
    ];
}
