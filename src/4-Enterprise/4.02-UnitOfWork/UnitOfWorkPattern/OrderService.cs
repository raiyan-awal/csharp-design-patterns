namespace UnitOfWorkPattern;

public sealed record CartLine(int ProductId, int Quantity);

// Business logic depends only on IUnitOfWork — it has no idea whether
// changes are staged in memory or inside a real SQL transaction.
public static class OrderService
{
    public static async Task<Order> PlaceOrderAsync(IUnitOfWork uow, string customerName, IEnumerable<CartLine> cart)
    {
        var order = new Order { CustomerName = customerName, OrderDate = DateTime.UtcNow };

        foreach (var line in cart)
        {
            var product = await uow.Products.GetByIdAsync(line.ProductId)
                ?? throw new InvalidOperationException($"Product #{line.ProductId} not found");

            if (product.StockQuantity < line.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for {product.Name}: requested {line.Quantity}, available {product.StockQuantity}");

            product.StockQuantity -= line.Quantity;
            await uow.Products.UpdateAsync(product);

            order.Items.Add(new OrderItem
            {
                ProductId   = product.Id,
                ProductName = product.Name,
                Quantity    = line.Quantity,
                UnitPrice   = product.Price,
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.LineTotal);
        await uow.Orders.AddAsync(order);

        // Every Products.UpdateAsync and Orders.AddAsync call above only staged a change —
        // nothing is durable until this single line runs.
        await uow.CommitAsync();
        return order;
    }
}
