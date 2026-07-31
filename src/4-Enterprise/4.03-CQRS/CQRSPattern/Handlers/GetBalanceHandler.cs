namespace CQRSPattern;

public sealed record BalanceResult(
    string   AccountId,
    string   OwnerName,
    decimal  Balance,
    DateTime AsOf);

public sealed class GetBalanceHandler : IQueryHandler<GetBalanceQuery, BalanceResult>
{
    private readonly ReadStore _readStore;

    public GetBalanceHandler(ReadStore readStore) => _readStore = readStore;

    public BalanceResult? Handle(GetBalanceQuery query)
    {
        var view = _readStore.Find(query.AccountId);
        return view is null
            ? null
            : new BalanceResult(view.AccountId, view.OwnerName, view.Balance, view.LastUpdated);
    }
}
