namespace StrategyPattern;

public sealed class Navigator
{
    private IRouteStrategy _strategy;

    public Navigator(IRouteStrategy strategy) => _strategy = strategy;

    public string CurrentStrategy => _strategy.Name;

    public void SetStrategy(IRouteStrategy strategy)
    {
        Console.WriteLine($"  Switching strategy: {_strategy.Name} → {strategy.Name}");
        _strategy = strategy;
    }

    public Route GetRoute(Location origin, Location destination)
    {
        Console.WriteLine($"  [{_strategy.Name}] {origin.Name} → {destination.Name}");
        var route = _strategy.Calculate(origin, destination);
        Console.WriteLine($"  Distance : {route.DistanceKm:F1} km");
        Console.WriteLine($"  Duration : {FormatDuration(route.Duration)}");
        Console.WriteLine($"  Cost     : {FormatCost(route.Cost)}");
        foreach (var step in route.Steps)
            Console.WriteLine($"    • {step}");
        return route;
    }

    private static string FormatDuration(TimeSpan t)
        => t.TotalHours >= 1
            ? $"{(int)t.TotalHours}h {t.Minutes:D2}m"
            : $"{t.Minutes}m";

    private static string FormatCost(decimal cost)
        => cost == 0 ? "Free" : $"${cost:F2}";
}
