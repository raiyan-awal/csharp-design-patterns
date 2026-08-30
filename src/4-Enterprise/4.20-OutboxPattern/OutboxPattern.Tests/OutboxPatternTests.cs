using OutboxPattern.Core;
using OutboxPattern.Domain;
using OutboxPattern.Infrastructure;
using OutboxPattern.Services;

namespace OutboxPattern.Tests;

// ── Stubs ─────────────────────────────────────────────────────────────────────

file sealed class ThrowingOrderRepository : IOrderRepository
{
    public void                  Save(Order _)    => throw new InvalidOperationException("DB error.");
    public Order?                FindById(Guid _) => null;
    public IReadOnlyList<Order>  GetAll()         => [];
}

// ── Suite 1: OutboxMessage ────────────────────────────────────────────────────

public sealed class OutboxMessageTests
{
    [Fact]
    public void NewMessage_IsNotProcessed()
    {
        var msg = new OutboxMessage();

        Assert.False(msg.IsProcessed);
        Assert.Null(msg.ProcessedAtUtc);
    }

    [Fact]
    public void SettingProcessedAtUtc_SetsIsProcessed()
    {
        var msg = new OutboxMessage();
        msg.ProcessedAtUtc = DateTime.UtcNow;

        Assert.True(msg.IsProcessed);
    }

    [Fact]
    public void TwoNewMessages_HaveDifferentIds()
    {
        var a = new OutboxMessage();
        var b = new OutboxMessage();

        Assert.NotEqual(a.Id, b.Id);
    }
}

// ── Suite 2: InMemoryOutboxStore ──────────────────────────────────────────────

public sealed class InMemoryOutboxStoreTests
{
    [Fact]
    public void Add_MessageAppearsInGetUnprocessed()
    {
        var store = new InMemoryOutboxStore();
        var msg   = new OutboxMessage { EventType = "TestEvent" };

        store.Add(msg);

        Assert.Single(store.GetUnprocessed());
    }

    [Fact]
    public void MarkProcessed_RemovesFromGetUnprocessed()
    {
        var store = new InMemoryOutboxStore();
        var msg   = new OutboxMessage();
        store.Add(msg);

        store.MarkProcessed(msg.Id);

        Assert.Empty(store.GetUnprocessed());
    }

    [Fact]
    public void GetUnprocessed_ExcludesProcessedMessages()
    {
        var store = new InMemoryOutboxStore();
        var msg1  = new OutboxMessage { EventType = "A" };
        var msg2  = new OutboxMessage { EventType = "B" };
        store.Add(msg1);
        store.Add(msg2);
        store.MarkProcessed(msg1.Id);

        var unprocessed = store.GetUnprocessed();

        Assert.Single(unprocessed);
        Assert.Equal("B", unprocessed[0].EventType);
    }

    [Fact]
    public void GetUnprocessed_ReturnsAllUnprocessed_WhenMixed()
    {
        var store = new InMemoryOutboxStore();
        for (var i = 0; i < 5; i++) store.Add(new OutboxMessage());
        store.MarkProcessed(store.All[0].Id);
        store.MarkProcessed(store.All[2].Id);

        Assert.Equal(3, store.GetUnprocessed().Count);
    }

    [Fact]
    public void MarkProcessed_UnknownId_DoesNotThrow()
    {
        var store = new InMemoryOutboxStore();

        var ex = Record.Exception(() => store.MarkProcessed(Guid.NewGuid()));

        Assert.Null(ex);
    }
}

// ── Suite 3: OutboxRelay ──────────────────────────────────────────────────────

public sealed class OutboxRelayTests
{
    [Fact]
    public void ProcessPending_PublishesUnprocessedMessages()
    {
        var store     = new InMemoryOutboxStore();
        var published = new List<string>();
        store.Add(new OutboxMessage { EventType = "OrderPlaced" });

        var relay = new OutboxRelay(store, msg => published.Add(msg.EventType));
        relay.ProcessPending();

        Assert.Equal(["OrderPlaced"], published);
    }

    [Fact]
    public void ProcessPending_SkipsAlreadyProcessedMessages()
    {
        var store = new InMemoryOutboxStore();
        var msg   = new OutboxMessage { EventType = "OldEvent" };
        store.Add(msg);
        store.MarkProcessed(msg.Id);

        var published = new List<string>();
        var relay     = new OutboxRelay(store, m => published.Add(m.EventType));
        relay.ProcessPending();

        Assert.Empty(published);
    }

    [Fact]
    public void ProcessPending_MarksSuccessfulMessagesAsProcessed()
    {
        var store = new InMemoryOutboxStore();
        store.Add(new OutboxMessage());

        var relay = new OutboxRelay(store, _ => { });
        relay.ProcessPending();

        Assert.Empty(store.GetUnprocessed());
    }

    [Fact]
    public void ProcessPending_PublishFailure_LeavesMessageUnprocessed()
    {
        var store = new InMemoryOutboxStore();
        store.Add(new OutboxMessage { EventType = "OrderPlaced" });

        var relay = new OutboxRelay(store, _ => throw new InvalidOperationException("broker down"));
        relay.ProcessPending();

        Assert.Single(store.GetUnprocessed());
    }

