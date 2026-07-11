namespace StrategyPattern;

public record Route(
    string Mode,
    double DistanceKm,
    TimeSpan Duration,
    decimal Cost,
    IReadOnlyList<string> Steps);
