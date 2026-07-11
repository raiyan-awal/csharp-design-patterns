namespace StrategyPattern;

public sealed class PublicTransitStrategy : IRouteStrategy
{
    private readonly double  _pathMultiplier;
    private readonly double  _avgSpeedKmh;
    private readonly decimal _flatFare;

    public PublicTransitStrategy(
        double  pathMultiplier = 1.40,
        double  avgSpeedKmh   = 28.0,
        decimal flatFare      = 3.30m)
    {
        _pathMultiplier = pathMultiplier;
        _avgSpeedKmh    = avgSpeedKmh;
        _flatFare       = flatFare;
    }

    public string Name => "Public Transit";

    public Route Calculate(Location origin, Location destination)
    {
        var straight = GeoHelper.HaversineKm(origin, destination);
        var distance = straight * _pathMultiplier;
        var hours    = distance / _avgSpeedKmh;

        return new Route(
            Mode:        Name,
            DistanceKm:  Math.Round(distance, 1),
            Duration:    TimeSpan.FromHours(hours),
            Cost:        _flatFare,
            Steps: [
                $"Walk to the nearest TTC stop from {origin.Name}",
                "Board the subway or streetcar",
                "Transfer if required",
                $"Alight and walk to {destination.Name}"
            ]);
    }
}
