namespace DecoratorPattern;

// ============================================================
// ABSTRACT BASE DECORATOR
// ============================================================
// Holds the inner INotifier and delegates to it by default.
// Concrete decorators override Send() to inject behaviour
// before, after, or around the inner call.
//
// Why an abstract base class and not just INotifier directly?
// Without it, every concrete decorator must duplicate the
// constructor and the default delegation — boilerplate that
// doesn't vary. The base class absorbs that repetition so
// concrete decorators only declare what changes.

public abstract class NotifierDecorator : INotifier
{
    protected readonly INotifier _inner;

    protected NotifierDecorator(INotifier inner)
        => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public virtual void Send(string recipient, string subject, string body)
        => _inner.Send(recipient, subject, body);
}
