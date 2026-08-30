using SagaPattern.Core;
using SagaPattern.Domain;
using SagaPattern.Services;

namespace SagaPattern.Steps;

public sealed class PaymentStep(SimulatedPaymentService service) : ISagaStep<BookingContext>
{
    public string Name => "Payment";

    public void Execute(BookingContext context)
    {
        context.PaymentRef = service.Charge(context.CustomerId, context.TotalCostCAD);
    }

    public void Compensate(BookingContext context)
    {
        if (context.PaymentRef is not null)
            service.Refund(context.PaymentRef, context.TotalCostCAD);
    }
}
