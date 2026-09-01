using CacheAsidePattern.Core;
using CacheAsidePattern.Data;
using CacheAsidePattern.Domain;
using CacheAsidePattern.Services;

namespace CacheAsidePattern.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────────

file sealed class FakeClock(DateTimeOffset initial)
{
    private DateTimeOffset _current = initial;
    public DateTimeOffset Now() => _current;
    public void Advance(TimeSpan by) => _current = _current.Add(by);
}

file static class Factory
{
    public static Book MakeBook(string id = "b1", string author = "Margaret Atwood") =>
        new(id, $"Title-{id}", author, $"isbn-{id}", 19.99m, "Fiction");

    public static (InMemoryBookRepository Repo, InMemoryCache<string, Book> BookCache,
                   InMemoryCache<string, IReadOnlyList<Book>> ListCache, BookCatalogueService Service)
        MakeCatalogue(Func<DateTimeOffset>? clock = null)
    {
        var repo      = new InMemoryBookRepository();
        var bookCache = new InMemoryCache<string, Book>(clock);
        var listCache = new InMemoryCache<string, IReadOnlyList<Book>>(clock);
        var service   = new BookCatalogueService(repo, bookCache, listCache);
        return (repo, bookCache, listCache, service);
    }
}

// ── Suite 1: InMemoryCache — TryGet basics ────────────────────────────────────

public sealed class InMemoryCache_TryGet
{
    [Fact]
    public void TryGet_OnEmptyCache_ReturnsFalse()
    {
        var cache = new InMemoryCache<string, Book>();
        Assert.False(cache.TryGet("missing", out _));
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsTrueAndValue()
    {
        var cache = new InMemoryCache<string, Book>();
        var book  = Factory.MakeBook();
        cache.Set("k", book);
        Assert.True(cache.TryGet("k", out var result));
        Assert.Equal(book, result);
    }

    [Fact]
    public void TryGet_ExpiredEntry_ReturnsFalse()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var cache = new InMemoryCache<string, Book>(clock.Now);
        cache.Set("k", Factory.MakeBook(), TimeSpan.FromMinutes(5));

        clock.Advance(TimeSpan.FromMinutes(6));

        Assert.False(cache.TryGet("k", out _));
    }

    [Fact]
    public void TryGet_NonExpiredEntry_ReturnsTrue()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var cache = new InMemoryCache<string, Book>(clock.Now);
        cache.Set("k", Factory.MakeBook(), TimeSpan.FromMinutes(10));

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.True(cache.TryGet("k", out _));
    }
}

// ── Suite 2: InMemoryCache — TTL ─────────────────────────────────────────────

public sealed class InMemoryCache_TTL
{
    [Fact]
    public void EntryWithNoTtl_NeverExpires()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var cache = new InMemoryCache<string, Book>(clock.Now);
        cache.Set("k", Factory.MakeBook());

        clock.Advance(TimeSpan.FromDays(365));

        Assert.True(cache.TryGet("k", out _));
    }

    [Fact]
    public void EntryWithTtl_ExpiresAfterDeadline()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var cache = new InMemoryCache<string, Book>(clock.Now);
        cache.Set("k", Factory.MakeBook(), TimeSpan.FromSeconds(30));

        clock.Advance(TimeSpan.FromSeconds(31));

        Assert.False(cache.TryGet("k", out _));
    }

    [Fact]
    public void DifferentEntries_CanHaveDifferentTtls()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var cache = new InMemoryCache<string, Book>(clock.Now);
        cache.Set("short", Factory.MakeBook("b1"), TimeSpan.FromMinutes(1));
        cache.Set("long",  Factory.MakeBook("b2"), TimeSpan.FromHours(1));

        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.False(cache.TryGet("short", out _));
        Assert.True(cache.TryGet("long",  out _));
    }
}

// ── Suite 3: InMemoryCache — Remove and Clear ────────────────────────────────

