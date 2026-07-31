namespace CQRSPattern;

// The write-side store — holds BankAccount aggregates.
// Mutated exclusively by command handlers.
public sealed class WriteStore
{
    private readonly Dictionary<string, BankAccount> _accounts = new();

    public void         Save(BankAccount account) => _accounts[account.AccountId] = account;
    public BankAccount? Find(string id)           => _accounts.GetValueOrDefault(id);
    public bool         Exists(string id)         => _accounts.ContainsKey(id);
}
