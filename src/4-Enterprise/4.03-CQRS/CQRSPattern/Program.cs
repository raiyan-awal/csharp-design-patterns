using CQRSPattern;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

static void Header(string title)
{
    Console.WriteLine(new string('─', 62));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('─', 62));
}

static void PrintResult(CommandResult result)
{
    if (!result.IsSuccess)
        Console.WriteLine($"  [ERROR] {result.Error}");
}

// ── Composition root ──────────────────────────────────────────────────────────
var writeStore  = new WriteStore();
var readStore   = new ReadStore();
var projector   = new AccountProjector();

var openHandler     = new OpenAccountHandler (writeStore, readStore, projector);
var depositHandler  = new DepositHandler     (writeStore, readStore, projector);
var withdrawHandler = new WithdrawHandler    (writeStore, readStore, projector);

var balanceHandler     = new GetBalanceHandler           (readStore);
var historyHandler     = new GetTransactionHistoryHandler(readStore);
var summaryHandler     = new GetAccountSummaryHandler    (readStore);

Console.WriteLine("=== CQRS Pattern — Banking Demo ===\n");

// ─── THE PROBLEM ──────────────────────────────────────────────────────────────
Header("THE PROBLEM — one model doing everything");
Console.WriteLine("""

  Without CQRS, a single Account class handles reads AND writes:

    public class AccountService
    {
        // Write: must load the full aggregate, validate, mutate, save
        public void Deposit(string id, decimal amount)
        {
            var account = _db.Load(id);
            account.Balance += amount;
            _db.Save(account);
        }

        // Read: also loads the full aggregate just to report the balance
        public decimal GetBalance(string id)
            => _db.Load(id).Balance;

        // Read: loads the full aggregate to compute derived data
        public AccountSummary GetSummary(string id)
        {
            var account = _db.Load(id);
            return new AccountSummary
            {
                Balance        = account.Balance,
                TotalDeposited = account.Transactions.Where(t => t.Type != "W").Sum(t => t.Amount),
                TotalWithdrawn = account.Transactions.Where(t => t.Type == "W").Sum(t => t.Amount),
            };
        }
    }

  Problems:
  - Read and write optimisations conflict (indexes help one, hurt the other)
  - Every query recalculates aggregates from raw transactions — wasteful
  - Complex write logic (validation, invariants) is mixed with query logic
  - Cannot scale reads and writes independently

  CQRS separates this into two models:
  - Commands mutate the write model (BankAccount aggregate)
  - Queries read a pre-projected read model (AccountView)

""");
Pause();

// ─── DEMO 1: Commands ─────────────────────────────────────────────────────────
Header("DEMO 1 — Commands: open accounts and record transactions");
Console.WriteLine();

PrintResult(openHandler.Handle(new OpenAccountCommand("ACC-001", "Rania Choudhury", 5_000m)));
PrintResult(openHandler.Handle(new OpenAccountCommand("ACC-002", "Marcus Osei",     2_500m)));

Console.WriteLine();
PrintResult(depositHandler.Handle(new DepositCommand ("ACC-001", 1_200m, "Payroll deposit — TD Bank")));
PrintResult(withdrawHandler.Handle(new WithdrawCommand("ACC-002",   300m, "E-transfer to Rania")));
PrintResult(depositHandler.Handle(new DepositCommand ("ACC-001",   300m, "E-transfer from Marcus")));
PrintResult(withdrawHandler.Handle(new WithdrawCommand("ACC-001",   850m, "Rent — 204 Queen St W")));
PrintResult(withdrawHandler.Handle(new WithdrawCommand("ACC-001",   120m, "Groceries — Loblaws")));
PrintResult(depositHandler.Handle(new DepositCommand ("ACC-002",   750m, "Freelance invoice #42")));
PrintResult(withdrawHandler.Handle(new WithdrawCommand("ACC-002",   300m, "Monthly TTC Metropass")));
Pause();

// ─── DEMO 2: Query — GetBalance ───────────────────────────────────────────────
Header("DEMO 2 — Query: GetBalance (reads from pre-projected AccountView)");
Console.WriteLine();

Console.WriteLine("  Commands wrote to WriteStore. Queries read from ReadStore — separate stores.\n");

foreach (var id in new[] { "ACC-001", "ACC-002" })
{
    var result = balanceHandler.Handle(new GetBalanceQuery(id));
    if (result is not null)
        Console.WriteLine($"  [QRY]   GetBalance   → {result.OwnerName,-20} ${result.Balance:F2}");
}