public sealed class InMemoryCache_Remove_And_Clear
{
    [Fact]
    public void Remove_DeletesSpecificEntry()
    {
        var cache = new InMemoryCache<string, Book>();
        cache.Set("a", Factory.MakeBook("b1"));
        cache.Set("b", Factory.MakeBook("b2"));
        cache.Remove("a");
        Assert.False(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
    }

    [Fact]
    public void Remove_NonExistentKey_DoesNotThrow()
    {
        var cache = new InMemoryCache<string, Book>();
        var ex = Record.Exception(() => cache.Remove("nope"));
        Assert.Null(ex);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var cache = new InMemoryCache<string, Book>();
        cache.Set("a", Factory.MakeBook("b1"));
        cache.Set("b", Factory.MakeBook("b2"));
        cache.Clear();
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Count_ReflectsLiveEntriesOnly()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var cache = new InMemoryCache<string, Book>(clock.Now);
        cache.Set("live",    Factory.MakeBook("b1"));
        cache.Set("expires", Factory.MakeBook("b2"), TimeSpan.FromMinutes(1));
        clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(1, cache.Count);
    }
}

// ── Suite 4: InMemoryCache — Metrics ─────────────────────────────────────────

public sealed class InMemoryCache_Metrics
{
    [Fact]
    public void Hit_IncrementsHitCount()
    {
        var cache = new InMemoryCache<string, Book>();
        cache.Set("k", Factory.MakeBook());
        cache.TryGet("k", out _);
        cache.TryGet("k", out _);
        Assert.Equal(2, cache.Hits);
    }

    [Fact]
    public void Miss_IncrementsMissCount()
    {
        var cache = new InMemoryCache<string, Book>();
        cache.TryGet("nope", out _);
        cache.TryGet("also-nope", out _);
        Assert.Equal(2, cache.Misses);
    }

    [Fact]
    public void ExpiredEntry_CountsAsMiss()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var cache = new InMemoryCache<string, Book>(clock.Now);
        cache.Set("k", Factory.MakeBook(), TimeSpan.FromMinutes(1));
        clock.Advance(TimeSpan.FromMinutes(2));
        cache.TryGet("k", out _);
        Assert.Equal(1, cache.Misses);
        Assert.Equal(0, cache.Hits);
    }
}

// ── Suite 5: BookCatalogueService — Read path ────────────────────────────────

