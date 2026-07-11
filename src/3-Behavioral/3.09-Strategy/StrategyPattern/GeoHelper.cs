namespace StrategyPattern;

internal static class GeoHelper
{
    private const double EarthRadiusKm = 6371.0;

    public static double HaversineKm(Location a, Location b)
    {
        var dLat = ToRad(b.Lat - a.Lat);
        var dLng = ToRad(b.Lng - a.Lng);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(a.Lat)) * Math.Cos(ToRad(b.Lat))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(h));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}
