namespace SagaPattern.Core;

public interface ISagaStep<TContext>
{
    string Name { get; }
    void Execute(TContext context);
    void Compensate(TContext context);
}
