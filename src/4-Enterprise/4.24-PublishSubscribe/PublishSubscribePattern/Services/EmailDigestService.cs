using PublishSubscribePattern.Domain;
using PublishSubscribePattern.Events;

namespace PublishSubscribePattern.Services;

public sealed class EmailDigestService
{
    private readonly List<Article> _pending = [];

    public IReadOnlyList<Article> Pending => _pending;

    public void OnArticlePublished(ArticlePublishedEvent @event)
    {
        _pending.Add(@event.Article);
        Console.WriteLine($"[Email Digest]   Queued '{@event.Article.Title}' for next digest.");
    }

    public IReadOnlyList<Article> FlushDigest()
    {
        var batch = _pending.ToList();
        _pending.Clear();
        return batch;
    }
}
