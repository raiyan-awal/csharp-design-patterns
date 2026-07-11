namespace StrategyPattern;

public sealed class DrivingStrategy : IRouteStrategy
{
    private readonly double  _pathMultiplier;
    private readonly double  _avgSpeedKmh;
    private readonly decimal _costPerKm;

    public DrivingStrategy(
        double  pathMultiplier = 1.25,
        double  avgSpeedKmh   = 70.0,
        decimal costPerKm     = 0.18m)
    {
        _pathMultiplier = pathMultiplier;
        _avgSpeedKmh    = avgSpeedKmh;
        _costPerKm      = costPerKm;
    }

    public string Name => "Driving";

    public Route Calculate(Location origin, Location destination)
    {
        var straight = GeoHelper.HaversineKm(origin, destination);
        var distance = straight * _pathMultiplier;
        var hours    = distance / _avgSpeedKmh;
        var cost     = (decimal)distance * _costPerKm;

        return new Route(
            Mode:        Name,
            DistanceKm:  Math.Round(distance, 1),
            Duration:    TimeSpan.FromHours(hours),
            Cost:        Math.Round(cost, 2),
            Steps: [
                $"Head to the nearest on-ramp from {origin.Name}",
                $"Take the highway toward {destination.Name}",
                $"Exit and arrive at {destination.Name}"
            ]);
    }
}
