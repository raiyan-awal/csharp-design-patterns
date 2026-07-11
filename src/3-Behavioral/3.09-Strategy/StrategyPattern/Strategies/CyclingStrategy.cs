namespace StrategyPattern;

public sealed class CyclingStrategy : IRouteStrategy
{
    private readonly double _pathMultiplier;
    private readonly double _avgSpeedKmh;

    public CyclingStrategy(double pathMultiplier = 1.10, double avgSpeedKmh = 18.0)
    {
        _pathMultiplier = pathMultiplier;
        _avgSpeedKmh    = avgSpeedKmh;
    }

    public string Name => "Cycling";

    public Route Calculate(Location origin, Location destination)
    {
        var straight = GeoHelper.HaversineKm(origin, destination);
        var distance = straight * _pathMultiplier;
        var hours    = distance / _avgSpeedKmh;

        return new Route(
            Mode:        Name,
            DistanceKm:  Math.Round(distance, 1),
            Duration:    TimeSpan.FromHours(hours),
            Cost:        0m,
            Steps: [
                $"Join the bike lane from {origin.Name}",
                "Follow the cycling trail",
                $"Arrive at {destination.Name}"
            ]);
    }
}
