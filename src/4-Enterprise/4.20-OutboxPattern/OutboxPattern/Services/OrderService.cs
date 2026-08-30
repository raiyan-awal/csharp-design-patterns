using OutboxPattern.Core;
using OutboxPattern.Domain;
using OutboxPattern.Infrastructure;

namespace OutboxPattern.Services;

public sealed class OrderService(IOrderRepository orderRepo, IOutboxStore outboxStore)
{
    public Order PlaceOrder(string customerId, string customerName, IEnumerable<OrderItem> items)
    {
        var order = new Order
        {
            CustomerId   = customerId,
            CustomerName = customerName,
            Items        = [.. items],
        };

        // In a real application, both of these writes happen inside a single database transaction.
        // If the transaction rolls back (e.g. a constraint violation), both the order row and the
        // outbox row disappear together — no orphaned events, no missed events.
        orderRepo.Save(order);
        outboxStore.Add(new OutboxMessage
        {
            EventType = "OrderPlaced",
            Payload   = $"{{\"orderId\":\"{order.Id}\",\"customerId\":\"{order.CustomerId}\"," +
                        $"\"customerName\":\"{order.CustomerName}\",\"totalCAD\":{order.TotalCAD}}}",
        });

        return order;
    }
}
