using ServiceLayerPattern.Repositories;
using ServiceLayerPattern.Services;
using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Tests;

public sealed class ServiceLayerTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static (IBookService books, IMemberService members, ILoanService loans) Build()
    {
        var bookRepo   = new InMemoryBookRepository();
        var memberRepo = new InMemoryMemberRepository();
        var loanRepo   = new InMemoryLoanRepository();
        return (
            new BookService(bookRepo),
            new MemberService(memberRepo),
            new LoanService(loanRepo, bookRepo, memberRepo)
        );
    }

    // ── BookService ───────────────────────────────────────────────────────────

    [Fact]
    public void AddBook_ReturnsBookWithCorrectFields()
    {
        var (books, _, _) = Build();
        var book = books.AddBook("The Handmaid's Tale", "Margaret Atwood", "978-0-7710-0813-2", "Fiction", 3);

        Assert.Equal("The Handmaid's Tale", book.Title);
        Assert.Equal("Margaret Atwood", book.Author);
        Assert.Equal(3, book.TotalCopies);
        Assert.Equal(3, book.AvailableCopies);
        Assert.True(book.IsAvailable);
    }

    [Fact]
    public void AddBook_AssignsIncrementingIds()
    {
        var (books, _, _) = Build();
        var b1 = books.AddBook("Book A", "Author A", "", "Fiction", 1);
        var b2 = books.AddBook("Book B", "Author B", "", "Fiction", 1);
        Assert.NotEqual(b1.Id, b2.Id);
    }

    [Fact]
    public void AddBook_ThrowsOnBlankTitle()
    {
        var (books, _, _) = Build();
        Assert.Throws<ArgumentException>(() => books.AddBook("", "Author", "", "Fiction", 1));
    }

    [Fact]
    public void AddBook_ThrowsOnZeroCopies()
    {
        var (books, _, _) = Build();
        Assert.Throws<ArgumentException>(() => books.AddBook("Title", "Author", "", "Fiction", 0));
    }

    [Fact]
    public void GetBook_ThrowsWhenNotFound()
    {
        var (books, _, _) = Build();
        Assert.Throws<KeyNotFoundException>(() => books.GetBook(99));
    }

    [Fact]
    public void SearchBooks_MatchesByTitle()
    {
        var (books, _, _) = Build();
        books.AddBook("Anne of Green Gables", "L.M. Montgomery", "", "Fiction", 2);
        books.AddBook("The Stone Angel", "Margaret Laurence", "", "Fiction", 1);

        var results = books.SearchBooks("anne");
        Assert.Single(results);
        Assert.Equal("Anne of Green Gables", results[0].Title);
    }

    [Fact]
    public void SearchBooks_MatchesByAuthor()
    {
        var (books, _, _) = Build();
        books.AddBook("The Handmaid's Tale", "Margaret Atwood", "", "Fiction", 2);
        books.AddBook("Anne of Green Gables", "L.M. Montgomery", "", "Fiction", 1);

        var results = books.SearchBooks("atwood");
        Assert.Single(results);
        Assert.Equal("The Handmaid's Tale", results[0].Title);
    }

    [Fact]
    public void SearchBooks_EmptyQueryReturnsAll()
    {
        var (books, _, _) = Build();
        books.AddBook("Book A", "Author A", "", "Fiction", 1);
        books.AddBook("Book B", "Author B", "", "Thriller", 1);

        Assert.Equal(2, books.SearchBooks("").Count);
    }

    // ── MemberService ─────────────────────────────────────────────────────────

    [Fact]
    public void RegisterMember_ReturnsActiveMembers()
    {
        var (_, members, _) = Build();
        var m = members.RegisterMember("Alice Tremblay", "alice@example.ca");

        Assert.Equal("Alice Tremblay", m.Name);
        Assert.True(m.IsActive);
        Assert.StartsWith("TPL-", m.MemberNumber);
    }

    [Fact]
    public void RegisterMember_AssignsUniqueMemberNumbers()
    {
        var (_, members, _) = Build();
        var m1 = members.RegisterMember("Alice", "a@a.ca");
        var m2 = members.RegisterMember("Bob", "b@b.ca");
        Assert.NotEqual(m1.MemberNumber, m2.MemberNumber);
    }

    [Fact]
    public void RegisterMember_ThrowsOnBlankName()
    {
        var (_, members, _) = Build();
        Assert.Throws<ArgumentException>(() => members.RegisterMember("", "a@a.ca"));
    }

    [Fact]
    public void DeactivateMember_SetsIsActiveFalse()
    {
        var (_, members, _) = Build();
        var m = members.RegisterMember("Alice", "a@a.ca");
        members.DeactivateMember(m.Id);
        Assert.False(members.GetMember(m.Id).IsActive);
    }

    [Fact]
    public void ReactivateMember_SetsIsActiveTrue()
    {
        var (_, members, _) = Build();
        var m = members.RegisterMember("Alice", "a@a.ca");
        members.DeactivateMember(m.Id);
        members.ReactivateMember(m.Id);
        Assert.True(members.GetMember(m.Id).IsActive);
    }

    // ── LoanService — happy path ──────────────────────────────────────────────

    [Fact]
    public void BorrowBook_CreatesLoanAndDecrementsAvailability()
    {
        var (books, members, loans) = Build();
        var book   = books.AddBook("The Handmaid's Tale", "Margaret Atwood", "", "Fiction", 2);
        var member = members.RegisterMember("Alice", "a@a.ca");

        var loan = loans.BorrowBook(member.Id, book.Id);

        Assert.Equal(book.Id, loan.BookId);
        Assert.Equal(member.Id, loan.MemberId);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(1, books.GetBook(book.Id).AvailableCopies);
    }

    [Fact]
    public void BorrowBook_DueDateIs21DaysFromNow()
    {
        var (books, members, loans) = Build();
        var book   = books.AddBook("Book", "Author", "", "Fiction", 1);
        var member = members.RegisterMember("Alice", "a@a.ca");

        var loan = loans.BorrowBook(member.Id, book.Id);
        var expectedDue = DateTime.UtcNow.AddDays(21);

        Assert.Equal(expectedDue.Date, loan.DueDate.Date);
    }

    [Fact]
    public void ReturnBook_IncrementsAvailabilityAndMarksReturned()
    {
        var (books, members, loans) = Build();
        var book   = books.AddBook("Book", "Author", "", "Fiction", 1);
        var member = members.RegisterMember("Alice", "a@a.ca");

        var loan = loans.BorrowBook(member.Id, book.Id);
        loans.ReturnBook(loan.Id);

        Assert.Equal(1, books.GetBook(book.Id).AvailableCopies);
        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.NotNull(loan.ReturnedAt);
    }

    // ── LoanService — business rules ──────────────────────────────────────────

    [Fact]
    public void BorrowBook_ThrowsWhenBookNotAvailable()
    {
        var (books, members, loans) = Build();
        var book   = books.AddBook("Book", "Author", "", "Fiction", 1);
        var alice  = members.RegisterMember("Alice", "a@a.ca");
        var bob    = members.RegisterMember("Bob",   "b@b.ca");

        loans.BorrowBook(alice.Id, book.Id);

        var ex = Assert.Throws<InvalidOperationException>(() => loans.BorrowBook(bob.Id, book.Id));
        Assert.Contains("available", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BorrowBook_ThrowsWhenMemberInactive()
    {
        var (books, members, loans) = Build();
        var book   = books.AddBook("Book", "Author", "", "Fiction", 1);
        var member = members.RegisterMember("Carol", "c@c.ca");
        members.DeactivateMember(member.Id);

        var ex = Assert.Throws<InvalidOperationException>(() => loans.BorrowBook(member.Id, book.Id));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BorrowBook_ThrowsWhenLoanLimitReached()
    {
        var (books, members, loans) = Build();
        var member = members.RegisterMember("Alice", "a@a.ca");
        for (var i = 0; i < 5; i++)
        {
            var b = books.AddBook($"Book {i}", "Author", "", "Fiction", 1);
            loans.BorrowBook(member.Id, b.Id);
        }
        var extra = books.AddBook("Book Extra", "Author", "", "Fiction", 1);

        var ex = Assert.Throws<InvalidOperationException>(() => loans.BorrowBook(member.Id, extra.Id));
        Assert.Contains("maximum", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReturnBook_ThrowsOnDoubleReturn()
    {
        var (books, members, loans) = Build();
        var book   = books.AddBook("Book", "Author", "", "Fiction", 1);
        var member = members.RegisterMember("Alice", "a@a.ca");
        var loan   = loans.BorrowBook(member.Id, book.Id);
        loans.ReturnBook(loan.Id);

        Assert.Throws<InvalidOperationException>(() => loans.ReturnBook(loan.Id));
    }

    [Fact]
    public void GetMemberLoans_ReturnsOnlyThatMembersLoans()
    {
        var (books, members, loans) = Build();
        var b1 = books.AddBook("Book A", "Author", "", "Fiction", 1);
        var b2 = books.AddBook("Book B", "Author", "", "Fiction", 1);
        var alice = members.RegisterMember("Alice", "a@a.ca");
        var bob   = members.RegisterMember("Bob",   "b@b.ca");

        loans.BorrowBook(alice.Id, b1.Id);
        loans.BorrowBook(bob.Id,   b2.Id);

        var aliceLoans = loans.GetMemberLoans(alice.Id);
        Assert.Single(aliceLoans);
        Assert.Equal(alice.Id, aliceLoans[0].MemberId);
    }

    [Fact]
    public void GetActiveLoans_ExcludesReturned()
    {
        var (books, members, loans) = Build();
        var b1 = books.AddBook("Book A", "Author", "", "Fiction", 1);
        var b2 = books.AddBook("Book B", "Author", "", "Fiction", 1);
        var member = members.RegisterMember("Alice", "a@a.ca");

        var loan1 = loans.BorrowBook(member.Id, b1.Id);
        loans.BorrowBook(member.Id, b2.Id);
        loans.ReturnBook(loan1.Id);

        var active = loans.GetActiveLoans();
        Assert.Single(active);
        Assert.NotEqual(loan1.Id, active[0].Id);
    }
}
