using SagaPattern.Core;
using SagaPattern.Domain;
using SagaPattern.Services;
using SagaPattern.Steps;

Console.WriteLine("=== Maple Travel — Saga Pattern Demo ===\n");

var flightSvc  = new SimulatedFlightService();
var hotelSvc   = new SimulatedHotelService();
var carSvc     = new SimulatedCarRentalService();
var paymentSvc = new SimulatedPaymentService();

var flightStep  = new FlightReservationStep(flightSvc);
var hotelStep   = new HotelBookingStep(hotelSvc);
var carStep     = new CarRentalStep(carSvc);
var paymentStep = new PaymentStep(paymentSvc);

ISagaStep<BookingContext>[] allSteps = [flightStep, hotelStep, carStep, paymentStep];

// ── Section 1: Successful Booking ────────────────────────────────────────────
Console.WriteLine("--- Section 1: Successful Vacation Package ---");

var ctx1 = new BookingContext
{
    CustomerId   = "CUST-001",
    CustomerName = "Alice Tremblay",
    Destination  = "Banff, AB",
    TravelDate   = new DateOnly(2027, 4, 14),
    Passengers   = 2,
    NightCount   = 5,
    DayCount     = 5,
};

var executed1     = new List<string>();
var compensated1  = new List<string>();
var orchestrator1 = new SagaOrchestrator<BookingContext>(
    allSteps,
    onExecuted:    name => executed1.Add(name),
    onCompensated: name => compensated1.Add(name));

var result1 = orchestrator1.Execute(ctx1);

foreach (var s in executed1)
    Console.WriteLine($"  ✓ {s}");

if (result1.IsSuccess)
{
    Console.WriteLine();
    Console.WriteLine($"  Booking confirmed!");
    Console.WriteLine($"  Customer    : {ctx1.CustomerName} ({ctx1.CustomerId})");
    Console.WriteLine($"  Destination : {ctx1.Destination} — Apr 14, 2027");
    Console.WriteLine($"  Flight      : {ctx1.FlightRef}");
    Console.WriteLine($"  Hotel       : {ctx1.HotelRef}  ({ctx1.NightCount} nights × $219)");
    Console.WriteLine($"  Car         : {ctx1.CarRef}  ({ctx1.DayCount} days × $89)");
    Console.WriteLine($"  Payment     : {ctx1.PaymentRef}  | Total ${ctx1.TotalCostCAD:N2} CAD");
}

Pause();

// ── Section 2: Payment Failure — Full Compensation ────────────────────────────
Console.WriteLine("--- Section 2: Payment Failure — Full Compensation ---");

paymentSvc.FailOnNextCall();

var ctx2 = new BookingContext
{
    CustomerId   = "CUST-002",
    CustomerName = "Ben Kowalczyk",
    Destination  = "Whistler, BC",
    TravelDate   = new DateOnly(2027, 2, 20),
    Passengers   = 1,
    NightCount   = 3,
    DayCount     = 3,
};

var executed2     = new List<string>();
var compensated2  = new List<string>();
var orchestrator2 = new SagaOrchestrator<BookingContext>(
    allSteps,
    onExecuted:    name => executed2.Add(name),
    onCompensated: name => compensated2.Add(name));

var result2 = orchestrator2.Execute(ctx2);

foreach (var s in executed2)
    Console.WriteLine($"  ✓ {s}");

Console.WriteLine($"  ✗ {result2.FailedStep} — {result2.Error!.Message}");

if (compensated2.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("  Rolling back completed steps:");
    foreach (var s in compensated2)
        Console.WriteLine($"  ↩ {s}");
}

Pause();

// ── Section 3: Car Rental Unavailable — Partial Compensation ─────────────────
Console.WriteLine("--- Section 3: Car Unavailable — Partial Compensation ---");

carSvc.FailOnNextCall();

var ctx3 = new BookingContext
{
    CustomerId   = "CUST-003",
    CustomerName = "Sophie Bouchard",
    Destination  = "Quebec City, QC",
    TravelDate   = new DateOnly(2027, 7, 1),
    Passengers   = 4,
    NightCount   = 4,
    DayCount     = 4,
};

var executed3     = new List<string>();
var compensated3  = new List<string>();
var orchestrator3 = new SagaOrchestrator<BookingContext>(
    allSteps,
    onExecuted:    name => executed3.Add(name),
    onCompensated: name => compensated3.Add(name));

var result3 = orchestrator3.Execute(ctx3);

foreach (var s in executed3)
    Console.WriteLine($"  ✓ {s}");

Console.WriteLine($"  ✗ {result3.FailedStep} — {result3.Error!.Message}");

if (compensated3.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("  Rolling back completed steps:");
    foreach (var s in compensated3)
        Console.WriteLine($"  ↩ {s}");
}

Pause();

// ── Section 4: Flight Unavailable — No Compensation Needed ───────────────────
Console.WriteLine("--- Section 4: Flight Unavailable — No Compensation Needed ---");

flightSvc.FailOnNextCall();

var ctx4 = new BookingContext
{
    CustomerId   = "CUST-004",
    CustomerName = "Marcus Osei",
    Destination  = "Halifax, NS",
    TravelDate   = new DateOnly(2027, 9, 15),
    Passengers   = 2,
    NightCount   = 5,
    DayCount     = 5,
};

var executed4     = new List<string>();
var compensated4  = new List<string>();
var orchestrator4 = new SagaOrchestrator<BookingContext>(
    allSteps,
    onExecuted:    name => executed4.Add(name),
    onCompensated: name => compensated4.Add(name));

var result4 = orchestrator4.Execute(ctx4);

Console.WriteLine($"  ✗ {result4.FailedStep} — {result4.Error!.Message}");

if (compensated4.Count == 0)
    Console.WriteLine("  No compensation required — no steps had succeeded.");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