public sealed class BookCatalogueService_ReadPath
{
    [Fact]
    public void GetById_CacheMiss_LoadsFromRepository()
    {
        var (repo, _, _, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1")]);

        var book = service.GetById("b1");

        Assert.NotNull(book);
        Assert.Equal(1, repo.LoadCount);
    }

    [Fact]
    public void GetById_CacheHit_DoesNotQueryRepository()
    {
        var (repo, bookCache, _, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1")]);

        service.GetById("b1");   // miss — populates cache
        var loadAfterFirst = repo.LoadCount;
        service.GetById("b1");   // hit
        service.GetById("b1");   // hit

        Assert.Equal(loadAfterFirst, repo.LoadCount);
        Assert.Equal(2, bookCache.Hits);
    }

    [Fact]
    public void GetById_NonExistentBook_ReturnsNullAndDoesNotCache()
    {
        var (repo, bookCache, _, service) = Factory.MakeCatalogue();

        var result = service.GetById("missing");

        Assert.Null(result);
        Assert.Equal(0, bookCache.Count);
    }

    [Fact]
    public void GetByAuthor_CacheMiss_LoadsFromRepository()
    {
        var (repo, _, listCache, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1", "Margaret Atwood"), Factory.MakeBook("b2", "Margaret Atwood")]);

        var books = service.GetByAuthor("Margaret Atwood");

        Assert.Equal(2, books.Count);
        Assert.Equal(1, repo.LoadCount);
        Assert.Equal(1, listCache.Count);
    }

    [Fact]
    public void GetByAuthor_CacheHit_DoesNotQueryRepository()
    {
        var (repo, _, listCache, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1", "Wayson Choy")]);

        service.GetByAuthor("Wayson Choy");   // miss
        var loadAfterFirst = repo.LoadCount;
        service.GetByAuthor("Wayson Choy");   // hit

        Assert.Equal(loadAfterFirst, repo.LoadCount);
        Assert.Equal(1, listCache.Hits);
    }

    [Fact]
    public void GetById_DifferentBooks_EachMissOnce()
    {
        var (repo, bookCache, _, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1"), Factory.MakeBook("b2"), Factory.MakeBook("b3")]);

        service.GetById("b1");
        service.GetById("b2");
        service.GetById("b3");

        Assert.Equal(3, repo.LoadCount);
        Assert.Equal(3, bookCache.Misses);
    }
}

// ── Suite 6: BookCatalogueService — Write path ───────────────────────────────

public sealed class BookCatalogueService_WritePath
{
    [Fact]
    public void Save_UpdatesRepositoryAndInvalidatesBookCache()
    {
        var (repo, bookCache, _, service) = Factory.MakeCatalogue();
        var original = Factory.MakeBook("b1");
        repo.Seed([original]);

        service.GetById("b1");   // populate cache
        Assert.Equal(1, bookCache.Count);

        service.Save(original with { PriceCAD = 29.99m });

        Assert.Equal(0, bookCache.Count);   // cache entry evicted
    }

    [Fact]
    public void Save_FreshGetReturnsUpdatedValue()
    {
        var (repo, _, _, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1")]);

        service.GetById("b1");   // cache it at original price ($19.99)
        service.Save(new Book("b1", "Title-b1", "Margaret Atwood", "isbn-b1", 34.99m, "Fiction"));

        var refreshed = service.GetById("b1");

        Assert.Equal(34.99m, refreshed!.PriceCAD);
    }

    [Fact]
    public void Save_ClearsAuthorListCache()
    {
        var (repo, _, listCache, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1", "Margaret Atwood")]);
        service.GetByAuthor("Margaret Atwood");   // populate list cache
        Assert.Equal(1, listCache.Count);

        service.Save(Factory.MakeBook("b2", "Margaret Atwood"));

        Assert.Equal(0, listCache.Count);
    }

    [Fact]
    public void Delete_RemovesFromRepositoryAndInvalidatesBookCache()
    {
        var (repo, bookCache, _, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1")]);
        service.GetById("b1");   // populate cache
        Assert.Equal(1, bookCache.Count);

        service.Delete("b1");

        Assert.Equal(0, bookCache.Count);
        Assert.Null(service.GetById("b1"));
    }

    [Fact]
    public void Delete_ClearsAuthorListCache()
    {
        var (repo, _, listCache, service) = Factory.MakeCatalogue();
        repo.Seed([Factory.MakeBook("b1", "Wayson Choy")]);
        service.GetByAuthor("Wayson Choy");   // populate list cache
        Assert.Equal(1, listCache.Count);

        service.Delete("b1");

        Assert.Equal(0, listCache.Count);
    }

    [Fact]
    public void Save_AfterTtlExpiry_ReloadsFromRepository()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var repo      = new InMemoryBookRepository();
        var bookCache = new InMemoryCache<string, Book>(clock.Now);
        var listCache = new InMemoryCache<string, IReadOnlyList<Book>>(clock.Now);
        var service   = new BookCatalogueService(repo, bookCache, listCache);

        var book = Factory.MakeBook("b1");
        repo.Seed([book]);
        bookCache.Set("book:b1", book, TimeSpan.FromMinutes(5));

        clock.Advance(TimeSpan.FromMinutes(6));

        // TTL expired — next GetById should miss and reload
        var loadBefore = repo.LoadCount;
        service.GetById("b1");
        Assert.Equal(loadBefore + 1, repo.LoadCount);
    }
}
