namespace CQRSPattern;

public sealed record AccountSummaryResult(
    string   AccountId,
    string   OwnerName,
    decimal  Balance,
    int      TransactionCount,
    decimal  TotalDeposited,
    decimal  TotalWithdrawn,
    DateTime LastUpdated);

public sealed class GetAccountSummaryHandler : IQueryHandler<GetAccountSummaryQuery, AccountSummaryResult>
{
    private readonly ReadStore _readStore;

    public GetAccountSummaryHandler(ReadStore readStore) => _readStore = readStore;

    public AccountSummaryResult? Handle(GetAccountSummaryQuery query)
    {
        var view = _readStore.Find(query.AccountId);
        return view is null
            ? null
            : new AccountSummaryResult(
                view.AccountId, view.OwnerName, view.Balance,
                view.TransactionCount, view.TotalDeposited, view.TotalWithdrawn,
                view.LastUpdated);
    }
}
