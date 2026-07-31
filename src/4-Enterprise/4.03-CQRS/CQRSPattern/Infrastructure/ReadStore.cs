namespace CQRSPattern;

// The read-side store — holds pre-projected AccountViews.
// Read exclusively by query handlers; updated by command handlers via AccountProjector.
public sealed class ReadStore
{
    private readonly Dictionary<string, AccountView> _views = new();

    public void         Save(AccountView view) => _views[view.AccountId] = view;
    public AccountView? Find(string id)        => _views.GetValueOrDefault(id);
    public IEnumerable<AccountView> All        => _views.Values;
}
