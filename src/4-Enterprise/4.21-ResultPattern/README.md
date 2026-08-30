# 4.21 — Result Pattern

## Intent

The Result Pattern replaces exceptions for expected business failures with an explicit return type that carries either a success value or an error message. Instead of letting failures escape as thrown exceptions — which are invisible in method signatures and require try-catch at the call site — every operation returns a `Result<T>` that the caller must inspect. Failures become data, not control flow.

## The Problem It Solves

Consider a loan evaluation that throws on every business rule violation:

```csharp
// Without Result Pattern
public LoanApproval Evaluate(LoanApplication app)
{
    if (app.AnnualIncomeCAD < 35_000)
        throw new LoanDeclinedException("Income too low.");          // invisible in signature
    if (app.CreditScore < 650)
        throw new LoanDeclinedException("Credit score too low.");    // caller must remember to catch
    // ...
    return CalculateApproval(app);
}

// Caller — what happens if they forget the catch?
var approval = loanService.Evaluate(app);  // can blow up silently
```

Problems:
- The method signature (`LoanApproval Evaluate(...)`) lies — it looks like it always returns an approval.
- Expected failures (low income, bad credit) use the same mechanism as unexpected failures (null reference, network error), making them hard to distinguish.
- Every caller must remember to wrap in try-catch; a forgotten catch lets the exception bubble to the wrong layer.
- Chaining multiple fallible steps produces deeply nested try-catch blocks.

## Solution: Return Result<T>

```csharp
public Result<LoanApproval> Evaluate(LoanApplication app) =>
    ValidateIncome(app)
        .Bind(ValidateCreditScore)
        .Bind(ValidateDebtRatio)
        .Bind(CalculateApproval);
```

The method signature now truthfully says: "this returns either a `LoanApproval` or an error." The caller handles both branches explicitly:

```csharp
service.Evaluate(app).Match(
    onSuccess: approval => Console.WriteLine($"Approved: ${approval.MonthlyPaymentCAD:N2}/mo"),
    onFailure: error    => Console.WriteLine($"Declined: {error}"));
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Result | `Result<T>` | Wraps either a success value `T` or a failure error string |
| Domain input | `LoanApplication` | Immutable record holding all applicant data |
| Domain output | `LoanApproval` | Immutable record holding the approved loan terms |
| Service | `LoanApplicationService` | Railway-oriented pipeline: Bind chains validate → validate → calculate |

## Structure

```
src/4-Enterprise/4.21-ResultPattern/
├── ResultPattern/
│   ├── Core/
│   │   └── Result.cs               ← Result<T> with Map, Bind, Match, OnSuccess, OnFailure
│   ├── Domain/
│   │   ├── LoanApplication.cs      ← applicant data record
│   │   └── LoanApproval.cs         ← approved loan terms record
│   ├── Services/
│   │   └── LoanApplicationService.cs ← Bind pipeline with 3 validations + calculation
│   └── Program.cs
└── ResultPattern.Tests/
    └── ResultPatternTests.cs       ← 24 tests across 6 suites
```

## Key Code

### Result<T> — the core type

```csharp
public sealed class Result<T>
{
    public bool   IsSuccess { get; }
    public T?     Value     { get; }
    public string Error     { get; }

    public static Result<T> Success(T value)      => new(value);
    public static Result<T> Failure(string error)  => new(error);

    public Result<TNext> Map<TNext>(Func<T, TNext> map)
        => IsSuccess ? Result<TNext>.Success(map(Value!)) : Result<TNext>.Failure(Error);

    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> bind)
        => IsSuccess ? bind(Value!) : Result<TNext>.Failure(Error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error);
}
```

`Bind` is the key operation for chaining: if the current result is a success it calls `bind` with the value; if it is a failure it propagates the error without calling `bind`. This is what enables the railway-oriented pipeline — once a step fails, all subsequent `Bind` calls are skipped automatically.

`Map` is `Bind` without the possibility of failure: it transforms the success value to a different type, always succeeding (or propagating the existing failure).

`Match` is the exit point: it forces the caller to handle both branches and produce a single value, making it impossible to accidentally ignore the failure case.

### Railway-oriented pipeline

```csharp
public Result<LoanApproval> Evaluate(LoanApplication app) =>
    ValidateIncome(app)       // Result<LoanApplication>
        .Bind(ValidateCreditScore)   // Result<LoanApplication> (or short-circuits)
        .Bind(ValidateDebtRatio)     // Result<LoanApplication> (or short-circuits)
        .Bind(CalculateApproval);    // Result<LoanApproval>    (or short-circuits)
