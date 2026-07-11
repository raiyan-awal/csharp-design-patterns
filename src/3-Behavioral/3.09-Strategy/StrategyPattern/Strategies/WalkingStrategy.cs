namespace StrategyPattern;

public sealed class WalkingStrategy : IRouteStrategy
{
    private readonly double _pathMultiplier;
    private readonly double _avgSpeedKmh;

    public WalkingStrategy(double pathMultiplier = 1.05, double avgSpeedKmh = 5.0)
    {
        _pathMultiplier = pathMultiplier;
        _avgSpeedKmh    = avgSpeedKmh;
    }

    public string Name => "Walking";

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
                $"Walk south from {origin.Name}",
                "Continue along the main street",
                $"Arrive at {destination.Name}"
            ]);
    }
}
