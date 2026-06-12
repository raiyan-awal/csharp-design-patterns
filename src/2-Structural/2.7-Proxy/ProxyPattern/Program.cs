using ProxyPattern;

// ============================================================
// PROXY PATTERN — DEMO
// ============================================================
// Three proxy variants wrap the same IDocumentRepository:
//   Virtual Proxy      — defers creation until first call
//   Caching Proxy      — skips I/O on repeated reads
//   Authorization Proxy — enforces access control
// All three implement IDocumentRepository and are stackable.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       PROXY PATTERN — Document Service               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Three proxy variants wrap the same IDocumentRepository:");
Console.WriteLine("  Virtual (Lazy) Proxy — defers construction until first call");
Console.WriteLine("  Caching Proxy        — caches results, skips repeated I/O");
Console.WriteLine("  Authorization Proxy  — checks permissions before forwarding");
Console.WriteLine();
Console.WriteLine("All three implement IDocumentRepository — they are");
Console.WriteLine("interchangeable and stackable without the caller knowing.");

Pause();

// ── DEMO 1: Virtual (Lazy) Proxy ─────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Virtual (Lazy) Proxy");
Console.WriteLine("         Repository is NOT created until the first call");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var lazyProxy = new LazyDocumentProxy();

Console.WriteLine($"Proxy created.  IsInitialized = {lazyProxy.IsInitialized}");
Console.WriteLine("(DocumentRepository has NOT been constructed yet)");
Console.WriteLine();
Console.WriteLine("Calling Load(\"doc-1\")...");

var doc = lazyProxy.Load("doc-1");

Console.WriteLine();
Console.WriteLine($"Loaded: [{doc?.Id}] {doc?.Title}");
Console.WriteLine($"IsInitialized = {lazyProxy.IsInitialized}  (created on first use)");
Console.WriteLine();
Console.WriteLine("Calling Load(\"doc-1\") a second time — same inner instance:");
lazyProxy.Load("doc-1");
Console.WriteLine($"IsInitialized = {lazyProxy.IsInitialized}  (no re-creation)");

Pause();

// ── DEMO 2: Caching Proxy ─────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Caching Proxy");
Console.WriteLine("         First load goes to the repository; repeats hit cache");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var repo         = new DocumentRepository();
var cachingProxy = new CachingDocumentProxy(repo);

Console.WriteLine("Load doc-1 (first time — cache miss):");
cachingProxy.Load("doc-1");

Console.WriteLine();
Console.WriteLine("Load doc-2 (first time — cache miss):");
cachingProxy.Load("doc-2");

Console.WriteLine();
Console.WriteLine("Load doc-1 (second time — cache hit, repo not called):");
cachingProxy.Load("doc-1");

Console.WriteLine();
Console.WriteLine("Load doc-1 (third time — cache hit again):");
cachingProxy.Load("doc-1");

Console.WriteLine();
Console.WriteLine($"Repository.LoadCallCount : {repo.LoadCallCount}  (only 2 real loads)");
Console.WriteLine($"Cache hits               : {cachingProxy.CacheHitCount}");
Console.WriteLine($"Cache misses             : {cachingProxy.CacheMissCount}");

Console.WriteLine();
Console.WriteLine("Invalidating doc-1 and reloading (forces a trip to repo):");
cachingProxy.Invalidate("doc-1");
cachingProxy.Load("doc-1");
Console.WriteLine($"Repository.LoadCallCount after invalidation: {repo.LoadCallCount}");

Pause();

// ── DEMO 3: Authorization Proxy ───────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — Authorization Proxy");
Console.WriteLine("         Only permitted documents are accessible");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var innerRepo  = new DocumentRepository();
// alice is permitted to read doc-1 and doc-2 only
var aliceProxy = new AuthorizationDocumentProxy(innerRepo, "alice", ["doc-1", "doc-2"]);

Console.WriteLine("Alice's visible document IDs:");
foreach (var id in aliceProxy.ListIds())
    Console.WriteLine($"  {id}");

Console.WriteLine();
Console.WriteLine("Loading doc-1 (allowed):");
var allowed = aliceProxy.Load("doc-1");
Console.WriteLine($"  Result: [{allowed?.Id}] {allowed?.Title}");

Console.WriteLine();
Console.WriteLine("Loading doc-4 (not in alice's allowed list):");
try
{
    aliceProxy.Load("doc-4");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"  Exception: {ex.Message}");
}

Pause();

// ── DEMO 4: All three proxies stacked ────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Stacked: Authorization → Cache → Lazy");
Console.WriteLine("         Auth gates first; cache before any real I/O;");
Console.WriteLine("         real repo constructed only if needed");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var lazyLayer  = new LazyDocumentProxy();
var cacheLayer = new CachingDocumentProxy(lazyLayer);
var authLayer  = new AuthorizationDocumentProxy(cacheLayer, "bob", ["doc-1", "doc-3"]);

Console.WriteLine($"Before any call — LazyProxy.IsInitialized = {lazyLayer.IsInitialized}");
Console.WriteLine();

Console.WriteLine("Bob loads doc-3 (allowed):");
var bobDoc = authLayer.Load("doc-3");
Console.WriteLine($"  Result: [{bobDoc?.Id}] {bobDoc?.Title}");
Console.WriteLine($"  LazyProxy.IsInitialized = {lazyLayer.IsInitialized}");

Console.WriteLine();
Console.WriteLine("Bob loads doc-3 again (cache hit — repo NOT called again):");
authLayer.Load("doc-3");
Console.WriteLine($"  CacheHits = {cacheLayer.CacheHitCount}");

Console.WriteLine();
Console.WriteLine("Bob tries doc-2 (denied at auth — cache is never checked):");
try
{
    authLayer.Load("doc-2");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"  Exception: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • LazyDocumentProxy       — creates inner on first call (Virtual Proxy)");
Console.WriteLine("  • CachingDocumentProxy    — caches results per id (Caching Proxy)");
Console.WriteLine("  • AuthorizationDocumentProxy — enforces per-user allow-list (Protection Proxy)");
Console.WriteLine("  • All implement IDocumentRepository — stackable, interchangeable");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
