namespace PublishSubscribePattern.Domain;

public sealed record Article(
    Guid Id,
    string Title,
    string Body,
    ArticleCategory Category,
    string Author,
    DateTimeOffset PublishedAt
);
