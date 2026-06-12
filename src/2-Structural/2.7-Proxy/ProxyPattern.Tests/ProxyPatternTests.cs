using ProxyPattern;

namespace ProxyPattern.Tests;

// ── Test doubles ──────────────────────────────────────────────

file sealed class TrackingRepository : IDocumentRepository
{
    private readonly Document[] _docs;
    public int LoadCallCount { get; private set; }

    public TrackingRepository(params Document[] docs) => _docs = docs;

    public Document? Load(string id)
    {
        LoadCallCount++;
        return _docs.FirstOrDefault(d => d.Id == id);
    }

    public IReadOnlyList<string> ListIds() => _docs.Select(d => d.Id).ToList();
}

// ── LazyDocumentProxy ─────────────────────────────────────────

public class LazyDocumentProxyTests
{
    private static Document Doc(string id) => new(id, $"Title {id}", "content", "owner");

    [Fact]
    public void IsInitialized_IsFalse_BeforeAnyCall()
    {
        var proxy = new LazyDocumentProxy(() => new TrackingRepository());
        Assert.False(proxy.IsInitialized);
    }

    [Fact]
    public void IsInitialized_IsTrue_AfterLoad()
    {
        var proxy = new LazyDocumentProxy(() => new TrackingRepository());
        proxy.Load("anything");
        Assert.True(proxy.IsInitialized);
    }

    [Fact]
    public void IsInitialized_IsTrue_AfterListIds()
    {
        var proxy = new LazyDocumentProxy(() => new TrackingRepository());
        proxy.ListIds();
        Assert.True(proxy.IsInitialized);
    }

    [Fact]
    public void Load_ReturnsDocument_WhenExists()
    {
        var doc   = Doc("d1");
        var proxy = new LazyDocumentProxy(() => new TrackingRepository(doc));

        var result = proxy.Load("d1");

        Assert.NotNull(result);
        Assert.Equal("d1", result.Id);
    }

    [Fact]
    public void Load_ReturnsNull_WhenMissing()
    {
        var proxy = new LazyDocumentProxy(() => new TrackingRepository());
        Assert.Null(proxy.Load("missing"));
    }

    [Fact]
    public void InnerRepository_CreatedOnlyOnce_OnMultipleCalls()
    {
        int factoryCallCount = 0;
        var proxy = new LazyDocumentProxy(() =>
        {
            factoryCallCount++;
            return new TrackingRepository(Doc("d1"));
        });

        proxy.Load("d1");
        proxy.Load("d1");
        proxy.Load("d1");

        Assert.Equal(1, factoryCallCount);
    }
}

// ── CachingDocumentProxy ──────────────────────────────────────

public class CachingDocumentProxyTests
{
    private static Document Doc(string id) => new(id, $"Title {id}", "content", "owner");

    [Fact]
    public void Load_FirstCall_ForwardsToInner()
    {
        var inner = new TrackingRepository(Doc("d1"));
        var proxy = new CachingDocumentProxy(inner);

        proxy.Load("d1");

        Assert.Equal(1, inner.LoadCallCount);
        Assert.Equal(0, proxy.CacheHitCount);
        Assert.Equal(1, proxy.CacheMissCount);
    }

    [Fact]
    public void Load_SecondCall_SameId_ReturnsCachedDocument()
    {
        var inner = new TrackingRepository(Doc("d1"));
        var proxy = new CachingDocumentProxy(inner);

        var first  = proxy.Load("d1");
        var second = proxy.Load("d1");

        Assert.Equal(1, inner.LoadCallCount);
        Assert.Equal(1, proxy.CacheHitCount);
        Assert.Same(first, second);
    }

    [Fact]
    public void Load_DifferentIds_BothForwardToInner()
    {
        var inner = new TrackingRepository(Doc("d1"), Doc("d2"));
        var proxy = new CachingDocumentProxy(inner);

        proxy.Load("d1");
        proxy.Load("d2");

        Assert.Equal(2, inner.LoadCallCount);
        Assert.Equal(2, proxy.CacheMissCount);
    }

