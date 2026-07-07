namespace ObserverPattern;

public interface IOrderObserver
{
    void OnOrderUpdated(Order order, OrderStatus previousStatus);
}
