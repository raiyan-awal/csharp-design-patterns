using ServiceLayerPattern.Domain;
using ServiceLayerPattern.Repositories;

namespace ServiceLayerPattern.Services;

public sealed class LoanService(
    ILoanRepository loans,
    IBookRepository books,
    IMemberRepository members) : ILoanService
{
    private const int MaxActiveLoans = 5;
    private const int LoanDays = 21;
    private int _nextId = 1;

    public Loan BorrowBook(int memberId, int bookId)
    {
        var member = members.GetById(memberId)
            ?? throw new KeyNotFoundException($"Member {memberId} not found.");

        if (!member.IsActive)
            throw new InvalidOperationException(
                $"Member '{member.Name}' is inactive and cannot borrow books.");

        if (loans.MemberHasOverdueLoans(memberId))
            throw new InvalidOperationException(
                $"Member '{member.Name}' has overdue loans and must return them before borrowing.");

        var activeCount = loans.CountActiveByMemberId(memberId);
        if (activeCount >= MaxActiveLoans)
            throw new InvalidOperationException(
                $"Member '{member.Name}' has reached the maximum of {MaxActiveLoans} active loans.");

        var book = books.GetById(bookId)
            ?? throw new KeyNotFoundException($"Book {bookId} not found.");

        if (!book.IsAvailable)
            throw new InvalidOperationException(
                $"No copies of '{book.Title}' are currently available.");

        book.CheckOut();
        books.Update(book);

        var now = DateTime.UtcNow;
        var loan = new Loan(_nextId++, bookId, memberId, book.Title, member.Name, now, now.AddDays(LoanDays));
        loans.Add(loan);
        return loan;
    }

    public void ReturnBook(int loanId)
    {
        var loan = loans.GetById(loanId)
            ?? throw new KeyNotFoundException($"Loan {loanId} not found.");

        if (loan.ReturnedAt.HasValue)
            throw new InvalidOperationException($"Loan {loanId} has already been returned.");

        var book = books.GetById(loan.BookId)
            ?? throw new KeyNotFoundException($"Book {loan.BookId} not found.");

        book.Return();
        books.Update(book);

        loan.MarkReturned(DateTime.UtcNow);
        loans.Update(loan);
    }

    public IReadOnlyList<Loan> GetMemberLoans(int memberId) => loans.GetByMemberId(memberId);

    public IReadOnlyList<Loan> GetActiveLoans() => loans.GetActiveLoans();

    public IReadOnlyList<Loan> GetOverdueLoans() => loans.GetOverdueLoans();
}
