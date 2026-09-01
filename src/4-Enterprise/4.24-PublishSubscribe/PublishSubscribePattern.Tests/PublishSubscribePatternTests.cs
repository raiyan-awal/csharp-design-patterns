using PublishSubscribePattern.Core;
using PublishSubscribePattern.Domain;
using PublishSubscribePattern.Events;
using PublishSubscribePattern.Services;

namespace PublishSubscribePattern.Tests;

// ── Helpers ──────────────────────────────────────────────────────────────────

file static class Factory
{
    public static Article MakeArticle(
        ArticleCategory category = ArticleCategory.Technology,
        string title = "Test Article",
        string author = "Test Author") =>
        new(Guid.NewGuid(), title, "Body text.", category, author, DateTimeOffset.UtcNow);

    public static (InMemoryEventBus Bus, EditorialService Editorial) MakeNewsroom()
    {
        var bus = new InMemoryEventBus();
        return (bus, new EditorialService(bus));
    }
}

// ── Suite 1: InMemoryEventBus — Subscribe and Publish ────────────────────────

public sealed class EventBus_Subscribe_And_Publish
{
    [Fact]
    public void SingleSubscriber_ReceivesPublishedEvent()
    {
        var bus = new InMemoryEventBus();
        ArticlePublishedEvent? received = null;
        bus.Subscribe<ArticlePublishedEvent>(e => received = e);

        var article = Factory.MakeArticle();
        bus.Publish(new ArticlePublishedEvent(article, DateTimeOffset.UtcNow));

        Assert.NotNull(received);
        Assert.Equal(article.Id, received.Article.Id);
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveEvent()
    {
        var bus = new InMemoryEventBus();
        int count = 0;
        bus.Subscribe<ArticlePublishedEvent>(_ => count++);
        bus.Subscribe<ArticlePublishedEvent>(_ => count++);
        bus.Subscribe<ArticlePublishedEvent>(_ => count++);

        bus.Publish(new ArticlePublishedEvent(Factory.MakeArticle(), DateTimeOffset.UtcNow));

        Assert.Equal(3, count);
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new InMemoryEventBus();
        var ex = Record.Exception(() =>
            bus.Publish(new ArticlePublishedEvent(Factory.MakeArticle(), DateTimeOffset.UtcNow)));
        Assert.Null(ex);
    }

    [Fact]
    public void EventCarriesCorrectData()
    {
        var bus = new InMemoryEventBus();
        ArticlePublishedEvent? received = null;
        bus.Subscribe<ArticlePublishedEvent>(e => received = e);

        var article = Factory.MakeArticle(ArticleCategory.Business, "Budget 2025", "Claire Roy");
        var at = DateTimeOffset.UtcNow;
        bus.Publish(new ArticlePublishedEvent(article, at));

        Assert.NotNull(received);
        Assert.Equal("Budget 2025", received.Article.Title);
        Assert.Equal(ArticleCategory.Business, received.Article.Category);
        Assert.Equal("Claire Roy", received.Article.Author);
        Assert.Equal(at, received.OccurredAt);
    }
}

// ── Suite 2: InMemoryEventBus — Unsubscribe ───────────────────────────────────

public sealed class EventBus_Unsubscribe
{
    [Fact]
    public void UnsubscribedHandler_IsNotCalled()
    {
        var bus = new InMemoryEventBus();
        int count = 0;
        void Handler(ArticlePublishedEvent _) => count++;

        bus.Subscribe<ArticlePublishedEvent>(Handler);
        bus.Publish(new ArticlePublishedEvent(Factory.MakeArticle(), DateTimeOffset.UtcNow));
        bus.Unsubscribe<ArticlePublishedEvent>(Handler);
        bus.Publish(new ArticlePublishedEvent(Factory.MakeArticle(), DateTimeOffset.UtcNow));

        Assert.Equal(1, count);
    }

