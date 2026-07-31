# 4.03 — CQRS (Command Query Responsibility Segregation)

## Intent

Segregate operations that modify state (commands) from operations that return data (queries), giving each its own model, its own store, and its own optimization path.

---

## The Problem It Solves

Without CQRS, a single model serves both reads and writes. This creates tension:

```csharp
// ONE model, doing everything:
public class AccountService
{
    public void Deposit(string id, decimal amount)
    {
        var account = _db.Load(id);   // load full aggregate just to change balance
        account.Balance += amount;
        _db.Save(account);
    }

    public AccountSummary GetSummary(string id)
    {
        var account = _db.Load(id);   // load full aggregate just to read it
        return new AccountSummary     // re-compute aggregates on every query
        {
            Balance        = account.Balance,
            TotalDeposited = account.Transactions.Where(t => t.Type != "W").Sum(t => t.Amount),
            TotalWithdrawn = account.Transactions.Where(t => t.Type == "W").Sum(t => t.Amount),
        };
    }
}
```

Problems:
- Read and write indexes on the same table conflict (each helps one, hurts the other)
- Every `GetSummary` call recomputes `TotalDeposited` and `TotalWithdrawn` from raw rows
- Write complexity (invariant enforcement) and read complexity (projections) are mixed
- Reads and writes cannot be scaled independently

---

## Solution: Separate Command and Query Models

### Write side — commands
A command represents intent to change state. It is validated, applied to the aggregate, and the aggregate is saved. After every successful command the projector rebuilds the read model.

### Read side — queries
A query reads a pre-projected, denormalised view. It never touches the write store. Aggregates like `TotalDeposited` are computed once at write time and served instantly at read time.

---

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| **Command** | `OpenAccountCommand`, `DepositCommand`, `WithdrawCommand` | Carries intent + data; immutable record |
| **Command handler** | `OpenAccountHandler`, `DepositHandler`, `WithdrawHandler` | Validates, mutates aggregate, updates read model |
| **Query** | `GetBalanceQuery`, `GetAccountSummaryQuery`, `GetTransactionHistoryQuery` | Carries query parameters; immutable record |
| **Query handler** | `GetBalanceHandler`, `GetAccountSummaryHandler`, `GetTransactionHistoryHandler` | Reads from `ReadStore` only |
| **Aggregate** | `BankAccount` | Write-side domain object; enforces invariants |
| **Read model** | `AccountView`, `TransactionView` | Denormalised, query-optimised projections |
| **Projector** | `AccountProjector` | Converts `BankAccount` → `AccountView` after each command |
| **Write store** | `WriteStore` | Holds aggregates; mutated by command handlers only |
| **Read store** | `ReadStore` | Holds views; read by query handlers; updated via projector |

---

## Structure

```
CQRSPattern/
├── Domain/
│   ├── BankAccount.cs          ← write-side aggregate (invariants + state)
│   └── Transaction.cs          ← domain record stored by the aggregate
├── Commands/
│   ├── OpenAccountCommand.cs
│   ├── DepositCommand.cs
│   └── WithdrawCommand.cs
├── Queries/
│   ├── GetBalanceQuery.cs
│   ├── GetAccountSummaryQuery.cs
│   └── GetTransactionHistoryQuery.cs
├── Handlers/
│   ├── ICommandHandler.cs      ← Handle(TCommand) → CommandResult
│   ├── IQueryHandler.cs        ← Handle(TQuery) → TResult?
│   ├── CommandResult.cs        ← success/failure wrapper
│   ├── OpenAccountHandler.cs
│   ├── DepositHandler.cs
│   ├── WithdrawHandler.cs
│   ├── GetBalanceHandler.cs
│   ├── GetAccountSummaryHandler.cs
│   └── GetTransactionHistoryHandler.cs
├── ReadModels/
│   ├── AccountView.cs          ← denormalised, pre-aggregated read model
│   └── TransactionView.cs      ← transaction with pre-computed running balance
└── Infrastructure/
    ├── WriteStore.cs            ← in-memory store for BankAccount aggregates
    ├── ReadStore.cs             ← in-memory store for AccountView projections
    └── AccountProjector.cs     ← converts BankAccount → AccountView
```

---

## Key Code

### Commands are records — immutable intent

```csharp
public sealed record DepositCommand(string AccountId, decimal Amount, string Description);
```

A command is data, not behaviour. It carries parameters and nothing else.

