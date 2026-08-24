# 4.13 — Aggregate Root

## Intent

An Aggregate Root is the sole entry point into a cluster of related domain objects — the Aggregate. All external code holds a reference only to the root; every mutation goes through root methods that enforce consistency rules across the entire cluster. This gives you a single, well-defined place where invariants are checked before any state can change.

## The Problem It Solves

```csharp
// Without Aggregate Root: external code reaches into sub-objects directly
var rider = new PolicyRider(1, "CriticalIllness", 250_000m, 420m);  // constructed anywhere
policy.Riders.Add(rider);  // bypasses all consistency checks

// Now the policy might be:
// - Over the $5,000,000 coverage maximum — nobody checked
// - Carrying a duplicate rider type  — nobody checked
// - Already cancelled                 — nobody checked
```

Problems:

- **No consistency boundary.** Any code in the application can construct a `PolicyRider` and push it into the list. The invariants that live in the domain are ignored.
- **Scattered validation.** Every call site must remember to check coverage limits, duplicate types, and policy status — and every new call site is a new opportunity to forget.
- **No single place to look.** When a bug in a rider causes an over-limit policy, there is no obvious entry point to audit — the mutation could have come from anywhere.
- **Implicit contracts.** The relationship between a policy and its riders is invisible in the type system. Nothing stops a rider from being "shared" between two policies, which makes no domain sense.

## Solution: Route All Mutations Through the Root

```csharp
// All changes go through InsurancePolicy — it checks everything before committing
policy.AddRider("CriticalIllness", 250_000m, 420m);   // duplicate check, limit check, status check
policy.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 60m);  // % cap, duplicate check

// External code can read the riders list but can never mutate it directly
IReadOnlyList<PolicyRider> riders = policy.Riders;
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Aggregate Root Base | `AggregateRoot` | Provides `Id` and `Version`; `IncrementVersion()` stamps each mutation |
| Aggregate Root | `InsurancePolicy` | Entry point for all state changes; enforces invariants across the whole cluster |
| Internal Entity | `PolicyRider` | A rider attached to a policy; `internal` constructor prevents external construction |
| Internal Entity | `Beneficiary` | A named beneficiary with a percentage; `internal` constructor prevents external construction |
| Repository | `IPolicyRepository` | Returns only `InsurancePolicy` — never `PolicyRider` or `Beneficiary` by ID |
| Repository Impl | `InMemoryPolicyRepository` | In-memory store keyed by policy ID |

## Structure

```
4.13-AggregateRoot/
├── AggregateRootPattern/
│   ├── Domain/
│   │   ├── AggregateRoot.cs       ← Id, Version, IncrementVersion()
│   │   ├── PolicyStatus.cs        ← Active / Cancelled / Expired enum
│   │   ├── InsurancePolicy.cs     ← aggregate root; owns riders + beneficiaries
│   │   ├── PolicyRider.cs         ← internal entity; internal constructor
│   │   └── Beneficiary.cs         ← internal entity; internal constructor
│   ├── Repositories/
│   │   ├── IPolicyRepository.cs   ← works only with InsurancePolicy
│   │   └── InMemoryPolicyRepository.cs
│   └── Program.cs                 ← 5-section demo
└── AggregateRootPattern.Tests/
    └── AggregateRootTests.cs      ← 33 tests across 5 suites
```

## Key Code

### AggregateRoot base class — identity and version

```csharp
public abstract class AggregateRoot
{
    public int Id      { get; protected set; }
    public int Version { get; private set; }

    protected void IncrementVersion() => Version++;
}
```

`Id` is the only globally meaningful identity in the aggregate. `Version` increments with every successful mutation and is used by a real persistence layer for optimistic concurrency: before saving, compare the stored version against the expected version; if they differ, another caller modified the aggregate concurrently and the save is rejected.

### InsurancePolicy — enforcing invariants at every entry point

```csharp
public void AddRider(string type, decimal additionalCoverage, decimal annualPremium)
{
    EnsureActive();
    if (_riders.Any(r => r.Type == type))
        throw new InvalidOperationException(
            $"Rider '{type}' is already attached to this policy.");
    if (TotalCoverage + additionalCoverage > MaxTotalCoverage)
        throw new InvalidOperationException(
            $"Adding this rider would exceed the maximum total coverage of ${MaxTotalCoverage:N0} CAD.");

    _riders.Add(new PolicyRider(_nextRiderId++, type, additionalCoverage, annualPremium));
    IncrementVersion();
}
```

Every mutation — `AddRider`, `RemoveRider`, `AddBeneficiary`, `RemoveBeneficiary`, `Cancel` — begins with `EnsureActive()` and then checks the invariants specific to that operation. The domain rules live in one class, not scattered across callers.

### Internal entity construction — enforcing the consistency boundary in the type system

```csharp
public sealed class PolicyRider
{
    public int     RiderId            { get; }
    public string  Type               { get; }
    public decimal AdditionalCoverage { get; }
    public decimal AnnualPremium      { get; }

    internal PolicyRider(int riderId, string type, decimal additionalCoverage, decimal annualPremium)
    { ... }
}
```

The `internal` constructor means `PolicyRider` can only be constructed by code in the same assembly — and within that assembly, only `InsurancePolicy.AddRider` ever calls it. Code outside the assembly cannot `new PolicyRider(...)` directly. The consistency boundary is enforced by the type system, not by convention.

### Local IDs — identity is scoped to the aggregate

```csharp
// InsurancePolicy assigns local sequential IDs to its sub-entities
private int _nextRiderId       = 1;
private int _nextBeneficiaryId = 1;

