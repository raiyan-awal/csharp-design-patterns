# 3.13 — Pipeline Pattern

## Intent

Pass data through a sequence of independent processing steps, where each step transforms or validates the data and decides whether to continue to the next step or stop early.

---

## The Problem It Solves

Without a pipeline, all processing logic lives in a single method that keeps growing:

```csharp
// WITHOUT Pipeline — one method does everything:
public LoanResult Process(LoanApplication app)
{
    if (string.IsNullOrEmpty(app.Name)) return Reject("Name required");
    if (app.Sin.Length != 9)            return Reject("Invalid SIN");
    if (app.Amount < 1_000)             return Reject("Amount too low");
    if (app.CreditScore < 600)          return Reject("Low credit score");

    var dti = CalculateDti(app);
    if (dti > 0.44m) return Reject($"DTI {dti:P1} exceeds 44%");

    if (IsWatchlisted(app)) return Reject("AML flag");

    var risk = ClassifyRisk(app, dti);
    return risk == "High" ? ApproveReduced(...) : ApproveFullAmount(...);
}
```

Every new requirement (fraud score, income verification, insurance check) means editing this method. Steps cannot be reordered or assembled differently per product. The method grows without bound. Pipeline addresses this by making each step its own class and assembling them at the call site.

---

## Solution: Loan Application Processing

A loan application flows through six independent steps. Each step either enriches the context and continues, or rejects and stops the pipeline. Composition happens at the call site — not inside any step.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **Step interface** | `IPipelineStep<TContext>` | `Name` + `Execute(ctx) : bool` |
| **Pipeline runner** | `Pipeline<TContext>` | Runs steps in order, short-circuits on `false` |
| **Context** | `LoanContext` | Carries the application data and accumulated results |
| **Steps** | `ApplicationValidationStep` | Required fields, valid amount range |
| | `CreditScoreStep` | Minimum credit score (300–900 Equifax/TransUnion scale) |
| | `DebtToIncomeStep` | TDS ratio ≤ 44% (OSFI guideline) |
| | `AmlScreeningStep` | FINTRAC watchlist check |
| | `RiskClassificationStep` | Low / Medium / High classification (enriches context, never rejects) |
| | `LoanDecisionStep` | Full approval or conditional approval at reduced amount |

### What each step checks

| Step | Passes when | Rejects when |
|------|-------------|-------------|
| **Validation** | Name present, 9-digit SIN, amount $1K–$1.5M, income > 0 | Any required field missing or out of range |
| **Credit Score** | Score ≥ 600 | Score < 600 |
| **TDS Ratio** | (existing debt + loan payment) / income ≤ 44% | > 44% |
| **AML** | Not on watchlist | On FINTRAC watchlist |
| **Risk Classification** | Always continues | Never — only enriches `RiskLevel` |
| **Loan Decision** | Always approves (full or conditional) | Never — decision step at end |

---

## Structure

```
PipelinePattern/
├── IPipelineStep.cs           ← interface: Name + Execute(ctx) : bool
├── Pipeline.cs                ← generic runner: AddStep (fluent) + Run
├── Models/
│   ├── LoanApplication.cs     ← immutable input (init properties)
│   └── LoanContext.cs         ← mutable context: enriched data + outcome
└── Steps/
    ├── ApplicationValidationStep.cs
    ├── CreditScoreStep.cs
    ├── DebtToIncomeStep.cs
    ├── AmlScreeningStep.cs
    ├── RiskClassificationStep.cs
    └── LoanDecisionStep.cs
```

---

## Key Code

### Step interface — one method per step class

```csharp
public interface IPipelineStep<TContext>
{
    string Name    { get; }
    bool   Execute(TContext context);   // true = continue, false = stop
}
```

### Pipeline runner — fluent builder + short-circuit execution

```csharp
public sealed class Pipeline<TContext>
{
    private readonly List<IPipelineStep<TContext>> _steps = [];

    public Pipeline<TContext> AddStep(IPipelineStep<TContext> step)
    {
        _steps.Add(step);
        return this;   // fluent
    }

    public TContext Run(TContext context)
    {
        foreach (var step in _steps)
        {
            var shouldContinue = step.Execute(context);
            if (!shouldContinue) break;   // short-circuit
        }
        return context;
    }
}
```

### Concrete step — self-contained logic, single responsibility

