using PublishSubscribePattern.Domain;
using PublishSubscribePattern.Events;

namespace PublishSubscribePattern.Services;

public sealed class ContentArchiveService
{
    private readonly Dictionary<Guid, Article> _archive = [];
    private readonly List<(Guid ArticleId, string Event, DateTimeOffset At)> _log = [];

    public IReadOnlyDictionary<Guid, Article> Archive => _archive;
    public IReadOnlyList<(Guid ArticleId, string Event, DateTimeOffset At)> Log => _log;

    public void OnArticlePublished(ArticlePublishedEvent @event)
    {
        _archive[@event.Article.Id] = @event.Article;
        _log.Add((@event.Article.Id, "Published", @event.OccurredAt));
        Console.WriteLine($"[Archive]        Stored '{@event.Article.Title}'");
    }

    public void OnArticleUpdated(ArticleUpdatedEvent @event)
    {
        _archive[@event.Updated.Id] = @event.Updated;
        _log.Add((@event.Updated.Id, $"Updated: {@event.ChangeReason}", @event.OccurredAt));
        Console.WriteLine($"[Archive]        Revised '{@event.Updated.Title}' — {@event.ChangeReason}");
    }
}
