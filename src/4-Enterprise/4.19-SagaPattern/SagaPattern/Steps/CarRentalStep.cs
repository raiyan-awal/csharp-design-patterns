using SagaPattern.Core;
using SagaPattern.Domain;
using SagaPattern.Services;

namespace SagaPattern.Steps;

public sealed class CarRentalStep(SimulatedCarRentalService service) : ISagaStep<BookingContext>
{
    public string Name => "Car Rental";

    public void Execute(BookingContext context)
    {
        context.CarRef        = service.Reserve(context.Destination, context.TravelDate, context.DayCount);
        context.TotalCostCAD += 89m * context.DayCount;
    }

    public void Compensate(BookingContext context)
    {
        if (context.CarRef is not null)
            service.Cancel(context.CarRef);
    }
}
