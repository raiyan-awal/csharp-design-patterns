namespace ObserverPattern;

public sealed class InvoiceService : IOrderObserver
{
    public bool     InvoiceGenerated { get; private set; }
    public decimal? InvoiceAmount    { get; private set; }

    public void OnOrderUpdated(Order order, OrderStatus previousStatus)
    {
        if (order.Status == OrderStatus.Delivered)
        {
            InvoiceGenerated = true;
            InvoiceAmount    = order.TotalAmount;
            Console.WriteLine($"  [InvoiceService] Invoice generated for order #{order.OrderId} — £{order.TotalAmount:F2}");
        }
    }
}
