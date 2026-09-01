using PublishSubscribePattern.Domain;

namespace PublishSubscribePattern.Events;

public sealed record BreakingNewsAlertEvent(
    Article Article,
    string AlertHeadline,
    DateTimeOffset OccurredAt
);
