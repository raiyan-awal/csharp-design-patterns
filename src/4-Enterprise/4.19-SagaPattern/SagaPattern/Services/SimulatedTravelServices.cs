namespace SagaPattern.Services;

public sealed class SimulatedFlightService
{
    private bool _failNext;

    public int ReservationCount  { get; private set; }
    public int CancellationCount { get; private set; }

    public void FailOnNextCall() => _failNext = true;

    public string Reserve(string destination, DateOnly date, int passengers)
    {
        if (_failNext)
        {
            _failNext = false;
            throw new FlightUnavailableException(
                $"No available seats to {destination} on {date:MMM d, yyyy}.");
        }
        ReservationCount++;
        return $"AC{Random.Shared.Next(100, 999)}-{date:MMMyy}".ToUpper();
    }

    public void Cancel(string flightRef)
    {
        CancellationCount++;
    }
}

public sealed class SimulatedHotelService
{
    private bool _failNext;

    public int BookingCount      { get; private set; }
    public int CancellationCount { get; private set; }

    public void FailOnNextCall() => _failNext = true;

    public string Book(string destination, DateOnly checkIn, int nights)
    {
        if (_failNext)
        {
            _failNext = false;
            throw new HotelUnavailableException(
                $"No rooms available in {destination} for {nights} night(s) from {checkIn:MMM d, yyyy}.");
        }
        BookingCount++;
        return $"HTL-{Random.Shared.Next(10000, 99999)}";
    }

    public void Cancel(string hotelRef)
    {
        CancellationCount++;
    }
}

public sealed class SimulatedCarRentalService
{
    private bool _failNext;

    public int ReservationCount  { get; private set; }
    public int CancellationCount { get; private set; }

    public void FailOnNextCall() => _failNext = true;

    public string Reserve(string destination, DateOnly pickUp, int days)
    {
        if (_failNext)
        {
            _failNext = false;
            throw new CarUnavailableException(
                $"No vehicles available in {destination} on {pickUp:MMM d, yyyy}.");
        }
        ReservationCount++;
        return $"CAR-{Random.Shared.Next(10000, 99999)}";
    }

    public void Cancel(string carRef)
    {
        CancellationCount++;
    }
}

public sealed class SimulatedPaymentService
{
    private bool _failNext;

    public int ChargeCount { get; private set; }
    public int RefundCount { get; private set; }

    public void FailOnNextCall() => _failNext = true;

    public string Charge(string customerId, decimal amountCAD)
    {
        if (_failNext)
        {
            _failNext = false;
            throw new PaymentDeclinedException(
                $"Card declined for customer {customerId} — amount ${amountCAD:N2} CAD.");
        }
        ChargeCount++;
        return $"PAY-{Random.Shared.Next(10000, 99999)}";
    }

    public void Refund(string paymentRef, decimal amountCAD)
    {
        RefundCount++;
    }
}
