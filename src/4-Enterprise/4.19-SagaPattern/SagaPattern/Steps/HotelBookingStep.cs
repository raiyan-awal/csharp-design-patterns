using SagaPattern.Core;
using SagaPattern.Domain;
using SagaPattern.Services;

namespace SagaPattern.Steps;

public sealed class HotelBookingStep(SimulatedHotelService service) : ISagaStep<BookingContext>
{
    public string Name => "Hotel Booking";

    public void Execute(BookingContext context)
    {
        context.HotelRef      = service.Book(context.Destination, context.TravelDate, context.NightCount);
        context.TotalCostCAD += 219m * context.NightCount;
    }

    public void Compensate(BookingContext context)
    {
        if (context.HotelRef is not null)
            service.Cancel(context.HotelRef);
    }
}