    [Fact]
    public void OtherHandlers_StillCalledAfterOneUnsubscribes()
    {
        var bus = new InMemoryEventBus();
        int countA = 0, countB = 0;
        void HandlerA(ArticlePublishedEvent _) => countA++;
        void HandlerB(ArticlePublishedEvent _) => countB++;

        bus.Subscribe<ArticlePublishedEvent>(HandlerA);
        bus.Subscribe<ArticlePublishedEvent>(HandlerB);
        bus.Unsubscribe<ArticlePublishedEvent>(HandlerA);

        bus.Publish(new ArticlePublishedEvent(Factory.MakeArticle(), DateTimeOffset.UtcNow));

        Assert.Equal(0, countA);
        Assert.Equal(1, countB);
    }

    [Fact]
    public void UnsubscribeNonRegisteredHandler_DoesNotThrow()
    {
        var bus = new InMemoryEventBus();
        void Handler(ArticlePublishedEvent _) { }

        var ex = Record.Exception(() => bus.Unsubscribe<ArticlePublishedEvent>(Handler));
        Assert.Null(ex);
    }
}

// ── Suite 3: InMemoryEventBus — Type filtering ────────────────────────────────

public sealed class EventBus_TypeFiltering
{
    [Fact]
    public void Handler_OnlyReceivesSubscribedEventType()
    {
        var bus = new InMemoryEventBus();
        int publishedCount = 0, alertCount = 0;

        bus.Subscribe<ArticlePublishedEvent>(_ => publishedCount++);
        bus.Subscribe<BreakingNewsAlertEvent>(_ => alertCount++);

        var article = Factory.MakeArticle();
        bus.Publish(new ArticlePublishedEvent(article, DateTimeOffset.UtcNow));

        Assert.Equal(1, publishedCount);
        Assert.Equal(0, alertCount);
    }

    [Fact]
    public void DifferentEventTypes_DispatchedIndependently()
    {
        var bus = new InMemoryEventBus();
        int published = 0, updated = 0, alerts = 0;

        bus.Subscribe<ArticlePublishedEvent>(_ => published++);
        bus.Subscribe<ArticleUpdatedEvent>(_ => updated++);
        bus.Subscribe<BreakingNewsAlertEvent>(_ => alerts++);

        var article = Factory.MakeArticle();
        bus.Publish(new ArticlePublishedEvent(article, DateTimeOffset.UtcNow));
        bus.Publish(new ArticleUpdatedEvent(article, article, "Fix typo", DateTimeOffset.UtcNow));
        bus.Publish(new BreakingNewsAlertEvent(article, "BREAKING", DateTimeOffset.UtcNow));

        Assert.Equal(1, published);
        Assert.Equal(1, updated);
        Assert.Equal(1, alerts);
    }
}

// ── Suite 4: EditorialService ─────────────────────────────────────────────────

public sealed class EditorialService_Tests
{
    [Fact]
    public void PublishArticle_PublishesArticlePublishedEvent()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        ArticlePublishedEvent? received = null;
        bus.Subscribe<ArticlePublishedEvent>(e => received = e);

        editorial.PublishArticle("Maple Leaf Foods Reports Record Q2", "Sales surged...", ArticleCategory.Business, "Sam Park");

        Assert.NotNull(received);
        Assert.Equal("Maple Leaf Foods Reports Record Q2", received.Article.Title);
    }

    [Fact]
    public void PublishBreakingNews_AlsoPublishesBreakingNewsAlertEvent()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        BreakingNewsAlertEvent? alert = null;
        bus.Subscribe<BreakingNewsAlertEvent>(e => alert = e);

        editorial.PublishArticle("Bank of Canada Cuts Rate", "Effective immediately...", ArticleCategory.BreakingNews, "Claire Tremblay");

