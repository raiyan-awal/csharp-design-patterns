using SagaPattern.Core;
using SagaPattern.Domain;
using SagaPattern.Services;
using SagaPattern.Steps;

namespace SagaPattern.Tests;

// ── Shared stub ──────────────────────────────────────────────────────────────

file sealed class RecordingStep(
    string      name,
    List<string> log,
    bool        failOnExecute    = false,
    bool        failOnCompensate = false) : ISagaStep<BookingContext>
{
    public string Name => name;

    public void Execute(BookingContext _)
    {
        log.Add($"exec:{name}");
        if (failOnExecute)
            throw new InvalidOperationException($"{name} failed on Execute.");
    }

    public void Compensate(BookingContext _)
    {
        log.Add($"comp:{name}");
        if (failOnCompensate)
            throw new InvalidOperationException($"{name} failed on Compensate.");
    }
}

// ── Suite 1: Happy path ──────────────────────────────────────────────────────

public sealed class SagaSuccessTests
{
    [Fact]
    public void Execute_ReturnsSuccess_WhenAllStepsPass()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log),
            new RecordingStep("B", log),
            new RecordingStep("C", log),
        ]);

        var result = orchestrator.Execute(new BookingContext());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Execute_RunsAllSteps_WhenNoFailure()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log),
            new RecordingStep("B", log),
            new RecordingStep("C", log),
        ]);

        orchestrator.Execute(new BookingContext());

        Assert.Equal(["exec:A", "exec:B", "exec:C"], log);
    }

    [Fact]
    public void Execute_ReturnsSuccess_WhenNoSteps()
    {
        var orchestrator = new SagaOrchestrator<BookingContext>([]);

        var result = orchestrator.Execute(new BookingContext());

        Assert.True(result.IsSuccess);
    }
}

// ── Suite 2: Compensation ────────────────────────────────────────────────────

public sealed class SagaCompensationTests
{
    [Fact]
    public void NoCompensation_WhenFirstStepFails()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log, failOnExecute: true),
            new RecordingStep("B", log),
        ]);

        orchestrator.Execute(new BookingContext());

        Assert.DoesNotContain(log, e => e.StartsWith("comp:"));
    }

    [Fact]
    public void CompensatesStep1_WhenStep2Fails()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log),
            new RecordingStep("B", log, failOnExecute: true),
        ]);

        orchestrator.Execute(new BookingContext());

        Assert.Contains("comp:A", log);
        Assert.DoesNotContain("comp:B", log);
    }

    [Fact]
    public void CompensatesInReverseOrder_WhenStep3Fails()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log),
            new RecordingStep("B", log),
            new RecordingStep("C", log, failOnExecute: true),
        ]);

        orchestrator.Execute(new BookingContext());

        var compEvents = log.Where(e => e.StartsWith("comp:")).ToList();
        Assert.Equal(["comp:B", "comp:A"], compEvents);
    }

    [Fact]
    public void CompensatesInReverseOrder_WhenStep4Fails()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log),
            new RecordingStep("B", log),
            new RecordingStep("C", log),
            new RecordingStep("D", log, failOnExecute: true),
        ]);

        orchestrator.Execute(new BookingContext());

        var compEvents = log.Where(e => e.StartsWith("comp:")).ToList();
        Assert.Equal(["comp:C", "comp:B", "comp:A"], compEvents);
    }

    [Fact]
    public void FailedStepName_IsRecordedInResult()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("Step One", log),
            new RecordingStep("Step Two", log, failOnExecute: true),
        ]);

        var result = orchestrator.Execute(new BookingContext());

        Assert.Equal("Step Two", result.FailedStep);
    }

    [Fact]
    public void FailureException_IsRecordedInResult()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log, failOnExecute: true),
        ]);

        var result = orchestrator.Execute(new BookingContext());

        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationException>(result.Error);
    }

    [Fact]
    public void IsSuccess_IsFalse_WhenAnyStepFails()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log),
            new RecordingStep("B", log, failOnExecute: true),
        ]);

        var result = orchestrator.Execute(new BookingContext());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CompensationError_DoesNotAbortOtherCompensations()
    {
        var log = new List<string>();
        var orchestrator = new SagaOrchestrator<BookingContext>([
            new RecordingStep("A", log),
            new RecordingStep("B", log, failOnCompensate: true),
            new RecordingStep("C", log, failOnExecute: true),
        ]);

        // B throws on Compensate, but A must still be compensated
        orchestrator.Execute(new BookingContext());

        Assert.Contains("comp:A", log);
    }
}

// ── Suite 3: Observability callbacks ─────────────────────────────────────────