```

Each `ValidateXxx` has the signature `Result<LoanApplication> ValidateXxx(LoanApplication app)`. On success it returns the unchanged application (passing it down the chain). On failure it returns a `Failure` with an explanatory message. `CalculateApproval` transforms the validated application into the final `LoanApproval`. If any step fails, all subsequent steps are bypassed — no exception thrown, no try-catch needed.

### Map — transform without leaving the Result

```csharp
service.Evaluate(app)
    .Map(approval => $"Approval {approval.ApplicationRef}: " +
                     $"${approval.ApprovedAmountCAD:N0} over {approval.TermMonths}mo")
    .OnSuccess(summary => Console.WriteLine(summary))
    .OnFailure(error   => Console.WriteLine($"Declined: {error}"));
```

`Map` lets you transform the success value into a different type — here from `LoanApproval` to `string` — while staying inside the Result railway. `OnSuccess` and `OnFailure` fire side-effect callbacks (logging, metrics) and return the same result so they can be chained.

## Demo Scenarios

```
=== Maple Bank — Result Pattern Demo ===

--- Section 1: Approved Application ---
  ✓ Approved — Alice Tremblay
    Ref        : A3F8C21B
    Amount     : $25,000.00 CAD
    Rate       : 5.49%
    Term       : 60 months
    Monthly    : $479.12 CAD

--- Section 2: Declined — Income Below Minimum ---
  ✗ Declined — Ben Kowalczyk
    Reason: Annual income $28,500 CAD is below the $35,000 CAD minimum.

--- Section 3: Declined — Credit Score Below Minimum ---
  ✗ Declined — Sophie Bouchard
    Reason: Credit score 603 is below the minimum score of 650 required for approval.

--- Section 4: Declined — Debt-to-Income Ratio Exceeded ---
  ✗ Declined — Marcus Osei
    Reason: Debt-to-income ratio of 48% exceeds the maximum 43% allowed.

--- Section 5: Map, OnSuccess, and OnFailure ---
  Summary  : Approval A3F8C21B: $25,000 over 60mo at 5.49%
  Logged   : Credit score 603 is below the minimum score of 650 ...
```

## When to Use

- A method has multiple expected failure modes that are part of normal business logic (validation, not-found, authorization), not bugs.
- You want the method signature to communicate the possibility of failure without callers having to read documentation or source code.
- You are chaining multiple fallible operations and want to short-circuit cleanly without nested try-catch.
- You want to keep exception handling for truly unexpected failures (null reference, I/O errors) and use Result for predictable ones.

## When NOT to Use

- Truly exceptional failures (out of memory, I/O corruption, programming errors) — these should still throw exceptions, not be wrapped in Result.
- Very simple methods where a null return or a bool is sufficient and the caller always checks it.
- Codebases heavily using existing exception-based APIs where wrapping every call in Result adds more noise than clarity.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Honest signatures | `Result<LoanApproval>` tells callers failure is possible; `LoanApproval` does not. |
| No silent failures | The caller must extract the value — there is no way to ignore the error branch with `Match`. |
| Chainable pipeline | `Bind` composes fallible steps without nesting; failure short-circuits automatically. |
| Testable | Each validation step is a pure function returning `Result<T>` — no exceptions to catch in tests. |
| Separation of concerns | Business rule failures are explicit return values; infrastructure failures are still exceptions. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Single error | This implementation carries one error string. Production variants often use `IReadOnlyList<string>` for collecting all validation errors at once rather than stopping at the first. |
| Unfamiliar pattern | Developers accustomed to try-catch need time to adopt the railway style; `Bind` and `Map` can look abstract at first. |
| Wrapping overhead | Every call to an existing exception-based API must be wrapped in a try-catch that converts to `Result`, adding boilerplate at integration boundaries. |

## Related Patterns

- **Specification Pattern (4.04)** — encapsulates individual business rules as objects; the Result Pattern can carry the output of a Specification check rather than throwing when a rule fails.
- **Saga Pattern (4.19)** — each saga step can return `Result<T>` instead of throwing, making the orchestrator's decision to compensate explicit rather than exception-driven.
- **Pipeline Pattern (3.13)** — similar "railway" shape: data flows through a sequence of steps, each transforming and forwarding. Result Pattern adds explicit failure short-circuiting to that idea.

## Running the Demo

```bash
cd src/4-Enterprise/4.21-ResultPattern/ResultPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.21-ResultPattern/ResultPattern.Tests
dotnet test
```
