namespace HostedServicePattern.Domain;

public sealed record MetricsSnapshot(DateTimeOffset TakenAt, int OrderCount, decimal TotalRevenueCAD);
