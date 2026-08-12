# 4.06 — Service Layer

## Intent

A Service Layer defines an application's boundary with a layer of services that establishes a set of available operations and coordinates the application's response in each operation. It acts as the single entry point for all use cases, keeping coordination and business rules out of both the presentation layer and the domain objects.

## The Problem It Solves

Without a Service Layer, callers must coordinate domain objects and enforce business rules themselves:

```csharp
// In a controller — too much responsibility here
var member = memberRepo.GetById(memberId);
if (member == null) return NotFound();
if (!member.IsActive) return BadRequest("Inactive member");
if (loanRepo.MemberHasOverdueLoans(memberId)) return BadRequest("Has overdue loans");
if (loanRepo.CountActiveByMemberId(memberId) >= 5) return BadRequest("Loan limit reached");
var book = bookRepo.GetById(bookId);
if (book == null) return NotFound();
if (!book.IsAvailable) return BadRequest("No copies available");
book.CheckOut();
bookRepo.Update(book);
loanRepo.Add(new Loan(book, member, DateTime.UtcNow, DateTime.UtcNow.AddDays(21)));
```

Problems this creates:

- **Duplicated coordination** — every caller (web controller, CLI, background job) must repeat the same checks and steps
- **Scattered business rules** — rules like "max 5 loans" live in many places; changing one means finding all of them
- **Fat controllers / fat UI** — the caller knows more about the domain than it should
- **Hard to test** — you can't test the use case without going through the full HTTP stack
- **No clear application boundary** — nothing defines what operations the application supports

## Solution: Centralize Use Cases Behind a Service Interface

Each service class groups related operations, enforces all business rules for those operations, and coordinates the underlying repositories:

```csharp
// Controller — delegates everything to the service layer
var loan = loanService.BorrowBook(memberId, bookId);
```

`LoanService.BorrowBook` handles member active check, overdue check, loan limit check, book availability check, `CheckOut()`, repository updates, and `Loan` creation — all in one place, called the same way from any entry point.

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| **Service Interface** | `IBookService`, `IMemberService`, `ILoanService` | Define available operations as a contract |
| **Service Implementation** | `BookService`, `MemberService`, `LoanService` | Coordinate repositories, enforce business rules, manage use-case flow |
| **Repository Interface** | `IBookRepository`, `IMemberRepository`, `ILoanRepository` | Data access abstraction the services depend on |
| **Repository Implementation** | `InMemory*Repository` | Concrete storage (in-memory for this demo) |
| **Domain Objects** | `Book`, `Member`, `Loan` | Encapsulate entity state and per-object invariants |

## Structure

```
4.06-ServiceLayer/
├── ServiceLayerPattern/
│   ├── Domain/
│   │   ├── Book.cs              ← entity: title, author, copies, CheckOut()/Return()
│   │   ├── Member.cs            ← entity: name, member number, IsActive, Deactivate()
│   │   └── Loan.cs              ← entity: status derived from DueDate and ReturnedAt
│   ├── Repositories/
│   │   ├── IBookRepository.cs
│   │   ├── IMemberRepository.cs
│   │   ├── ILoanRepository.cs
│   │   ├── InMemoryBookRepository.cs
│   │   ├── InMemoryMemberRepository.cs
│   │   └── InMemoryLoanRepository.cs
│   ├── Services/
│   │   ├── IBookService.cs      ← service interface (the application boundary)
│   │   ├── IMemberService.cs
│   │   ├── ILoanService.cs
│   │   ├── BookService.cs       ← use-case implementation
│   │   ├── MemberService.cs
│   │   └── LoanService.cs       ← coordinates 3 repositories, enforces 4 business rules
│   └── Program.cs
└── ServiceLayerPattern.Tests/
    └── ServiceLayerTests.cs     ← 22 tests
```

## Key Code

### Service Interface — the Application Boundary

```csharp
public interface ILoanService
{
    Loan BorrowBook(int memberId, int bookId);
    void ReturnBook(int loanId);
    IReadOnlyList<Loan> GetMemberLoans(int memberId);
    IReadOnlyList<Loan> GetActiveLoans();
    IReadOnlyList<Loan> GetOverdueLoans();
}
```

The interface lists every operation the application supports for loans. Nothing outside `LoanService` needs to know how any of these work.

### Service Implementation — Coordination and Rules

```csharp
public Loan BorrowBook(int memberId, int bookId)
{
    var member = members.GetById(memberId)
        ?? throw new KeyNotFoundException($"Member {memberId} not found.");

    if (!member.IsActive)
        throw new InvalidOperationException(
            $"Member '{member.Name}' is inactive and cannot borrow books.");

    if (loans.MemberHasOverdueLoans(memberId))
        throw new InvalidOperationException(
            $"Member '{member.Name}' has overdue loans and must return them before borrowing.");

    var activeCount = loans.CountActiveByMemberId(memberId);
    if (activeCount >= MaxActiveLoans)
        throw new InvalidOperationException(
            $"Member '{member.Name}' has reached the maximum of {MaxActiveLoans} active loans.");

    var book = books.GetById(bookId)
        ?? throw new KeyNotFoundException($"Book {bookId} not found.");

    if (!book.IsAvailable)
        throw new InvalidOperationException(
            $"No copies of '{book.Title}' are currently available.");

    book.CheckOut();
    books.Update(book);

    var now = DateTime.UtcNow;
    var loan = new Loan(_nextId++, bookId, memberId, book.Title, member.Name, now, now.AddDays(LoanDays));
    loans.Add(loan);
    return loan;
}
```