Console.WriteLine();
// Show that queries never touch the write store
Console.WriteLine("  The write store (BankAccount aggregates) was not accessed for these queries.");
Console.WriteLine("  Zero read contention on the write side.\n");
Pause();

// ─── DEMO 3: Query — GetAccountSummary ───────────────────────────────────────
Header("DEMO 3 — Query: GetAccountSummary (pre-computed aggregates)");
Console.WriteLine();

var summary = summaryHandler.Handle(new GetAccountSummaryQuery("ACC-001"));
if (summary is not null)
{
    Console.WriteLine($"  Account:          {summary.OwnerName} ({summary.AccountId})");
    Console.WriteLine($"  Balance:          ${summary.Balance:F2}");
    Console.WriteLine($"  Transactions:     {summary.TransactionCount}");
    Console.WriteLine($"  Total deposited:  ${summary.TotalDeposited:F2}");
    Console.WriteLine($"  Total withdrawn:  ${summary.TotalWithdrawn:F2}");
    Console.WriteLine($"  Last updated:     {summary.LastUpdated:HH:mm:ss}");
}

Console.WriteLine();
Console.WriteLine("  TotalDeposited and TotalWithdrawn are pre-computed by AccountProjector");
Console.WriteLine("  at write time — no Sum() over transactions at query time.");
Pause();

// ─── DEMO 4: Query — GetTransactionHistory ───────────────────────────────────
Header("DEMO 4 — Query: GetTransactionHistory (with running balance)");
Console.WriteLine();

var history = historyHandler.Handle(new GetTransactionHistoryQuery("ACC-001", MaxCount: 10));
if (history is not null)
{
    Console.WriteLine($"  {"Type",-12} {"Amount",10}  {"Balance After",14}  Description");
    Console.WriteLine($"  {new string('-', 60)}");
    foreach (var tx in history)
        Console.WriteLine($"  {tx.Type,-12} {tx.Amount,10:F2}  {tx.BalanceAfter,14:F2}  {tx.Description}");
}

Console.WriteLine();
Console.WriteLine("  BalanceAfter is pre-computed by AccountProjector — not re-derived per query.");
Pause();

// ─── DEMO 5: Command failure ──────────────────────────────────────────────────
Header("DEMO 5 — Commands enforce invariants; queries never need to");
Console.WriteLine();

Console.WriteLine("  Attempting duplicate account:");
var dup = openHandler.Handle(new OpenAccountCommand("ACC-001", "Duplicate", 0m));
Console.WriteLine($"  Result: {(dup.IsSuccess ? "OK" : $"FAILED — {dup.Error}")}");

Console.WriteLine("\n  Attempting overdraft:");
var overdraft = withdrawHandler.Handle(new WithdrawCommand("ACC-001", 99_999m, "Too much"));
Console.WriteLine($"  Result: {(overdraft.IsSuccess ? "OK" : $"FAILED — {overdraft.Error}")}");

Console.WriteLine("\n  Attempting deposit on unknown account:");
var missing = depositHandler.Handle(new DepositCommand("ACC-999", 100m, "Nobody home"));
Console.WriteLine($"  Result: {(missing.IsSuccess ? "OK" : $"FAILED — {missing.Error}")}");

Console.WriteLine("""

  Invariant enforcement lives exclusively in command handlers.
  Query handlers never need to validate — they only read pre-built views.

""");
Pause();

// ─── DEMO 6: The separation ───────────────────────────────────────────────────
Header("DEMO 6 — The separation in one picture");
Console.WriteLine("""

  WRITE SIDE                          READ SIDE
  ──────────────────────────────────  ──────────────────────────────────
  Command (OpenAccountCommand)        Query (GetBalanceQuery)
      ↓                                   ↓
  CommandHandler (OpenAccountHandler) QueryHandler (GetBalanceHandler)
      ↓                                   ↓
  BankAccount aggregate               ReadStore → AccountView
      ↓
  WriteStore
      ↓
  AccountProjector → AccountView
      ↓
  ReadStore (updated)

  Commands touch WriteStore + update ReadStore via the projector.
  Queries ONLY touch ReadStore — they never see WriteStore.
  The two sides can grow, scale, and be optimised independently.

""");
Pause();

Console.WriteLine("  Done.");
