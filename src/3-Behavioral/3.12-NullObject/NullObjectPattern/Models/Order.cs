namespace NullObjectPattern;

public sealed class Order
{
    public string  OrderId      { get; }
    public string  CustomerName { get; }
    public string  Email        { get; }
    public string  Phone        { get; }
    public decimal Total        { get; }
    public string  Status       { get; private set; }

    public Order(string orderId, string customerName, string email, string phone, decimal total)
    {
        OrderId      = orderId;
        CustomerName = customerName;
        Email        = email;
        Phone        = phone;
        Total        = total;
        Status       = "Pending";
    }

    internal void UpdateStatus(string status) => Status = status;
}