All four business rules — active member, no overdue loans, loan limit, book availability — are enforced here. No caller needs to repeat them.

### Domain Object — Per-Entity Invariants

```csharp
public enum LoanStatus { Active, Returned, Overdue }

public LoanStatus Status => ReturnedAt.HasValue
    ? LoanStatus.Returned
    : DateTime.UtcNow > DueDate ? LoanStatus.Overdue : LoanStatus.Active;
```

`Loan.Status` is derived, not stored — it can never get out of sync with `ReturnedAt` and `DueDate`. The service layer enforces cross-entity rules; the domain object enforces its own internal consistency.

### Repository Interface — Data Access Contract

```csharp
public interface ILoanRepository
{
    Loan? GetById(int id);
    IReadOnlyList<Loan> GetByMemberId(int memberId);
    IReadOnlyList<Loan> GetActiveLoans();
    IReadOnlyList<Loan> GetOverdueLoans();
    int CountActiveByMemberId(int memberId);
    bool MemberHasOverdueLoans(int memberId);
    void Add(Loan loan);
    void Update(Loan loan);
}
```

The service depends on the interface, not the implementation — the storage can be swapped from in-memory to SQL without touching `LoanService`.

## Demo Scenarios

```
── The Problem Without a Service Layer ──────────────────────────────────────────
  Shows the raw coordination code a controller would duplicate per-caller.
  Contrast with the single-call equivalent using the service layer.

── Demo 1: Book Catalogue ───────────────────────────────────────────────────────
  Adds 5 Canadian titles via BookService.AddBook.
  Searches by author ("margaret") to show the search operation.

── Demo 2: Member Registration ──────────────────────────────────────────────────
  Registers 3 members, each assigned a unique TPL-XXXX member number.
  Shows all members via GetAllMembers.

── Demo 3: Borrowing Books ──────────────────────────────────────────────────────
  Alice borrows two books, Bob borrows one — shows copies decrement.
  Tests two business rule violations:
    - Borrow when no copies available (last copy already out)
    - Borrow as inactive member

── Demo 4: Returning Books ──────────────────────────────────────────────────────
  Returns a loan, shows copies increment back.
  Tests double-return protection.

── Demo 5: Loan Reports ─────────────────────────────────────────────────────────
  Active loans list, member history, overdue count.
```

## When to Use

- Your application has multiple entry points (web API, CLI, background jobs, tests) that need the same use-case logic
- Business rules span multiple domain objects or repositories and must be enforced consistently
- You want to write use-case tests without going through the HTTP/UI layer
- You are building a layered architecture (Presentation → Service → Domain → Data)

## When NOT to Use

- Simple CRUD with no multi-step coordination — a direct repository call is cleaner
- The application has only one entry point and is unlikely to grow
- The "service" would just delegate to one repository with no added logic — that is unnecessary indirection

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Single source of truth for use cases** | Business rules live in one place; changing a rule means changing one file |
| **Testable in isolation** | Services can be tested with in-memory repositories, no HTTP layer required |
| **Clear application boundary** | The service interface documents exactly what the application can do |
| **Consistent enforcement** | No caller can bypass a business rule by going to the repository directly |
| **Swap-friendly** | Storage, notifications, and other infrastructure can be swapped without touching service logic |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Extra layer** | Adds files and indirection for simple CRUD that doesn't need coordination |
| **Anemic domain risk** | If all logic moves to services, domain objects become plain data bags with no behaviour |
| **Transaction management** | The service layer must own the transaction boundary; missing this leads to partial writes |
| **Interface proliferation** | Every service needs an interface, an implementation, and wiring — overhead for small apps |

## Related Patterns

- **Repository (4.01)** — Service Layer depends on Repository interfaces for data access; the two layers work together
- **Unit of Work (4.02)** — the service method is the natural transaction boundary; a Unit of Work wraps the repositories it coordinates
- **Dependency Injection (4.05)** — services and repositories are typically wired via DI so the composition root controls lifetimes and swapping
- **Facade (2.5)** — Service Layer is a specific application of the Facade idea: a unified interface over a subsystem, applied at the application boundary
- **CQRS (4.03)** — a Service Layer can be split into a command service and a query service when read/write models diverge significantly

## Running the Demo

```bash
cd src/4-Enterprise/4.06-ServiceLayer/ServiceLayerPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.06-ServiceLayer/ServiceLayerPattern.Tests
dotnet test
```