// Inside AddRider:
_riders.Add(new PolicyRider(_nextRiderId++, type, additionalCoverage, annualPremium));
```

`RiderId = 2` means "the second rider on this policy", not a globally unique ID. There is no `PolicyRiderRepository` with a `FindById(int riderId)` — external code only reaches riders through their owning `InsurancePolicy`. This scoping is the defining characteristic of an internal entity.

### Repository returns only the root

```csharp
public interface IPolicyRepository
{
    InsurancePolicy?               FindById(int id);
    InsurancePolicy?               FindByPolicyNumber(string policyNumber);
    IReadOnlyList<InsurancePolicy> FindActiveByHolder(string holderName);
    void                           Save(InsurancePolicy policy);
}
```

Every method that returns domain objects returns `InsurancePolicy`. There is no `FindRiderById`, no `FindBeneficiaryByName`. If you want a rider, you get the policy and read `policy.Riders`. This enforces the consistency boundary at the infrastructure level too.

## Demo Scenarios

```
=== Northern Shield Life Insurance — Aggregate Root Demo ===

--- Creating Policies ---
[POLICY] NSL-2026-001 — Jean-François Tremblay | Base: $500,000 CAD | Premium: $1,200.00/yr | Status: Active | v0
[POLICY] NSL-2026-002 — Amara Okonkwo          | Base: $1,000,000 CAD | ...                 | Status: Active | v0

--- Adding Riders ---
  NSL-2026-001 | Total coverage: $1,250,000 CAD | Total premium: $1,800.00/yr | v2
    [1] CriticalIllness     +$250,000    $420.00/yr
    [2] AccidentalDeath     +$500,000    $180.00/yr

--- Invariant Violations ---
  Duplicate rider 'CriticalIllness': [ERROR] Rider 'CriticalIllness' is already attached to this policy.
  Rider would push total past $5,000,000 CAD limit: [ERROR] Adding this rider would exceed...
  Remove non-existent rider 'WaiverOfPremium': [ERROR] Rider 'WaiverOfPremium' is not attached...

--- Adding Beneficiaries ---
  NSL-2026-001 beneficiaries (total: 100%):
    [1] Marie-Claire Tremblay  (Spouse    ) — 60%
    [2] Luc Tremblay           (Child     ) — 30%
    [3] Sophie Tremblay        (Child     ) — 10%
  Beneficiary allocation already at 100%: [ERROR] Adding 5% would exceed 100% total allocation...

--- Cancelling a Policy ---
[POLICY] NSL-2026-002 | Status: Cancelled | v3
[REASON] Client requested cancellation — relocated outside Canada.
  Add rider to cancelled policy: [ERROR] Policy NSL-2026-002 is Cancelled and cannot be modified.
  Cancel already-cancelled policy: [ERROR] Policy is already cancelled.
```

## When to Use

- A group of domain objects must change together atomically, and you need a single place to enforce the rules that govern the whole group.
- You have internal entities that make no sense outside their owning aggregate (a `PolicyRider` without an `InsurancePolicy`, an `OrderLine` without an `Order`).
- You want to prevent scattered, per-call-site validation by making the root the only way to mutate the cluster.
- You are implementing event sourcing and need a clear boundary for which events belong together.

## When NOT to Use

- The "aggregate" contains only a single entity with no meaningful sub-objects — the pattern adds ceremony with no benefit.
- Sub-entities genuinely need to be queried and retrieved in isolation at scale (e.g., a reporting query across all `PolicyRider` rows in a table). In that case, a read model or separate query path is the right tool, not widening the aggregate.
- The cluster has no cross-entity invariants — if items are truly independent, model them as independent aggregates.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Single consistency boundary | All invariants for the cluster live in one class; adding a rule means editing one method. |
| Type-system enforcement | `internal` constructors prevent external code from bypassing the root entirely. |
| Testable invariants | Each invariant is a method on the root — unit-testable with zero infrastructure. |
| Optimistic concurrency | `Version` gives persistence layers a cheap, correct way to detect concurrent modifications. |
| Clear persistence unit | The repository saves and loads the aggregate as a whole — one save per logical operation. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Large aggregates can grow stiff | A root with many sub-entities and many rules becomes a large, hard-to-navigate class; keep aggregates small. |
| No direct sub-entity queries | You cannot `SELECT * FROM riders` and map to domain objects without going through the root first; read models or raw queries are the workaround. |
| Loading overhead | Fetching the full aggregate (including all riders and beneficiaries) just to add one field can be expensive; lazy loading or partial hydration is required at scale. |

## Related Patterns

- **Domain Event (4.12)** — the aggregate root is the natural place to raise domain events; `InsurancePolicy` would call `Raise(new RiderAddedEvent(...))` after each successful `AddRider` call.
- **Repository (4.01)** — the repository always uses the aggregate root as its unit of storage and retrieval; there is never a `PolicyRiderRepository`.
- **Unit of Work (4.02)** — when multiple aggregates change in one request, a Unit of Work commits all their changes atomically; each aggregate is still the owner of its own consistency.
- **Value Object (4.11)** — sub-entities that have no identity of their own and are equal by value (e.g., a `Money` amount on a rider) should be value objects, not internal entities.
- **Entity (4.14)** — entities that do have global identity and exist independently of any aggregate are the contrast to internal entities; `InsurancePolicy` is both an entity (it has a globally unique ID) and an aggregate root.

## Running the Demo

```bash
cd src/4-Enterprise/4.13-AggregateRoot/AggregateRootPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.13-AggregateRoot/AggregateRootPattern.Tests && dotnet test
```
