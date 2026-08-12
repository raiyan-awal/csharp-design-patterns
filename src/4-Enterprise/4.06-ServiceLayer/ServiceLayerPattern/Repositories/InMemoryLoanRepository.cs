using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Repositories;

public sealed class InMemoryLoanRepository : ILoanRepository
{
    private readonly List<Loan> _loans = [];

    public Loan? GetById(int id) => _loans.FirstOrDefault(l => l.Id == id);

    public IReadOnlyList<Loan> GetByMemberId(int memberId) =>
        _loans.Where(l => l.MemberId == memberId).ToList().AsReadOnly();

    public IReadOnlyList<Loan> GetActiveLoans() =>
        _loans.Where(l => l.Status != LoanStatus.Returned).ToList().AsReadOnly();

    public IReadOnlyList<Loan> GetOverdueLoans() =>
        _loans.Where(l => l.IsOverdue).ToList().AsReadOnly();

    public int CountActiveByMemberId(int memberId) =>
        _loans.Count(l => l.MemberId == memberId && l.Status != LoanStatus.Returned);

    public bool MemberHasOverdueLoans(int memberId) =>
        _loans.Any(l => l.MemberId == memberId && l.IsOverdue);

    public void Add(Loan loan) => _loans.Add(loan);

    public void Update(Loan loan)
    {
        var index = _loans.FindIndex(l => l.Id == loan.Id);
        if (index >= 0) _loans[index] = loan;
    }
}
