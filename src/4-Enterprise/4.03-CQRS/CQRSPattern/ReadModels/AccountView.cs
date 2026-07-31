namespace CQRSPattern;

// Read-side projection — denormalised and query-optimised.
// Pre-computes aggregates (TotalDeposited, TotalWithdrawn, TransactionCount) so queries
// never need to iterate transactions. Rebuilt from the write model by AccountProjector.
public sealed class AccountView
{
    public string   AccountId        { get; set; } = "";
    public string   OwnerName        { get; set; } = "";
    public decimal  Balance          { get; set; }
    public int      TransactionCount { get; set; }
    public decimal  TotalDeposited   { get; set; }
    public decimal  TotalWithdrawn   { get; set; }
    public DateTime LastUpdated      { get; set; }
    public List<TransactionView> Transactions { get; set; } = [];
}
