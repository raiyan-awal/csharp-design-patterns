namespace SagaPattern.Core;

public sealed class SagaOrchestrator<TContext>
{
    private readonly List<ISagaStep<TContext>> _steps;
    private readonly Action<string>?           _onExecuted;
    private readonly Action<string>?           _onCompensated;

    public SagaOrchestrator(
        IEnumerable<ISagaStep<TContext>> steps,
        Action<string>?                  onExecuted    = null,
        Action<string>?                  onCompensated = null)
    {
        _steps         = [.. steps];
        _onExecuted    = onExecuted;
        _onCompensated = onCompensated;
    }

    public SagaResult Execute(TContext context)
    {
        var executed = new Stack<ISagaStep<TContext>>();

        foreach (var step in _steps)
        {
            try
            {
                step.Execute(context);
                executed.Push(step);
                _onExecuted?.Invoke(step.Name);
            }
            catch (Exception ex)
            {
                foreach (var done in executed)
                {
                    try
                    {
                        done.Compensate(context);
                        _onCompensated?.Invoke(done.Name);
                    }
                    catch { /* compensation is best-effort; log in production */ }
                }

                return SagaResult.Failure(step.Name, ex);
            }
        }

        return SagaResult.Success();
    }
}
