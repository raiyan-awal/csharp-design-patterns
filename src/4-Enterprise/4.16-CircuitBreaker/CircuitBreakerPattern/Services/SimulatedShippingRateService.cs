namespace CircuitBreakerPattern.Services;

public sealed class SimulatedShippingRateService : IShippingRateService
{
    private bool _failing   = false;
    private int  _callCount = 0;

    public int  CallCount => _callCount;
    public bool IsHealthy => !_failing;

    public void SetHealthy()  => _failing = false;
    public void SetFailing()  => _failing = true;

    public ShippingRate GetRate(string origin, string destination, decimal weightKg)
    {
        _callCount++;

        if (_failing)
            throw new HttpRequestException(
                "Canada Post Rate API is currently unavailable (503 Service Unavailable).");

        var baseRate = destination.Contains("BC") || destination.Contains("AB") ? 14.99m : 9.99m;
        var price    = Math.Round(baseRate + (weightKg * 2.50m), 2);

        return new ShippingRate("Canada Post", "Expedited Parcel", price, EstimatedDays(origin, destination));
    }

    private static int EstimatedDays(string origin, string destination) =>
        (origin.Contains("ON") && destination.Contains("BC")) ? 5 :
        (origin.Contains("QC") && destination.Contains("AB")) ? 5 : 3;
}
