using PublishSubscribePattern.Domain;

namespace PublishSubscribePattern.Events;

public sealed record ArticlePublishedEvent(
    Article Article,
    DateTimeOffset OccurredAt
);
