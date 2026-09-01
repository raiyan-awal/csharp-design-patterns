# 4.24 — Publish-Subscribe

## Intent

Publish-Subscribe (Pub-Sub) decouples message producers from message consumers by routing events through an intermediary event bus. Publishers broadcast events without knowing who — or how many — subscribers are listening, and subscribers register interest in specific event types without knowing who published them.

## The Problem It Solves

Without this pattern, a component that needs to notify others must hold direct references to every consumer:

```csharp
// Without Pub-Sub: EditorialService must know about every downstream consumer
public class EditorialService(EmailDigestService digest, BreakingNewsAlertService alerts, AnalyticsService analytics, ContentArchiveService archive)
{
    public void PublishArticle(Article article)
    {
        // every subscriber is hard-wired here
        digest.OnArticlePublished(article);
        alerts.OnArticlePublished(article);
        analytics.OnArticlePublished(article);
        archive.OnArticlePublished(article);
        // adding a new consumer requires changing EditorialService
    }
}
```

Problems this creates:
- **Tight coupling** — publisher must import and instantiate every consumer.
- **Open/Closed violation** — adding a new subscriber requires modifying the publisher.
- **Circular dependencies** — if a subscriber also needs to publish, the dependency graph becomes circular.
- **Hard to test** — the publisher can only be unit tested with all its dependents present.

## Solution: Event Bus

An event bus acts as the intermediary channel. Publishers call `Publish<TEvent>()` and subscribers call `Subscribe<TEvent>()` independently; neither side knows the other exists.

```csharp
// Publisher emits an event — knows nothing about subscribers
bus.Publish(new ArticlePublishedEvent(article, DateTimeOffset.UtcNow));

// Each subscriber registers its own handler — knows nothing about the publisher
bus.Subscribe<ArticlePublishedEvent>(digest.OnArticlePublished);
bus.Subscribe<ArticlePublishedEvent>(analytics.OnArticlePublished);
bus.Subscribe<ArticlePublishedEvent>(archive.OnArticlePublished);
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Event Bus interface | `IEventBus` | Contract for publish, subscribe, and unsubscribe |
| Concrete event bus | `InMemoryEventBus` | Thread-safe, type-keyed handler registry and dispatch |
| Events | `ArticlePublishedEvent`, `ArticleUpdatedEvent`, `BreakingNewsAlertEvent` | Immutable data carriers; one record per event type |
| Publisher | `EditorialService` | Creates articles and publishes events without knowing who listens |
| Subscribers | `EmailDigestService`, `BreakingNewsAlertService`, `AnalyticsService`, `ContentArchiveService` | Each subscribes to only the event types it cares about |

## Structure

```
4.24-PublishSubscribe/
├── PublishSubscribePattern/
│   ├── Core/
│   │   ├── IEventBus.cs                    ← Subscribe<T>, Unsubscribe<T>, Publish<T>
│   │   └── InMemoryEventBus.cs             ← ConcurrentDictionary + Lock, snapshot dispatch
│   ├── Domain/
│   │   ├── Article.cs                      ← sealed record
│   │   └── ArticleCategory.cs              ← enum (BreakingNews, Sports, Technology, Business, Entertainment)
│   ├── Events/
│   │   ├── ArticlePublishedEvent.cs        ← new article goes live
│   │   ├── ArticleUpdatedEvent.cs          ← correction/update (carries both Original and Updated)
│   │   └── BreakingNewsAlertEvent.cs       ← push-alert event, only fired for BreakingNews category
│   ├── Services/
│   │   ├── EditorialService.cs             ← publisher: publishes articles and fires events
│   │   ├── EmailDigestService.cs           ← subscriber: queues articles for batch digest
│   │   ├── BreakingNewsAlertService.cs     ← subscriber: sends push alerts for breaking news
│   │   ├── AnalyticsService.cs             ← subscriber: counts publishes by category and updates
│   │   └── ContentArchiveService.cs        ← subscriber: stores latest version + audit log
│   └── Program.cs
└── PublishSubscribePattern.Tests/
    └── PublishSubscribePatternTests.cs     ← 24 tests across 6 suites
