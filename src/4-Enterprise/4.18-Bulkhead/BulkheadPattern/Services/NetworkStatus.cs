namespace BulkheadPattern.Services;

public sealed record NetworkStatus(string Region, string Status, int TowersOnline, int TowersTotal);
