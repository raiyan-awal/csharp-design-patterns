namespace RateLimitingPattern.Core;

public interface IRateLimiter
{
    // Returns true if the request is permitted; false if the limit has been exceeded.
    bool TryAcquire();

    int Available { get; }   // permits/tokens remaining right now
    int Limit { get; }       // maximum permits per window or bucket capacity
    string Algorithm { get; }
}
