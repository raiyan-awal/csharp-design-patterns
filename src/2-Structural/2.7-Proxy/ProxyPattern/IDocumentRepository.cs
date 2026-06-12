namespace ProxyPattern;

public interface IDocumentRepository
{
    Document? Load(string id);
    IReadOnlyList<string> ListIds();
}
