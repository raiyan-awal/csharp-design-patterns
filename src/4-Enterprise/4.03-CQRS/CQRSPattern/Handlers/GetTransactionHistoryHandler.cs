namespace CQRSPattern;

public sealed class GetTransactionHistoryHandler
    : IQueryHandler<GetTransactionHistoryQuery, IEnumerable<TransactionView>>
{
    private readonly ReadStore _readStore;

    public GetTransactionHistoryHandler(ReadStore readStore) => _readStore = readStore;

    public IEnumerable<TransactionView>? Handle(GetTransactionHistoryQuery query)
    {
        var view = _readStore.Find(query.AccountId);
        return view is null
            ? null
            : view.Transactions.TakeLast(query.MaxCount);
    }
}
