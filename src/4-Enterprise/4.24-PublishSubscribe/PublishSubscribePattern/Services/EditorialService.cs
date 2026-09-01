using PublishSubscribePattern.Core;
using PublishSubscribePattern.Domain;
using PublishSubscribePattern.Events;

namespace PublishSubscribePattern.Services;

public sealed class EditorialService(IEventBus bus)
{
    private readonly Dictionary<Guid, Article> _articles = [];

    public IReadOnlyDictionary<Guid, Article> Articles => _articles;

    public Article PublishArticle(string title, string body, ArticleCategory category, string author)
    {
        var article = new Article(Guid.NewGuid(), title, body, category, author, DateTimeOffset.UtcNow);
        _articles[article.Id] = article;

        bus.Publish(new ArticlePublishedEvent(article, DateTimeOffset.UtcNow));

        if (category == ArticleCategory.BreakingNews)
            bus.Publish(new BreakingNewsAlertEvent(article, $"BREAKING: {title}", DateTimeOffset.UtcNow));

        return article;
    }

    public Article UpdateArticle(Guid id, string newBody, string changeReason)
    {
        if (!_articles.TryGetValue(id, out var original))
            throw new KeyNotFoundException($"Article {id} not found.");

        var updated = original with { Body = newBody };
        _articles[id] = updated;

        bus.Publish(new ArticleUpdatedEvent(original, updated, changeReason, DateTimeOffset.UtcNow));

        return updated;
    }
}
