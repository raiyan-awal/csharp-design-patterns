namespace CQRSPattern;

// Converts a BankAccount (write model) into an AccountView (read model).
// Called by every command handler after a successful mutation so the read
// store is always consistent with the write store.
//
// In an event-sourced system this would be an event handler subscribing to
// domain events. Here it is called synchronously for simplicity.
public sealed class AccountProjector
{
    public AccountView Project(BankAccount account)
    {
        var running = 0m;
        var views   = new List<TransactionView>(account.Transactions.Count);

        foreach (var tx in account.Transactions)
        {
            running += tx.Type == "WITHDRAWAL" ? -tx.Amount : tx.Amount;
            views.Add(new TransactionView(tx.Type, tx.Amount, tx.Description, tx.Timestamp, running));
        }

        return new AccountView
        {
            AccountId        = account.AccountId,
            OwnerName        = account.OwnerName,
            Balance          = account.Balance,
            TransactionCount = account.Transactions.Count,
            TotalDeposited   = account.Transactions.Where(t => t.Type != "WITHDRAWAL").Sum(t => t.Amount),
            TotalWithdrawn   = account.Transactions.Where(t => t.Type == "WITHDRAWAL").Sum(t => t.Amount),
            LastUpdated      = account.Transactions[^1].Timestamp,
            Transactions     = views,
        };
    }
}
