# 4.19 — Saga Pattern

## Intent

The Saga pattern manages long-running, distributed transactions by decomposing them into a sequence of local transactions, each paired with a **compensating transaction** that reverses its effect. When any step fails, completed steps are undone in reverse order, leaving the system in a consistent state — without requiring distributed locks or a two-phase commit protocol.

## The Problem It Solves

Consider a travel booking that must reserve a flight, a hotel, and a car rental, then charge a credit card:

```csharp
// Without Saga
flightRef  = flightApi.Reserve(destination, date);
hotelRef   = hotelApi.Book(destination, date);
carRef     = carApi.Reserve(destination, date);
paymentRef = paymentApi.Charge(customerId, totalCost);  // ← fails halfway through
// Flight, hotel, and car are now reserved with no way to roll them back
```

Problems:
- No atomic guarantee across services — partial success leaves the system in an inconsistent state.
- Each service owns its own database; a distributed lock or two-phase commit is impractical or unavailable.
- Failures can happen at any step, leaving orphaned reservations that cost money and block inventory.

## Solution: Saga Orchestrator with Compensating Transactions

Each step implements a forward `Execute` and a backward `Compensate`. The orchestrator runs steps in sequence; if any step throws, it runs each previously-completed step's `Compensate` in reverse order before returning a failure result.

```csharp
var orchestrator = new SagaOrchestrator<BookingContext>(
    [flightStep, hotelStep, carStep, paymentStep],
    onExecuted:    name => Console.WriteLine($"  ✓ {name}"),
    onCompensated: name => Console.WriteLine($"  ↩ {name}"));

var result = orchestrator.Execute(context);

if (!result.IsSuccess)
    Console.WriteLine($"  Failed at: {result.FailedStep} — {result.Error!.Message}");
```

When payment fails after flight, hotel, and car have succeeded:

```
  ✓ Flight Reservation
  ✓ Hotel Booking
  ✓ Car Rental
  ✗ Payment — Card declined for customer CUST-002 — amount $2,497.00 CAD.

  Rolling back:
  ↩ Car Rental
  ↩ Hotel Booking
  ↩ Flight Reservation
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Step interface | `ISagaStep<TContext>` | Contract: `Name`, `Execute`, `Compensate` |
| Orchestrator | `SagaOrchestrator<TContext>` | Runs steps in order; compensates in reverse on failure |
| Result | `SagaResult` | Carries `IsSuccess`, `FailedStep`, `Error` |
| Context | `BookingContext` | Shared state flowing through all steps (refs, totals) |
| Steps | `FlightReservationStep`, `HotelBookingStep`, `CarRentalStep`, `PaymentStep` | Each encapsulates one local transaction and its compensation |
| Services | `SimulatedFlightService`, `SimulatedHotelService`, `SimulatedCarRentalService`, `SimulatedPaymentService` | Simulated external systems with configurable failure |

## Structure

```
src/4-Enterprise/4.19-SagaPattern/
├── SagaPattern/
│   ├── Core/
│   │   ├── ISagaStep.cs             ← step interface
│   │   ├── SagaResult.cs            ← success/failure value object
│   │   └── SagaOrchestrator.cs      ← runs steps, compensates on failure
│   ├── Domain/
│   │   └── BookingContext.cs        ← shared context (refs, totals)
│   ├── Steps/
│   │   ├── FlightReservationStep.cs ← reserve / cancel flight
│   │   ├── HotelBookingStep.cs      ← book / cancel hotel
│   │   ├── CarRentalStep.cs         ← reserve / cancel car
│   │   └── PaymentStep.cs           ← charge / refund payment
│   ├── Services/
│   │   ├── TravelExceptions.cs      ← domain-specific exceptions
│   │   └── SimulatedTravelServices.cs
│   └── Program.cs
└── SagaPattern.Tests/
    └── SagaOrchestratorTests.cs     ← 18 tests across 4 suites
```

## Key Code

### ISagaStep — the two-sided contract

```csharp
public interface ISagaStep<TContext>
{
    string Name { get; }
    void Execute(TContext context);
    void Compensate(TContext context);
}
```

Every step must implement both directions. The orchestrator calls `Execute` on the forward pass and `Compensate` on the rollback pass. Compensation receives the same context as execution, which gives it access to whatever refs or IDs `Execute` stored there (e.g., `context.FlightRef`).

### SagaOrchestrator — run and rollback

```csharp
public SagaResult Execute(TContext context)
{
    var executed = new Stack<ISagaStep<TContext>>();

    foreach (var step in _steps)
    {
        try
        {
            step.Execute(context);
            executed.Push(step);
            _onExecuted?.Invoke(step.Name);
        }
        catch (Exception ex)
        {
            foreach (var done in executed)
            {
                try   { done.Compensate(context); _onCompensated?.Invoke(done.Name); }
                catch { /* compensation is best-effort */ }
            }
            return SagaResult.Failure(step.Name, ex);
        }
    }

    return SagaResult.Success();
}
```

A `Stack<T>` naturally gives reverse-insertion order on iteration — the last-pushed step is compensated first. Compensation errors are swallowed so a single failing compensation does not block the remaining rollbacks.

### BookingContext — shared state

```csharp
public sealed class BookingContext
{
    public string   CustomerId   { get; init; }
    public string   Destination  { get; init; }
    public DateOnly TravelDate   { get; init; }
    public int      Passengers   { get; init; }
    public int      NightCount   { get; init; }
    public int      DayCount     { get; init; }