        Assert.NotNull(alert);
        Assert.StartsWith("BREAKING:", alert.AlertHeadline);
        Assert.Contains("Bank of Canada Cuts Rate", alert.AlertHeadline);
    }

    [Fact]
    public void PublishNonBreakingNews_DoesNotPublishAlert()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        BreakingNewsAlertEvent? alert = null;
        bus.Subscribe<BreakingNewsAlertEvent>(e => alert = e);

        editorial.PublishArticle("Raptors Win", "Great game...", ArticleCategory.Sports, "James Beaumont");

        Assert.Null(alert);
    }

    [Fact]
    public void UpdateArticle_PublishesArticleUpdatedEvent()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        ArticleUpdatedEvent? updated = null;
        bus.Subscribe<ArticleUpdatedEvent>(e => updated = e);

        var article = editorial.PublishArticle("AI Fund Article", "Original body.", ArticleCategory.Technology, "Priya Sharma");
        editorial.UpdateArticle(article.Id, "Corrected body.", "Correction: Edmonton, not Vancouver");

        Assert.NotNull(updated);
        Assert.Equal("Original body.", updated.Original.Body);
        Assert.Equal("Corrected body.", updated.Updated.Body);
        Assert.Equal("Correction: Edmonton, not Vancouver", updated.ChangeReason);
    }

    [Fact]
    public void UpdateUnknownArticle_ThrowsKeyNotFoundException()
    {
        var (_, editorial) = Factory.MakeNewsroom();
        Assert.Throws<KeyNotFoundException>(() =>
            editorial.UpdateArticle(Guid.NewGuid(), "New body", "Reason"));
    }
}

// ── Suite 5: Subscriber services ─────────────────────────────────────────────

public sealed class SubscriberServices_Tests
{
    [Fact]
    public void EmailDigest_QueuesArticlesOnPublish()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        var digest = new EmailDigestService();
        bus.Subscribe<ArticlePublishedEvent>(digest.OnArticlePublished);

        editorial.PublishArticle("Article One", "Body", ArticleCategory.Sports, "Author");
        editorial.PublishArticle("Article Two", "Body", ArticleCategory.Technology, "Author");

        Assert.Equal(2, digest.Pending.Count);
        Assert.Equal("Article One", digest.Pending[0].Title);
    }

    [Fact]
    public void EmailDigest_FlushClears_AndReturnsBatch()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        var digest = new EmailDigestService();
        bus.Subscribe<ArticlePublishedEvent>(digest.OnArticlePublished);

        editorial.PublishArticle("Article One", "Body", ArticleCategory.Business, "Author");
        editorial.PublishArticle("Article Two", "Body", ArticleCategory.Business, "Author");

        var batch = digest.FlushDigest();

        Assert.Equal(2, batch.Count);
        Assert.Empty(digest.Pending);
    }

    [Fact]
    public void BreakingNewsAlert_TracksAlertsSent()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        var alerts = new BreakingNewsAlertService();
        bus.Subscribe<BreakingNewsAlertEvent>(alerts.OnBreakingNewsAlert);

        editorial.PublishArticle("Rate Cut", "Details...", ArticleCategory.BreakingNews, "Claire Tremblay");
        editorial.PublishArticle("Market Crash", "Details...", ArticleCategory.BreakingNews, "Sam Park");

        Assert.Equal(2, alerts.AlertsSent.Count);
        Assert.Contains("BREAKING: Rate Cut", alerts.AlertsSent[0]);
    }

    [Fact]
    public void Analytics_CountsPublishesByCategory()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        var analytics = new AnalyticsService();
        bus.Subscribe<ArticlePublishedEvent>(analytics.OnArticlePublished);

        editorial.PublishArticle("Tech 1", "Body", ArticleCategory.Technology, "Author");
        editorial.PublishArticle("Tech 2", "Body", ArticleCategory.Technology, "Author");
        editorial.PublishArticle("Sports 1", "Body", ArticleCategory.Sports, "Author");

        Assert.Equal(3, analytics.TotalPublished);
        Assert.Equal(2, analytics.PublishesByCategory[ArticleCategory.Technology]);
        Assert.Equal(1, analytics.PublishesByCategory[ArticleCategory.Sports]);
    }

    [Fact]
    public void Analytics_CountsUpdatesIndependently()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        var analytics = new AnalyticsService();
        bus.Subscribe<ArticlePublishedEvent>(analytics.OnArticlePublished);
        bus.Subscribe<ArticleUpdatedEvent>(analytics.OnArticleUpdated);

        var article = editorial.PublishArticle("Tech News", "Body", ArticleCategory.Technology, "Author");
        editorial.UpdateArticle(article.Id, "New body", "Correction");
        editorial.UpdateArticle(article.Id, "Final body", "Fact check");

        Assert.Equal(1, analytics.TotalPublished);
        Assert.Equal(2, analytics.TotalUpdated);
    }

    [Fact]
    public void Archive_StoresLatestVersionOnUpdate()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        var archive = new ContentArchiveService();
        bus.Subscribe<ArticlePublishedEvent>(archive.OnArticlePublished);
        bus.Subscribe<ArticleUpdatedEvent>(archive.OnArticleUpdated);

        var article = editorial.PublishArticle("Article", "V1 body", ArticleCategory.Business, "Author");
        editorial.UpdateArticle(article.Id, "V2 body", "Correction");

        Assert.Equal("V2 body", archive.Archive[article.Id].Body);
        Assert.Equal(2, archive.Log.Count);
    }

    [Fact]
    public void Archive_LogRecords_PublishedAndUpdatedEntries()
    {
        var (bus, editorial) = Factory.MakeNewsroom();
        var archive = new ContentArchiveService();
        bus.Subscribe<ArticlePublishedEvent>(archive.OnArticlePublished);
        bus.Subscribe<ArticleUpdatedEvent>(archive.OnArticleUpdated);

        var article = editorial.PublishArticle("Article", "Body", ArticleCategory.Entertainment, "Author");
        editorial.UpdateArticle(article.Id, "New body", "Fix spelling");

        Assert.Equal("Published", archive.Log[0].Event);
        Assert.StartsWith("Updated:", archive.Log[1].Event);
    }
}

