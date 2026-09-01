using PublishSubscribePattern.Domain;

namespace PublishSubscribePattern.Events;

public sealed record ArticleUpdatedEvent(
    Article Original,
    Article Updated,
    string ChangeReason,
    DateTimeOffset OccurredAt
);
