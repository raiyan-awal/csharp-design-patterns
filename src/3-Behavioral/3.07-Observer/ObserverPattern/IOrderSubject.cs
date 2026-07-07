namespace ObserverPattern;

public interface IOrderSubject
{
    void Subscribe(IOrderObserver observer);
    void Unsubscribe(IOrderObserver observer);
}
