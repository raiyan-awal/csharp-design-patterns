using SagaPattern.Core;
using SagaPattern.Domain;
using SagaPattern.Services;

namespace SagaPattern.Steps;

public sealed class FlightReservationStep(SimulatedFlightService service) : ISagaStep<BookingContext>
{
    public string Name => "Flight Reservation";

    public void Execute(BookingContext context)
    {
        context.FlightRef     = service.Reserve(context.Destination, context.TravelDate, context.Passengers);
        context.TotalCostCAD += 649m * context.Passengers;
    }

    public void Compensate(BookingContext context)
    {
        if (context.FlightRef is not null)
            service.Cancel(context.FlightRef);
    }
}