    // Written by steps during Execute; read by steps during Compensate
    public string?  FlightRef    { get; set; }
    public string?  HotelRef     { get; set; }
    public string?  CarRef       { get; set; }
    public string?  PaymentRef   { get; set; }
    public decimal  TotalCostCAD { get; set; }
}
```

The context is the only coupling between steps. Each step writes its own ref during `Execute` and reads it back during `Compensate` to know what to cancel.

### A step pair — flight reservation and cancellation

```csharp
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
```

`Compensate` guards on `context.FlightRef is not null` because if `Execute` threw before setting `FlightRef`, there is nothing to cancel.

## Demo Scenarios

```
=== Maple Travel — Saga Pattern Demo ===

--- Section 1: Successful Vacation Package ---
  ✓ Flight Reservation
  ✓ Hotel Booking
  ✓ Car Rental
  ✓ Payment
  Booking confirmed! Alice Tremblay → Banff, AB  $2,838.00 CAD

--- Section 2: Payment Failure — Full Compensation ---
  ✓ Flight Reservation
  ✓ Hotel Booking
  ✓ Car Rental
  ✗ Payment — Card declined for customer CUST-002 — amount $...
  Rolling back: ↩ Car Rental  ↩ Hotel Booking  ↩ Flight Reservation

--- Section 3: Car Unavailable — Partial Compensation ---
  ✓ Flight Reservation
  ✓ Hotel Booking
  ✗ Car Rental — No vehicles available in Quebec City, QC on Jul 1, 2027.
  Rolling back: ↩ Hotel Booking  ↩ Flight Reservation

--- Section 4: Flight Unavailable — No Compensation Needed ---
  ✗ Flight Reservation — No available seats to Halifax, NS on Sep 15, 2027.
  No compensation required — no steps had succeeded.
```

## When to Use

- A business operation spans multiple services or databases that cannot share a single ACID transaction.
- You need eventual consistency with explicit rollback semantics rather than locking.
- Each individual step can be made idempotent so compensation is safe to retry.
- You want to replace ad-hoc "undo" logic with a structured, testable pattern.

## When NOT to Use

- All data lives in a single database — use a regular database transaction instead.
- Compensating transactions are impossible or too expensive (e.g., you cannot unsend an email — use the Outbox pattern to defer sending until after all steps succeed).
- Steps must be truly atomic with no partial failure window — consider two-phase commit or a distributed transaction coordinator.
- The operation is short-lived and simple — the orchestrator adds overhead not justified for a single-service call.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| No distributed locks | Steps use local transactions only; no cross-service coordination required at runtime. |
| Explicit rollback | Compensating transactions are first-class code, tested and version-controlled alongside the forward path. |
| Failure isolation | A failure in one service triggers targeted compensation rather than leaving the system silently inconsistent. |
| Observable | `onExecuted` and `onCompensated` callbacks surface real-time step progress to logs, metrics, or the UI. |
| Testable | `ISagaStep<TContext>` is an interface; stubs with a shared log verify compensation order with no external dependencies. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Compensation complexity | Every step requires a compensating implementation that must stay in sync with the forward logic as the code evolves. |
| Eventual consistency | Between a step's `Execute` and its `Compensate`, the system is briefly inconsistent — other processes may observe the partial state. |
| Compensation may fail | Network errors or service downtime can prevent compensation from completing; production sagas need retry logic (see Retry Pattern 4.17) and a dead-letter mechanism. |
| Idempotency required | Compensations may be retried; each must produce the same result on repeated invocations or risk double-cancellations. |

## Related Patterns

- **Retry Pattern (4.17)** — wrap each step's `Execute` and `Compensate` calls in a `RetryPolicy` to handle transient failures before treating a step as permanently failed.
- **Circuit Breaker (4.16)** — protect calls to flaky downstream services inside each step, preventing the saga from hammering a degraded service.
- **Outbox Pattern (4.20)** — used alongside Saga to guarantee that messages or events emitted by a step are delivered exactly once, even if the process crashes between writing to the database and publishing to a message broker.
- **Domain Event (4.12)** — in a choreography-based Saga variant, each service listens for domain events from other services and reacts without a central orchestrator.

## Running the Demo

```bash
cd src/4-Enterprise/4.19-SagaPattern/SagaPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.19-SagaPattern/SagaPattern.Tests
dotnet test
```
