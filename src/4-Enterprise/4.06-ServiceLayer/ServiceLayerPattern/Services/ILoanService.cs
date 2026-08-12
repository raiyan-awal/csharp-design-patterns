using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Services;

public interface ILoanService
{
    Loan BorrowBook(int memberId, int bookId);
    void ReturnBook(int loanId);
    IReadOnlyList<Loan> GetMemberLoans(int memberId);
    IReadOnlyList<Loan> GetActiveLoans();
    IReadOnlyList<Loan> GetOverdueLoans();
}
