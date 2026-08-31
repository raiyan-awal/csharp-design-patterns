# 4.23 — DTO (Data Transfer Object)

## Intent

A Data Transfer Object is a plain data carrier used to move information across a layer boundary — between a service and its caller, between an API endpoint and a client, or between a domain layer and a persistence layer. The DTO has no business logic. Its only purpose is to define exactly what data crosses the boundary: no more, no less.

## The Problem It Solves

Without DTOs, domain objects leak across layer boundaries:

```csharp
// Without DTO — domain object returned directly from service
public Candidate GetCandidate(Guid id) => _repository.Find(id);

// Caller receives the entire domain object, including sensitive internal fields:
candidate.InternalScore        // 88 — should never leave the system
candidate.BackgroundCheckNotes // "Passed — minor credit flag" — confidential
candidate.RecordInternalAssessment(...)  // caller can mutate domain state
```

Problems:
- Sensitive fields leak to callers who have no business seeing them.
- The caller can call domain methods directly, bypassing business rules.
- The domain object's shape is now a public API contract — internal refactoring breaks external callers.
- A list endpoint and a detail endpoint return the same heavyweight object even when the list only needs three fields.

## Solution: Shape the Data for Each Boundary

```csharp
// Full detail response — no sensitive fields
var dto = new CandidateDto(
    Id: candidate.Id, Name: candidate.Name, Email: candidate.Email,
    Skills: candidate.Skills, YearsOfExperience: candidate.YearsOfExperience,
    SalaryExpectationCAD: candidate.SalaryExpectationCAD);
// InternalScore and BackgroundCheckNotes are simply not here

// Lightweight summary for list views
var summary = new CandidateSummaryDto(
    Id: candidate.Id, Name: candidate.Name,
    TopSkill: candidate.Skills.FirstOrDefault() ?? "(none)",
    YearsOfExperience: candidate.YearsOfExperience);
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Domain entity | `Candidate` | Holds all data including sensitive fields; owns business behaviour |
| Domain entity | `JobPosting` | Holds salary ranges, required skills, and the internal budget (never exposed) |
| Request DTO | `CreateCandidateRequest` | Defines exactly what the caller may provide to create a candidate |
| Request DTO | `CreateJobPostingRequest` | Defines input for creating a job posting, including the internal budget that only internal systems send |
| Response DTO | `CandidateDto` | Full profile without `InternalScore` or `BackgroundCheckNotes` |
| Response DTO | `CandidateSummaryDto` | Lightweight shape for list views: Id, Name, TopSkill, YearsOfExperience |
| Response DTO | `JobPostingDto` | Full posting without `InternalBudgetCAD` |
| Mapper | `CandidateMapper` | Translates between `Candidate`, `CreateCandidateRequest`, `CandidateDto`, `CandidateSummaryDto` |
| Mapper | `JobPostingMapper` | Translates between `JobPosting`, `CreateJobPostingRequest`, `JobPostingDto` |
| Service | `CandidateService` | Accepts request DTOs, returns response DTOs — domain objects never leave this layer |
| Service | `JobPostingService` | Same boundary enforcement for postings |

## Structure

```
src/4-Enterprise/4.23-DTO/
├── DTOPattern/
│   ├── Domain/
│   │   ├── Candidate.cs              ← rich domain object with InternalScore (sensitive)
│   │   └── JobPosting.cs             ← domain object with InternalBudgetCAD (sensitive)
│   ├── DTOs/
│   │   ├── CreateCandidateRequest.cs ← inbound request DTO
│   │   ├── CandidateDto.cs           ← full response DTO (no sensitive fields)
│   │   ├── CandidateSummaryDto.cs    ← lightweight list DTO
│   │   ├── CreateJobPostingRequest.cs
│   │   └── JobPostingDto.cs          ← response DTO (no InternalBudgetCAD)
│   ├── Mapping/
│   │   ├── CandidateMapper.cs        ← ToDto, ToSummaryDto, ToDomain
│   │   └── JobPostingMapper.cs       ← ToDto, ToDomain
│   ├── Services/
│   │   ├── CandidateService.cs       ← domain objects never cross this boundary
│   │   └── JobPostingService.cs
│   └── Program.cs
└── DTOPattern.Tests/
    └── DTOPatternTests.cs            ← 32 tests across 4 suites
```

## Key Code

### Domain objects hold sensitive data — DTOs deliberately omit it

```csharp
public sealed class Candidate
{
    public Guid   Id                   { get; private set; }
    public string Name                 { get; private set; } = "";
    public string Email                { get; private set; } = "";
    public IReadOnlyList<string> Skills { get; private set; } = [];
    public int    YearsOfExperience    { get; private set; }
    public decimal SalaryExpectationCAD { get; private set; }

    // Internal — never exposed in any API response
    public int    InternalScore        { get; private set; }
    public string BackgroundCheckNotes { get; private set; } = "";
}
```

```csharp
// CandidateDto — InternalScore and BackgroundCheckNotes simply do not exist here
public sealed record CandidateDto(
    Guid   Id,
    string Name,
    string Email,
    IReadOnlyList<string> Skills,
    int    YearsOfExperience,
    decimal SalaryExpectationCAD);
```

The mapper translates one to the other. Nothing forces the domain class to change shape when the API contract changes, and nothing leaks the sensitive fields.

### Different DTOs for different contexts

```csharp
// Full detail — used for a profile view or a candidate detail endpoint
public sealed record CandidateDto(
    Guid   Id, string Name, string Email,
    IReadOnlyList<string> Skills, int YearsOfExperience, decimal SalaryExpectationCAD);

