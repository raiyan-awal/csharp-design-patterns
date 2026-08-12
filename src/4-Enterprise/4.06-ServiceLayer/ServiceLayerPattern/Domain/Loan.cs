namespace ServiceLayerPattern.Domain;

public enum LoanStatus { Active, Returned, Overdue }

public sealed class Loan
{
    public int Id { get; init; }
    public int BookId { get; init; }
    public int MemberId { get; init; }
    public string BookTitle { get; init; } = "";
    public string MemberName { get; init; } = "";
    public DateTime BorrowedAt { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? ReturnedAt { get; private set; }

    public LoanStatus Status => ReturnedAt.HasValue
        ? LoanStatus.Returned
        : DateTime.UtcNow > DueDate ? LoanStatus.Overdue : LoanStatus.Active;

    public bool IsOverdue => Status == LoanStatus.Overdue;

    public Loan(int id, int bookId, int memberId, string bookTitle, string memberName,
                DateTime borrowedAt, DateTime dueDate)
    {
        Id = id;
        BookId = bookId;
        MemberId = memberId;
        BookTitle = bookTitle;
        MemberName = memberName;
        BorrowedAt = borrowedAt;
        DueDate = dueDate;
    }

    public void MarkReturned(DateTime returnedAt) => ReturnedAt = returnedAt;
}
