using RateLimitingPattern.Core;

namespace RateLimitingPattern.Middleware;

// Wraps a rate limiter around a named endpoint.
// Every HandleRequest call consults the limiter before processing.
public sealed class ApiGateway(IRateLimiter limiter, string endpoint)
{
    public string Endpoint { get; } = endpoint;
    public int RequestsHandled  { get; private set; }
    public int RequestsRejected { get; private set; }
    public int TotalRequests    => RequestsHandled + RequestsRejected;

    public bool HandleRequest()
    {
        if (!limiter.TryAcquire())
        {
            RequestsRejected++;
            return false;
        }
        RequestsHandled++;
        return true;
    }

    public int Available => limiter.Available;
}