// Lightweight summary — used for list endpoints where only three fields are needed
public sealed record CandidateSummaryDto(
    Guid Id, string Name, string TopSkill, int YearsOfExperience);
```

Both map from the same `Candidate` domain object. The mapper decides which fields to include for each shape. The list endpoint never pays the cost of transmitting fields it does not need.

### Mapper — the translation layer

```csharp
public static class CandidateMapper
{
    public static CandidateDto ToDto(Candidate candidate) =>
        new(candidate.Id, candidate.Name, candidate.Email,
            candidate.Skills, candidate.YearsOfExperience, candidate.SalaryExpectationCAD);

    public static CandidateSummaryDto ToSummaryDto(Candidate candidate) =>
        new(candidate.Id, candidate.Name,
            candidate.Skills.FirstOrDefault() ?? "(no skills listed)",
            candidate.YearsOfExperience);

    public static Candidate ToDomain(CreateCandidateRequest request) =>
        Candidate.Create(request.Name, request.Email, request.Skills,
            request.YearsOfExperience, request.SalaryExpectationCAD);
}
```

The mapper is the only place that knows both the domain shape and the DTO shape. Change the domain and only the mapper needs updating; the DTO and its callers are untouched.

### Service — domain objects never cross the boundary

```csharp
public sealed class CandidateService
{
    public CandidateDto Register(CreateCandidateRequest request)
    {
        var candidate = CandidateMapper.ToDomain(request);  // inbound: DTO → domain
        _candidates.Add(candidate);
        return CandidateMapper.ToDto(candidate);            // outbound: domain → DTO
    }

    public IReadOnlyList<CandidateSummaryDto> ListSummaries() =>
        _candidates.Select(CandidateMapper.ToSummaryDto).ToList();
}
```

The service layer is the enforcer: `Candidate` objects go in via `ToDomain`, come back via `ToDto` or `ToSummaryDto`. No caller ever holds a `Candidate` reference.

## Demo Scenarios

```
=== Maple Talent — DTO Pattern Demo ===

--- Section 1: Register Candidates (Request DTO → Domain → Response DTO) ---
  Registered: Sophie Tremblay  |  sophie.tremblay@gmail.com  |  $115,000 CAD
  Skills    : C#, .NET, Azure

  Note: InternalScore and BackgroundCheckNotes are NOT present on CandidateDto.

--- Section 2: List View — CandidateSummaryDto (lighter shape) ---
  Sophie Tremblay       Top skill: C#                     6 yr(s)
  Marcus Osei           Top skill: TypeScript             4 yr(s)
  Alice Chen            Top skill: Python                 8 yr(s)

--- Section 3: Create Job Postings (InternalBudgetCAD hidden in DTO) ---
  [Maple Systems Inc.] Senior .NET Developer
    Location : Toronto, ON
    Salary   : $105,000–$130,000 CAD
    Note: InternalBudgetCAD is NOT present on JobPostingDto.

--- Section 4: Find by ID — Demonstrating the Mapping Round-Trip ---
  Found     : Alice Chen
  Email     : alice.chen@maple.ca
  Skills    : Python, Machine Learning, PyTorch
  Random ID lookup: not found (returns null, not exception)
```

## When to Use

- A service or API endpoint must not expose every field of the underlying domain or persistence object — especially sensitive, internal, or unrelated data.
- Different consumers need different shapes of the same data (a list view needs 3 fields; a detail view needs 12).
- You want the domain model to evolve independently of the API contract — internal refactoring should not break external callers.
- Data must cross a process boundary (HTTP, message queue, gRPC) where a domain object with methods and invariants cannot be serialized meaningfully.

## When NOT to Use

- Very simple CRUD applications where the domain object and the API shape are genuinely identical and no sensitive fields exist — the mapper is pure boilerplate with no benefit.
- Internal utility code within the same layer where no boundary is being crossed.
- When an auto-mapper library (AutoMapper, Mapperly) would generate the mapping automatically — in those cases the DTO types still exist, but the mapper class is generated rather than handwritten.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Information hiding | Sensitive or internal domain fields are structurally absent from the DTO — they cannot leak even if a developer tries to include them. |
| Stable API contracts | The DTO is the public contract. Domain refactoring only touches the mapper, not the callers. |
| Right size per context | A lightweight summary DTO for lists, a full DTO for detail views — each shape carries exactly what its consumer needs. |
| Decoupled serialization | DTOs are plain data records with no behaviour, making them easy to serialize to JSON, XML, or Protobuf without leaking domain methods. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Mapping boilerplate | Every property that crosses the boundary must be spelled out in a mapper. Large domain objects with many fields produce large, repetitive mappers. |
| Synchronization burden | When the domain adds a field, you must update the DTO and the mapper. It is easy to forget one and ship a response that silently omits a new field. |
| Potential over-DTOing | It is tempting to create a new DTO for every minor variation. Without discipline, a codebase can accumulate dozens of near-identical DTO types. |

## Related Patterns

- **Data Mapper (4.07)** — the Data Mapper pattern handles the translation between domain objects and database rows. DTOs extend this idea to API layer boundaries.
- **CQRS (4.03)** — command-side accepts request DTOs; query-side returns read-model DTOs. The two patterns are natural companions.
- **Repository (4.01)** — repositories work with domain objects internally; they commonly return DTOs to the service layer above them.
- **Result Pattern (4.21)** — services that return `Result<CandidateDto>` combine explicit failure handling with the DTO boundary enforcement in one return type.

## Running the Demo

```bash
cd src/4-Enterprise/4.23-DTO/DTOPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.23-DTO/DTOPattern.Tests
dotnet test
```
