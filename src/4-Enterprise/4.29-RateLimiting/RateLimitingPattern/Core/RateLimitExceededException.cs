namespace RateLimitingPattern.Core;

public sealed class RateLimitExceededException(string endpoint, int limit)
    : Exception($"Rate limit of {limit} requests exceeded for endpoint '{endpoint}'.")
{
    public string Endpoint { get; } = endpoint;
    public int Limit { get; } = limit;
}
