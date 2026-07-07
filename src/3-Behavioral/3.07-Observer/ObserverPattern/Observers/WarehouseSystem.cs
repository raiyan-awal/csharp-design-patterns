namespace ObserverPattern;

public sealed class WarehouseSystem : IOrderObserver
{
    public bool IsPickingOrder     { get; private set; }
    public bool IsInventoryUpdated { get; private set; }
    public int  NotificationCount  { get; private set; }

    public void OnOrderUpdated(Order order, OrderStatus previousStatus)
    {
        NotificationCount++;

        switch (order.Status)
        {
            case OrderStatus.Processing:
                IsPickingOrder = true;
                Console.WriteLine($"  [Warehouse] Picking and packing order #{order.OrderId}.");
                break;
            case OrderStatus.Shipped:
                IsPickingOrder = false;
                Console.WriteLine($"  [Warehouse] Order #{order.OrderId} dispatched to carrier.");
                break;
            case OrderStatus.Delivered:
                IsInventoryUpdated = true;
                Console.WriteLine($"  [Warehouse] Inventory records updated for order #{order.OrderId}.");
                break;
            case OrderStatus.Cancelled:
                IsPickingOrder = false;
                Console.WriteLine($"  [Warehouse] Order #{order.OrderId} cancelled — items returned to shelf.");
                break;
        }
    }
}