// ── Suite 6: Integration ──────────────────────────────────────────────────────

public sealed class Integration_Tests
{
    [Fact]
    public void AllSubscribers_ReceiveEvents_InFullNewsroomFlow()
    {
        var bus = new InMemoryEventBus();
        var editorial = new EditorialService(bus);
        var digest = new EmailDigestService();
        var alerts = new BreakingNewsAlertService();
        var analytics = new AnalyticsService();
        var archive = new ContentArchiveService();

        bus.Subscribe<ArticlePublishedEvent>(digest.OnArticlePublished);
        bus.Subscribe<ArticlePublishedEvent>(analytics.OnArticlePublished);
        bus.Subscribe<ArticlePublishedEvent>(archive.OnArticlePublished);
        bus.Subscribe<BreakingNewsAlertEvent>(alerts.OnBreakingNewsAlert);
        bus.Subscribe<ArticleUpdatedEvent>(analytics.OnArticleUpdated);
        bus.Subscribe<ArticleUpdatedEvent>(archive.OnArticleUpdated);

        editorial.PublishArticle("TSX Dips", "Details...", ArticleCategory.Business, "Maya Okonkwo");
        editorial.PublishArticle("Bank of Canada Rate Hike", "Emergency...", ArticleCategory.BreakingNews, "Claire Tremblay");

        Assert.Equal(2, digest.Pending.Count);
        Assert.Single(alerts.AlertsSent);
        Assert.Equal(2, analytics.TotalPublished);
        Assert.Equal(2, archive.Archive.Count);
    }

    [Fact]
    public void ServiceNotSubscribedToEventType_DoesNotReceiveIt()
    {
        var bus = new InMemoryEventBus();
        var editorial = new EditorialService(bus);
        var alerts = new BreakingNewsAlertService();

        // alerts only listens to BreakingNewsAlertEvent
        bus.Subscribe<BreakingNewsAlertEvent>(alerts.OnBreakingNewsAlert);

        editorial.PublishArticle("Sports Story", "Body", ArticleCategory.Sports, "Author");

        Assert.Empty(alerts.AlertsSent);
    }

    [Fact]
    public void BreakingNewsPublish_FiresBothPublishedAndAlertEvents()
    {
        var bus = new InMemoryEventBus();
        var editorial = new EditorialService(bus);
        var digest = new EmailDigestService();
        var alerts = new BreakingNewsAlertService();

        bus.Subscribe<ArticlePublishedEvent>(digest.OnArticlePublished);
        bus.Subscribe<BreakingNewsAlertEvent>(alerts.OnBreakingNewsAlert);

        editorial.PublishArticle("Earthquake in BC", "A 5.2 magnitude earthquake...", ArticleCategory.BreakingNews, "Reporter");

        Assert.Single(digest.Pending);
        Assert.Single(alerts.AlertsSent);
    }
}