```csharp
public sealed class CreditScoreStep : IPipelineStep<LoanContext>
{
    private const int MinimumScore = 600;

    public string Name => "Credit Score Check";

    public bool Execute(LoanContext ctx)
    {
        if (ctx.Application.CreditScore < MinimumScore)
        {
            ctx.Reject($"Credit score {ctx.Application.CreditScore} below minimum of {MinimumScore}");
            return false;   // stops the pipeline
        }
        ctx.Log.Add($"      ✓ Credit score {ctx.Application.CreditScore} — passes");
        return true;        // continues to next step
    }
}
```

### Usage — composed at the call site

```csharp
// Standard full pipeline
var fullPipeline = new Pipeline<LoanContext>()
    .AddStep(new ApplicationValidationStep())
    .AddStep(new CreditScoreStep())
    .AddStep(new DebtToIncomeStep())
    .AddStep(new AmlScreeningStep())
    .AddStep(new RiskClassificationStep())
    .AddStep(new LoanDecisionStep());

var ctx = fullPipeline.Run(new LoanContext(application));
Console.WriteLine(ctx.IsApproved ? $"Approved: {ctx.OutcomeNote}" : $"Rejected: {ctx.OutcomeNote}");

// Pre-qualification pipeline — subset of steps, no AML, no final decision
var preQualPipeline = new Pipeline<LoanContext>()
    .AddStep(new ApplicationValidationStep())
    .AddStep(new CreditScoreStep())
    .AddStep(new DebtToIncomeStep());
```

---

## Demo Scenarios

```
PROBLEM  — shows the procedural approach and why it doesn't scale
DEMO 1   — Sophie Tremblay: low-risk, all steps pass, full approval
DEMO 2   — Marcus Reid: rejected at Step 1 — invalid SIN
DEMO 3   — Devon Park: rejected at Step 2 — credit score 540 < 600
DEMO 4   — Aisha Kowalski: rejected at Step 3 — TDS ratio exceeds 44%
DEMO 5   — Raj Mehta: rejected at Step 4 — matched FINTRAC watchlist
DEMO 6   — Lena Osei: high-risk profile, conditionally approved at 75% of requested amount
DEMO 7   — Custom 3-step pre-qualification pipeline assembled from the same step classes
```

---

## When to Use

- Processing has several distinct stages that each add to or validate a shared data object
- You need to run different subsets of stages for different products or entry points
- Stages must be individually testable, replaceable, or reorderable without touching other stages

---

## When NOT to Use

- You have one or two stages — the infrastructure outweighs the benefit
- Stages have complex dependencies on each other's internal state (pipelines work best when each stage is self-contained)
- You need branching logic between stages (consider Chain of Responsibility or a workflow engine instead)

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Single Responsibility** | Each step class does one thing and is independently testable |
| **Open/Closed** | Add a new step (fraud score, insurance check) without touching any existing step |
| **Composable** | Assemble different pipelines from the same step classes per product |
| **Short-circuit** | Early rejection stops execution immediately — later steps do no unnecessary work |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Linear only** | Standard pipeline has no branching — all paths are sequential |
| **Shared context coupling** | Steps communicate via a shared context object, which grows as steps are added |
| **Debugging complexity** | Tracing which step produced a result requires logging or structured context inspection |

---

## Pipeline vs Chain of Responsibility

Both pass a request through a series of handlers, but their intent differs.

| | Pipeline | Chain of Responsibility |
|---|---------|------------------------|
| **Data flows through** | All steps (unless short-circuited) | Stops at the first handler that claims it |
| **Context** | Shared, accumulated by each step | Handler decides whether to pass on or handle |
| **Purpose** | Transform / validate progressively | Find the right handler for a request |
| **Steps know each other?** | No — pipeline wires them | Sometimes — each handler holds a `next` reference |

---

## Related Patterns

- **Chain of Responsibility (3.01)** — similar structure; Pipeline focuses on transformation, CoR on finding the right handler
- **Decorator (2.4)** — wraps objects at build time; Pipeline assembles steps at runtime for each execution
- **Template Method (3.10)** — fixed algorithm skeleton in a base class; Pipeline is a runtime-configurable sequence
- **Strategy (3.09)** — swaps one algorithm; Pipeline chains many algorithms in sequence

---

## Running the Demo

```bash
cd src/3-Behavioral/3.13-Pipeline/PipelinePattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.13-Pipeline/PipelinePattern.Tests
dotnet test
```