### Command handler — mutate then project

```csharp
public sealed class DepositHandler : ICommandHandler<DepositCommand>
{
    public CommandResult Handle(DepositCommand command)
    {
        var account = _writeStore.Find(command.AccountId);
        if (account is null) return CommandResult.Fail("Account not found.");

        account.Deposit(command.Amount, command.Description);   // mutate aggregate
        _readStore.Save(_projector.Project(account));           // update read model
        return CommandResult.Ok();
    }
}
```

### Query handler — reads only, never writes

```csharp
public sealed class GetBalanceHandler : IQueryHandler<GetBalanceQuery, BalanceResult>
{
    private readonly ReadStore _readStore;   // ← only dependency; WriteStore never injected

    public BalanceResult? Handle(GetBalanceQuery query)
        => _readStore.Find(query.AccountId) is { } view
            ? new BalanceResult(view.AccountId, view.OwnerName, view.Balance, view.LastUpdated)
            : null;
}
```

### Projector — pre-computes all aggregates at write time

```csharp
public sealed class AccountProjector
{
    public AccountView Project(BankAccount account)
    {
        return new AccountView
        {
            Balance          = account.Balance,
            TotalDeposited   = account.Transactions.Where(t => t.Type != "WITHDRAWAL").Sum(t => t.Amount),
            TotalWithdrawn   = account.Transactions.Where(t => t.Type == "WITHDRAWAL").Sum(t => t.Amount),
            TransactionCount = account.Transactions.Count,
            // ... plus per-transaction running balance
        };
    }
}
```

`TotalDeposited` is computed once per command. Every subsequent `GetAccountSummary` query reads the pre-built value — no iteration at query time.

### The separation in one picture

```
WRITE SIDE                        READ SIDE
────────────────────────────────  ────────────────────────────────
DepositCommand                    GetBalanceQuery
    ↓                                 ↓
DepositHandler                    GetBalanceHandler
    ↓                                 ↓
BankAccount (aggregate)           ReadStore → AccountView
    ↓
WriteStore
    ↓
AccountProjector → AccountView
    ↓
ReadStore (updated)
```

Commands touch `WriteStore` and update `ReadStore` via the projector. Queries **only** touch `ReadStore`.

---

## Demo Scenarios

```
PROBLEM  — shows a single model handling both reads and writes
DEMO 1   — commands: open two accounts, make deposits and withdrawals
DEMO 2   — GetBalance: query reads AccountView, never touches WriteStore
DEMO 3   — GetAccountSummary: pre-computed TotalDeposited / TotalWithdrawn
DEMO 4   — GetTransactionHistory: running balance pre-computed by projector
DEMO 5   — command failures: duplicate account, overdraft, unknown account
DEMO 6   — the write/read separation shown as a data-flow diagram
```

---

## When to Use

- Reads and writes have very different load profiles (many more reads than writes, or vice versa)
- Query shapes diverge significantly from write shapes (complex projections, aggregates)
- You need to scale reads and writes independently (read replicas, separate services)
- You are applying Domain-Driven Design — the aggregate and the view model different things

## When NOT to Use

- Simple CRUD — the extra models and projectors add overhead with no payoff
- Read and write load is symmetric and small
- The write model and read model would be identical anyway

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Optimised reads** | Read models are pre-projected — no aggregation at query time |
| **Optimised writes** | Aggregates focus on invariants, not query shapes |
| **Independent scaling** | Read store can be replicated; write store kept small |
| **Single responsibility** | Command handlers validate and mutate; query handlers only read |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Eventual consistency** | In async systems, the read model may lag the write model briefly |
| **More moving parts** | Commands, queries, handlers, projectors, two stores — more files to maintain |
| **Projection complexity** | Every write must keep the read model current; bugs here cause stale reads |

---

## Related Patterns

- **Repository (4.01)** — `WriteStore` and `ReadStore` are simple repositories; CQRS decides which one each handler touches
- **Event Sourcing (4.15)** — CQRS pairs naturally with Event Sourcing: the projector subscribes to domain events rather than being called directly
- **Mediator (3.05)** — MediatR (a popular .NET library) implements CQRS dispatch via the Mediator pattern: `_mediator.Send(command)` routes to the correct handler

---

## Running the Demo

```bash
cd src/4-Enterprise/4.03-CQRS/CQRSPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.03-CQRS/CQRSPattern.Tests
dotnet test
```
