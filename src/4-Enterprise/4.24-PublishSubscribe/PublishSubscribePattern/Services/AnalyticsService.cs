using PublishSubscribePattern.Domain;
using PublishSubscribePattern.Events;

namespace PublishSubscribePattern.Services;

public sealed class AnalyticsService
{
    private readonly Dictionary<ArticleCategory, int> _publishesByCategory = [];

    public int TotalPublished { get; private set; }
    public int TotalUpdated { get; private set; }
    public IReadOnlyDictionary<ArticleCategory, int> PublishesByCategory => _publishesByCategory;

    public void OnArticlePublished(ArticlePublishedEvent @event)
    {
        TotalPublished++;
        _publishesByCategory.TryGetValue(@event.Article.Category, out var count);
        _publishesByCategory[@event.Article.Category] = count + 1;
        Console.WriteLine($"[Analytics]      Published tracked — category: {@event.Article.Category}");
    }

    public void OnArticleUpdated(ArticleUpdatedEvent @event)
    {
        TotalUpdated++;
        Console.WriteLine($"[Analytics]      Update tracked — reason: {@event.ChangeReason}");
    }

    public void PrintSummary()
    {
        Console.WriteLine($"  Total published : {TotalPublished}");
        Console.WriteLine($"  Total updated   : {TotalUpdated}");
        foreach (var (cat, cnt) in _publishesByCategory)
            Console.WriteLine($"  {cat,-16}: {cnt}");
    }
}
