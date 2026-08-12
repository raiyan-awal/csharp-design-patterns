using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Repositories;

public interface ILoanRepository
{
    Loan? GetById(int id);
    IReadOnlyList<Loan> GetByMemberId(int memberId);
    IReadOnlyList<Loan> GetActiveLoans();
    IReadOnlyList<Loan> GetOverdueLoans();
    int CountActiveByMemberId(int memberId);
    bool MemberHasOverdueLoans(int memberId);
    void Add(Loan loan);
    void Update(Loan loan);
}