public sealed class SagaObservabilityTests
{
    [Fact]
    public void OnExecuted_CalledForEachSuccessfulStep()
    {
        var log     = new List<string>();
        var notified = new List<string>();

        var orchestrator = new SagaOrchestrator<BookingContext>(
            [
                new RecordingStep("A", log),
                new RecordingStep("B", log),
                new RecordingStep("C", log),
            ],
            onExecuted: name => notified.Add(name));

        orchestrator.Execute(new BookingContext());

        Assert.Equal(["A", "B", "C"], notified);
    }

    [Fact]
    public void OnCompensated_CalledForEachCompensatedStep()
    {
        var log      = new List<string>();
        var notified = new List<string>();

        var orchestrator = new SagaOrchestrator<BookingContext>(
            [
                new RecordingStep("A", log),
                new RecordingStep("B", log),
                new RecordingStep("C", log, failOnExecute: true),
            ],
            onCompensated: name => notified.Add(name));

        orchestrator.Execute(new BookingContext());

        Assert.Equal(["B", "A"], notified);
    }

    [Fact]
    public void OnExecuted_NotCalledForFailingStep()
    {
        var log      = new List<string>();
        var notified = new List<string>();

        var orchestrator = new SagaOrchestrator<BookingContext>(
            [
                new RecordingStep("A", log),
                new RecordingStep("B", log, failOnExecute: true),
            ],
            onExecuted: name => notified.Add(name));

        orchestrator.Execute(new BookingContext());

        Assert.DoesNotContain("B", notified);
    }
}

// ── Suite 4: Integration with real steps and services ────────────────────────

public sealed class SagaIntegrationTests
{
    private static (SimulatedFlightService, SimulatedHotelService, SimulatedCarRentalService, SimulatedPaymentService,
                    ISagaStep<BookingContext>[])
        BuildServices()
    {
        var flight  = new SimulatedFlightService();
        var hotel   = new SimulatedHotelService();
        var car     = new SimulatedCarRentalService();
        var payment = new SimulatedPaymentService();
        ISagaStep<BookingContext>[] steps =
        [
            new FlightReservationStep(flight),
            new HotelBookingStep(hotel),
            new CarRentalStep(car),
            new PaymentStep(payment),
        ];
        return (flight, hotel, car, payment, steps);
    }

    private static BookingContext DefaultContext() => new()
    {
        CustomerId  = "CUST-001",
        Destination = "Banff, AB",
        TravelDate  = new DateOnly(2027, 4, 14),
        Passengers  = 2,
        NightCount  = 5,
        DayCount    = 5,
    };

    [Fact]
    public void FullBooking_PopulatesAllRefs_WhenAllServicesHealthy()
    {
        var (_, _, _, _, steps) = BuildServices();
        var orchestrator = new SagaOrchestrator<BookingContext>(steps);
        var context      = DefaultContext();

        var result = orchestrator.Execute(context);

        Assert.True(result.IsSuccess);
        Assert.NotNull(context.FlightRef);
        Assert.NotNull(context.HotelRef);
        Assert.NotNull(context.CarRef);
        Assert.NotNull(context.PaymentRef);
        Assert.True(context.TotalCostCAD > 0);
    }

    [Fact]
    public void PaymentFailure_CompensatesCarHotelAndFlight()
    {
        var (flight, hotel, car, payment, steps) = BuildServices();
        payment.FailOnNextCall();

        var orchestrator = new SagaOrchestrator<BookingContext>(steps);
        var result       = orchestrator.Execute(DefaultContext());

        Assert.False(result.IsSuccess);
        Assert.Equal("Payment", result.FailedStep);
        Assert.Equal(1, flight.CancellationCount);
        Assert.Equal(1, hotel.CancellationCount);
        Assert.Equal(1, car.CancellationCount);
    }

    [Fact]
    public void HotelFailure_CompensatesFlightOnly()
    {
        var (flight, hotel, car, _, steps) = BuildServices();
        hotel.FailOnNextCall();

        var orchestrator = new SagaOrchestrator<BookingContext>(steps);
        orchestrator.Execute(DefaultContext());

        Assert.Equal(1, flight.CancellationCount);
        Assert.Equal(0, hotel.CancellationCount);
        Assert.Equal(0, car.ReservationCount);
    }

    [Fact]
    public void FlightFailure_NoCompensationRuns()
    {
        var (flight, hotel, car, payment, steps) = BuildServices();
        flight.FailOnNextCall();

        var orchestrator = new SagaOrchestrator<BookingContext>(steps);
        orchestrator.Execute(DefaultContext());

        Assert.Equal(0, flight.CancellationCount);
        Assert.Equal(0, hotel.BookingCount);
        Assert.Equal(0, car.ReservationCount);
        Assert.Equal(0, payment.ChargeCount);
    }
}