```

## Key Code

### IEventBus — the contract

```csharp
public interface IEventBus
{
    void Publish<TEvent>(TEvent @event) where TEvent : class;
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}
```

The interface is generic: the type parameter `TEvent` is the routing key. Handlers are `Action<TEvent>` delegates — no interface to implement, no base class to inherit.

### InMemoryEventBus — dispatch with snapshot

```csharp
public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly Lock _lock = new();

    public void Publish<TEvent>(TEvent @event) where TEvent : class
    {
        List<Delegate> snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;
            snapshot = [..handlers];
        }
        foreach (var handler in snapshot)
            ((Action<TEvent>)handler)(@event);
    }
}
```

The lock is released before dispatch starts. Taking a snapshot of the handler list first means: a handler can subscribe or unsubscribe other handlers mid-dispatch without causing a `ConcurrentModificationException` or re-entrancy deadlock.

### EditorialService — publish without knowing subscribers

```csharp
public Article PublishArticle(string title, string body, ArticleCategory category, string author)
{
    var article = new Article(Guid.NewGuid(), title, body, category, author, DateTimeOffset.UtcNow);
    _articles[article.Id] = article;

    bus.Publish(new ArticlePublishedEvent(article, DateTimeOffset.UtcNow));

    if (category == ArticleCategory.BreakingNews)
        bus.Publish(new BreakingNewsAlertEvent(article, $"BREAKING: {title}", DateTimeOffset.UtcNow));

    return article;
}
```

`EditorialService` only knows about `IEventBus`. Adding a fifth subscriber requires zero changes to this class.

### Subscriber — subscribe at composition root

```csharp
bus.Subscribe<ArticlePublishedEvent>(digest.OnArticlePublished);
bus.Subscribe<ArticlePublishedEvent>(analytics.OnArticlePublished);
bus.Subscribe<ArticlePublishedEvent>(archive.OnArticlePublished);
bus.Subscribe<BreakingNewsAlertEvent>(alerts.OnBreakingNewsAlert);
bus.Subscribe<ArticleUpdatedEvent>(analytics.OnArticleUpdated);
bus.Subscribe<ArticleUpdatedEvent>(archive.OnArticleUpdated);
```

Each subscriber wires itself up independently. `EmailDigestService` never imports `BreakingNewsAlertService`; `BreakingNewsAlertService` never imports `AnalyticsService`. They are genuinely unaware of each other.

## Demo Scenarios

```
1. Basic Publish-Subscribe   — two handlers both receive the same event
2. Unsubscribe               — handler removed mid-session stops receiving
3. Type Filtering            — handlers only fire for their registered event type
4. Full Newsroom             — all four services wired, two articles published
5. Breaking News Alert       — BreakingNews category fires both ArticlePublished and BreakingNewsAlert
6. Article Correction        — UpdateArticle fires ArticleUpdatedEvent; archive stores new version
7. Email Digest Flush        — accumulated articles batched and cleared
8. Analytics Summary         — per-category publish counts and total update count
```

## When to Use

- Your system has multiple consumers that react to the same domain event, and you do not want them coupled to each other or to the source.
- You need to add or remove consumers at runtime or at the composition root without recompiling the publisher.
- You are implementing an event-driven architecture where modules should be independently deployable.
- You want to broadcast a single event to an open-ended number of listeners (fan-out).

## When NOT to Use

- You need a guaranteed response from a specific downstream system — use direct calls or the Request/Response pattern instead.
- The publisher needs to know whether any subscriber handled the event (e.g., error recovery that depends on consumer state).
- Debugging or tracing is critical and an implicit, invisible subscription chain would obscure the call flow too much.
- You only have one consumer and it will never grow — the indirection adds complexity for no benefit.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Loose coupling | Publisher and subscribers share only the event type; no direct dependency between them |
| Open/Closed | New subscribers are added at the composition root; the publisher never changes |
| Independent testability | Publishers and subscribers can be unit tested with a simple stub bus and captured handlers |
| Fan-out | One `Publish` call reaches any number of subscribers without extra code in the publisher |
| Runtime flexibility | Subscribers can register and unregister at any point, enabling feature toggles and A/B flows |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Invisible data flow | There is no static call site linking publisher to subscriber; tracing requires tooling or logging |
| No guaranteed delivery | `InMemoryEventBus` loses events if a subscriber throws or the process restarts; a durable broker is needed for reliability |
| Ordering not guaranteed | With multiple subscribers, invocation order is registration order — not always predictable in real systems |
| Error isolation | An exception in one subscriber can silently block subsequent subscribers unless the bus catches and swallows per-handler |

## Related Patterns

- **Observer (3.07)** — subjects hold direct references to observers; Pub-Sub decouples them through a bus. Observer is pull-based (subject notifies, observer calls back); Pub-Sub is push-based (bus delivers the full event).
- **Mediator (3.05)** — also centralises communication, but Mediator contains the coordination logic itself; a Pub-Sub bus is a dumb channel with no business logic.
- **Domain Event (4.12)** — typically used with Pub-Sub: aggregates raise domain events that are dispatched through a bus to independent handlers.
- **Outbox Pattern (4.20)** — solves the dual-write problem when Pub-Sub events must survive a process crash: write the event to a database outbox in the same transaction, then relay it to the bus asynchronously.
- **CQRS (4.03)** — commands mutate state and typically raise events that Pub-Sub delivers to read-model projectors.

## Running the Demo

```bash
cd src/4-Enterprise/4.24-PublishSubscribe/PublishSubscribePattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.24-PublishSubscribe/PublishSubscribePattern.Tests
dotnet test
```
