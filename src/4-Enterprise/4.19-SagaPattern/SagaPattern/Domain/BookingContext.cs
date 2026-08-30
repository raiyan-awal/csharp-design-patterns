namespace SagaPattern.Domain;

public sealed class BookingContext
{
    public string   CustomerId   { get; init; } = "";
    public string   CustomerName { get; init; } = "";
    public string   Destination  { get; init; } = "";
    public DateOnly TravelDate   { get; init; }
    public int      Passengers   { get; init; }
    public int      NightCount   { get; init; }
    public int      DayCount     { get; init; }

    // Populated by saga steps as they execute
    public string?  FlightRef    { get; set; }
    public string?  HotelRef     { get; set; }
    public string?  CarRef       { get; set; }
    public string?  PaymentRef   { get; set; }
    public decimal  TotalCostCAD { get; set; }
}
