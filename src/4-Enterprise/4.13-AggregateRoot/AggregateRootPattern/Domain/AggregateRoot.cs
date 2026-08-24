namespace AggregateRootPattern.Domain;

public abstract class AggregateRoot
{
    public int Id      { get; protected set; }
    public int Version { get; private set; }

    protected void IncrementVersion() => Version++;
}
