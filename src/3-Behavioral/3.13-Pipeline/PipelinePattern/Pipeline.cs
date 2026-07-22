namespace PipelinePattern;

// Generic pipeline runner. Steps are registered via fluent AddStep calls and
// executed in order. The first step that returns false stops the chain.
public sealed class Pipeline<TContext>
{
    private readonly List<IPipelineStep<TContext>> _steps = [];

    public Pipeline<TContext> AddStep(IPipelineStep<TContext> step)
    {
        _steps.Add(step);
        return this;
    }

    public TContext Run(TContext context)
    {
        foreach (var step in _steps)
        {
            Console.WriteLine($"    [{step.Name}]");
            var shouldContinue = step.Execute(context);
            if (!shouldContinue) break;
        }
        return context;
    }
}