    [Fact]
    public void ProcessPending_PublishFailure_ContinuesToProcessOtherMessages()
    {
        var store     = new InMemoryOutboxStore();
        var failMsg   = new OutboxMessage { EventType = "WillFail"  };
        var okMsg     = new OutboxMessage { EventType = "WillSucceed" };
        store.Add(failMsg);
        store.Add(okMsg);

        var callCount = 0;
        var relay = new OutboxRelay(store, _ =>
        {
            callCount++;
            if (callCount == 1) throw new InvalidOperationException("first publish fails");
        });

        var processed = relay.ProcessPending();

        Assert.Equal(1, processed);
        Assert.Single(store.GetUnprocessed());
        Assert.False(store.All.First(m => m.EventType == "WillFail").IsProcessed);
        Assert.True(store.All.First(m => m.EventType == "WillSucceed").IsProcessed);
    }
}

// ── Suite 4: OrderService ─────────────────────────────────────────────────────

public sealed class OrderServiceTests
{
    private static OrderItem[] DefaultItems() =>
        [new OrderItem("Maple Syrup", 1, 14.99m)];

    [Fact]
    public void PlaceOrder_SavesOrderToRepository()
    {
        var repo    = new InMemoryOrderRepository();
        var store   = new InMemoryOutboxStore();
        var service = new OrderService(repo, store);

        service.PlaceOrder("CUST-001", "Alice", DefaultItems());

        Assert.Equal(1, repo.Count);
    }

    [Fact]
    public void PlaceOrder_WritesOutboxMessage()
    {
        var repo    = new InMemoryOrderRepository();
        var store   = new InMemoryOutboxStore();
        var service = new OrderService(repo, store);

        service.PlaceOrder("CUST-001", "Alice", DefaultItems());

        Assert.Single(store.GetUnprocessed());
    }

    [Fact]
    public void PlaceOrder_OutboxMessage_HasOrderPlacedEventType()
    {
        var repo    = new InMemoryOrderRepository();
        var store   = new InMemoryOutboxStore();
        var service = new OrderService(repo, store);

        service.PlaceOrder("CUST-001", "Alice", DefaultItems());

        Assert.Equal("OrderPlaced", store.GetUnprocessed()[0].EventType);
    }

    [Fact]
    public void PlaceOrder_WhenRepositoryFails_NoOutboxEntryWritten()
    {
        var repo    = new ThrowingOrderRepository();
        var store   = new InMemoryOutboxStore();
        var service = new OrderService(repo, store);

        Assert.Throws<InvalidOperationException>(
            () => service.PlaceOrder("CUST-001", "Alice", DefaultItems()));

        Assert.Empty(store.GetUnprocessed());
    }
}

// ── Suite 5: Integration ──────────────────────────────────────────────────────

public sealed class IntegrationTests
{
    private static (InMemoryOrderRepository, InMemoryOutboxStore, OrderService, SimulatedEmailHandler, SimulatedInventoryHandler)
        BuildStack()
    {
        var repo      = new InMemoryOrderRepository();
        var store     = new InMemoryOutboxStore();
        var service   = new OrderService(repo, store);
        var email     = new SimulatedEmailHandler();
        var inventory = new SimulatedInventoryHandler();
        return (repo, store, service, email, inventory);
    }

    private static OutboxRelay BuildRelay(InMemoryOutboxStore store, SimulatedEmailHandler email, SimulatedInventoryHandler inventory)
        => new(store, msg => { inventory.Handle(msg); email.Handle(msg); });

    [Fact]
    public void FullFlow_OrderPlaced_BothHandlersNotified()
    {
        var (_, store, service, email, inventory) = BuildStack();
        service.PlaceOrder("CUST-001", "Alice", [new OrderItem("Poutine Kit", 1, 22.49m)]);

        var relay = BuildRelay(store, email, inventory);
        relay.ProcessPending();

        Assert.Single(email.ReceivedEvents);
        Assert.Single(inventory.ReceivedEvents);
        Assert.Empty(store.GetUnprocessed());
    }

    [Fact]
    public void RelayRetry_FailedPublish_SucceedsOnSecondRun()
    {
        var (_, store, service, email, inventory) = BuildStack();
        service.PlaceOrder("CUST-002", "Ben", [new OrderItem("Hockey Stick", 1, 189.99m)]);

        inventory.FailOnNextCall();

        var relay = BuildRelay(store, email, inventory);
        var first = relay.ProcessPending();
        Assert.Equal(0, first);
        Assert.Single(store.GetUnprocessed());

        var second = relay.ProcessPending();
        Assert.Equal(1, second);
        Assert.Empty(store.GetUnprocessed());
        Assert.Single(email.ReceivedEvents);
        Assert.Single(inventory.ReceivedEvents);
    }

    [Fact]
    public void MultipleOrders_AllProcessedInSingleRelayRun()
    {
        var (_, store, service, email, inventory) = BuildStack();
        service.PlaceOrder("CUST-003", "Sophie", [new OrderItem("Toque", 2, 24.99m)]);
        service.PlaceOrder("CUST-004", "Marcus", [new OrderItem("Ice Skates", 1, 149.99m)]);
        service.PlaceOrder("CUST-005", "Liu",    [new OrderItem("Flannel Shirt", 1, 59.99m)]);

        var relay     = BuildRelay(store, email, inventory);
        var processed = relay.ProcessPending();

        Assert.Equal(3, processed);
        Assert.Empty(store.GetUnprocessed());
        Assert.Equal(3, email.ReceivedEvents.Count);
        Assert.Equal(3, inventory.ReceivedEvents.Count);
    }
}