    [Fact]
    public void Invalidate_ForcesNextCallToForwardToInner()
    {
        var inner = new TrackingRepository(Doc("d1"));
        var proxy = new CachingDocumentProxy(inner);

        proxy.Load("d1");
        proxy.Invalidate("d1");
        proxy.Load("d1");

        Assert.Equal(2, inner.LoadCallCount);
    }

    [Fact]
    public void InvalidateAll_ForcesAllCallsToForwardToInner()
    {
        var inner = new TrackingRepository(Doc("d1"), Doc("d2"));
        var proxy = new CachingDocumentProxy(inner);

        proxy.Load("d1");
        proxy.Load("d2");
        proxy.InvalidateAll();
        proxy.Load("d1");
        proxy.Load("d2");

        Assert.Equal(4, inner.LoadCallCount);
    }

    [Fact]
    public void Load_NullResult_IsCached_DoesNotKeepCallingInner()
    {
        var inner = new TrackingRepository();   // empty — all loads return null
        var proxy = new CachingDocumentProxy(inner);

        proxy.Load("missing");
        proxy.Load("missing");

        Assert.Equal(1, inner.LoadCallCount);   // null is cached on first miss
        Assert.Equal(1, proxy.CacheHitCount);
    }
}

// ── AuthorizationDocumentProxy ────────────────────────────────

public class AuthorizationDocumentProxyTests
{
    private static Document Doc(string id) => new(id, $"Title {id}", "content", "owner");

    [Fact]
    public void Load_AllowedDocument_ReturnsDocument()
    {
        var inner = new TrackingRepository(Doc("d1"), Doc("d2"));
        var proxy = new AuthorizationDocumentProxy(inner, "alice", ["d1", "d2"]);

        var result = proxy.Load("d1");

        Assert.NotNull(result);
        Assert.Equal("d1", result.Id);
    }

    [Fact]
    public void Load_NotAllowedDocument_ThrowsUnauthorizedAccessException()
    {
        var inner = new TrackingRepository(Doc("d1"), Doc("secret"));
        var proxy = new AuthorizationDocumentProxy(inner, "alice", ["d1"]);

        Assert.Throws<UnauthorizedAccessException>(() => proxy.Load("secret"));
    }

    [Fact]
    public void Load_NotAllowedDocument_DoesNotForwardToInner()
    {
        var inner = new TrackingRepository(Doc("d1"), Doc("secret"));
        var proxy = new AuthorizationDocumentProxy(inner, "alice", ["d1"]);

        try { proxy.Load("secret"); } catch { }

        Assert.Equal(0, inner.LoadCallCount);
    }

    [Fact]
    public void Load_AllowedDocument_ForwardsToInner()
    {
        var inner = new TrackingRepository(Doc("d1"));
        var proxy = new AuthorizationDocumentProxy(inner, "alice", ["d1"]);

        proxy.Load("d1");

        Assert.Equal(1, inner.LoadCallCount);
    }

    [Fact]
    public void ListIds_ReturnsOnlyAllowedIds()
    {
        var inner = new TrackingRepository(Doc("d1"), Doc("d2"), Doc("d3"), Doc("d4"));
        var proxy = new AuthorizationDocumentProxy(inner, "alice", ["d1", "d3"]);

        var ids = proxy.ListIds();

        Assert.Equal(2, ids.Count);
        Assert.Contains("d1", ids);
        Assert.Contains("d3", ids);
        Assert.DoesNotContain("d2", ids);
        Assert.DoesNotContain("d4", ids);
    }

    [Fact]
    public void Load_EmptyAllowedIds_DeniesEverything()
    {
        var inner = new TrackingRepository(Doc("d1"));
        var proxy = new AuthorizationDocumentProxy(inner, "alice");  // no allowedIds

        Assert.Throws<UnauthorizedAccessException>(() => proxy.Load("d1"));
    }

    [Fact]
    public void ListIds_EmptyAllowedIds_ReturnsEmpty()
    {
        var inner = new TrackingRepository(Doc("d1"), Doc("d2"));
        var proxy = new AuthorizationDocumentProxy(inner, "alice");  // no allowedIds

        Assert.Empty(proxy.ListIds());
    }
}
