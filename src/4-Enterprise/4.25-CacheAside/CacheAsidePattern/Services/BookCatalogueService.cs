using CacheAsidePattern.Core;
using CacheAsidePattern.Data;
using CacheAsidePattern.Domain;

namespace CacheAsidePattern.Services;

public sealed class BookCatalogueService(
    IBookRepository repository,
    ICache<string, Book> bookCache,
    ICache<string, IReadOnlyList<Book>> listCache)
{
    private const string BookPrefix   = "book:";
    private const string AuthorPrefix = "author:";

    public Book? GetById(string id)
    {
        var key = $"{BookPrefix}{id}";
        if (bookCache.TryGet(key, out var cached))
            return cached;

        var book = repository.FindById(id);
        if (book is not null)
            bookCache.Set(key, book);
        return book;
    }

    public IReadOnlyList<Book> GetByAuthor(string author)
    {
        var key = $"{AuthorPrefix}{author}";
        if (listCache.TryGet(key, out var cached))
            return cached;

        var books = repository.FindByAuthor(author);
        listCache.Set(key, books);
        return books;
    }

    public void Save(Book book)
    {
        repository.Save(book);
        bookCache.Remove($"{BookPrefix}{book.Id}");
        // clear all author lists — we don't track which authors are affected
        listCache.Clear();
    }

    public void Delete(string id)
    {
        repository.Delete(id);
        bookCache.Remove($"{BookPrefix}{id}");
        listCache.Clear();
    }
}
