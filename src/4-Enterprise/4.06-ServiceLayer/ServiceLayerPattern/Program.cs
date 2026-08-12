using ServiceLayerPattern.Repositories;
using ServiceLayerPattern.Services;

Console.WriteLine("=== 4.06 Service Layer Pattern — Toronto Public Library ===");
Console.WriteLine();

// ── Problem: without a Service Layer ─────────────────────────────────────────
Console.WriteLine("── The Problem Without a Service Layer ──────────────────────────────────────");
Console.WriteLine();
Console.WriteLine("  Without a service layer, every caller (controller, CLI, background job)");
Console.WriteLine("  must duplicate the same coordination and business rule checks:");
Console.WriteLine();
Console.WriteLine("  // In a controller — too much responsibility:");
Console.WriteLine("  var member = memberRepo.GetById(memberId);");
Console.WriteLine("  if (member == null) return NotFound();");
Console.WriteLine("  if (!member.IsActive) return BadRequest(\"Inactive member\");");
Console.WriteLine("  if (loanRepo.MemberHasOverdueLoans(memberId)) return BadRequest(\"Overdue\");");
Console.WriteLine("  if (loanRepo.CountActiveByMemberId(memberId) >= 5) return BadRequest(\"Limit\");");
Console.WriteLine("  var book = bookRepo.GetById(bookId);");
Console.WriteLine("  if (book == null) return NotFound();");
Console.WriteLine("  if (!book.IsAvailable) return BadRequest(\"Unavailable\");");
Console.WriteLine("  book.CheckOut(); bookRepo.Update(book);");
Console.WriteLine("  loanRepo.Add(new Loan(...));");
Console.WriteLine();
Console.WriteLine("  // With a service layer — one line, rules enforced centrally:");
Console.WriteLine("  var loan = loanService.BorrowBook(memberId, bookId);");

Pause();

// ── Setup ─────────────────────────────────────────────────────────────────────
var bookRepo   = new InMemoryBookRepository();
var memberRepo = new InMemoryMemberRepository();
var loanRepo   = new InMemoryLoanRepository();

IBookService   bookService   = new BookService(bookRepo);
IMemberService memberService = new MemberService(memberRepo);
ILoanService   loanService   = new LoanService(loanRepo, bookRepo, memberRepo);

// ── Demo 1: Book Catalogue ───────────────────────────────────────────────────
Console.WriteLine("── Demo 1: Book Catalogue ───────────────────────────────────────────────────");
Console.WriteLine();

var handmaidsTale  = bookService.AddBook("The Handmaid's Tale",      "Margaret Atwood",   "978-0-7710-0813-2", "Fiction",    3);
var stoneAngel     = bookService.AddBook("The Stone Angel",           "Margaret Laurence", "978-0-7710-4891-6", "Fiction",    2);
var anneGables     = bookService.AddBook("Anne of Green Gables",      "L.M. Montgomery",   "978-0-7710-6099-4", "Fiction",    4);
var skinOfALion    = bookService.AddBook("In the Skin of a Lion",     "Michael Ondaatje",  "978-0-7710-6887-7", "Fiction",    2);
var watchingYou    = bookService.AddBook("Watching You Without Me",   "Lynn Coady",        "978-0-7710-2237-3", "Thriller",   1);

Console.WriteLine($"  Catalogue loaded — {bookService.GetAllBooks().Count} titles available");
Console.WriteLine();

Console.WriteLine("  Search for 'margaret':");
foreach (var b in bookService.SearchBooks("margaret"))
    Console.WriteLine($"    [{b.Id}] {b.Title} by {b.Author}  ({b.AvailableCopies}/{b.TotalCopies} copies)");

Pause();

// ── Demo 2: Member Registration ──────────────────────────────────────────────
Console.WriteLine("── Demo 2: Member Registration ──────────────────────────────────────────────");
Console.WriteLine();

var alice   = memberService.RegisterMember("Alice Tremblay",  "alice@example.ca");
var bob     = memberService.RegisterMember("Bob Nguyen",      "bob@example.ca");
var carol   = memberService.RegisterMember("Carol MacDonald", "carol@example.ca");

Console.WriteLine($"  Registered {memberService.GetAllMembers().Count} members:");
foreach (var m in memberService.GetAllMembers())
    Console.WriteLine($"    [{m.Id}] {m.Name}  {m.MemberNumber}  Active={m.IsActive}");

Pause();

// ── Demo 3: Borrowing Books ───────────────────────────────────────────────────
Console.WriteLine("── Demo 3: Borrowing Books ──────────────────────────────────────────────────");
Console.WriteLine();

var loan1 = loanService.BorrowBook(alice.Id, handmaidsTale.Id);
var loan2 = loanService.BorrowBook(alice.Id, stoneAngel.Id);
var loan3 = loanService.BorrowBook(bob.Id,   anneGables.Id);

Console.WriteLine($"  Alice borrowed: {loan1.BookTitle}  (due {loan1.DueDate:yyyy-MM-dd})");
Console.WriteLine($"  Alice borrowed: {loan2.BookTitle}  (due {loan2.DueDate:yyyy-MM-dd})");
Console.WriteLine($"  Bob   borrowed: {loan3.BookTitle}  (due {loan3.DueDate:yyyy-MM-dd})");
Console.WriteLine();

var remaining = bookService.GetBook(handmaidsTale.Id);
Console.WriteLine($"  '{remaining.Title}' copies remaining: {remaining.AvailableCopies}/{remaining.TotalCopies}");
Console.WriteLine();

Console.WriteLine("  Business rule — book not available (last copy of Watching You):");
loanService.BorrowBook(alice.Id, watchingYou.Id);
try { loanService.BorrowBook(bob.Id, watchingYou.Id); }
catch (InvalidOperationException ex) { Console.WriteLine($"    Blocked: {ex.Message}"); }

Console.WriteLine();
Console.WriteLine("  Business rule — inactive member cannot borrow:");
memberService.DeactivateMember(carol.Id);
try { loanService.BorrowBook(carol.Id, skinOfALion.Id); }
catch (InvalidOperationException ex) { Console.WriteLine($"    Blocked: {ex.Message}"); }

Pause();

// ── Demo 4: Returning Books ───────────────────────────────────────────────────
Console.WriteLine("── Demo 4: Returning Books ──────────────────────────────────────────────────");
Console.WriteLine();

loanService.ReturnBook(loan2.Id);
var updated = bookService.GetBook(stoneAngel.Id);
Console.WriteLine($"  Alice returned '{loan2.BookTitle}'");
Console.WriteLine($"  '{updated.Title}' copies now: {updated.AvailableCopies}/{updated.TotalCopies}");
Console.WriteLine();

Console.WriteLine("  Business rule — cannot return a loan twice:");
try { loanService.ReturnBook(loan2.Id); }
catch (InvalidOperationException ex) { Console.WriteLine($"    Blocked: {ex.Message}"); }

Pause();

// ── Demo 5: Loan Reports ──────────────────────────────────────────────────────
Console.WriteLine("── Demo 5: Loan Reports ─────────────────────────────────────────────────────");
Console.WriteLine();

Console.WriteLine("  Active loans:");
foreach (var l in loanService.GetActiveLoans())
    Console.WriteLine($"    [{l.Id}] '{l.BookTitle}'  → {l.MemberName}  due {l.DueDate:yyyy-MM-dd}  [{l.Status}]");

Console.WriteLine();
Console.WriteLine("  Alice's full history:");
foreach (var l in loanService.GetMemberLoans(alice.Id))
    Console.WriteLine($"    [{l.Id}] '{l.BookTitle}'  [{l.Status}]  returned={l.ReturnedAt?.ToString("yyyy-MM-dd") ?? "—"}");

Console.WriteLine();
Console.WriteLine("  (Overdue loans are flagged automatically based on due date vs. today)");
Console.WriteLine($"  Overdue count: {loanService.GetOverdueLoans().Count}");

Console.WriteLine();
Console.WriteLine("=== End of demo ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
