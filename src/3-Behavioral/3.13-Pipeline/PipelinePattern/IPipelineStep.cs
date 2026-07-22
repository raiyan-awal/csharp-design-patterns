namespace PipelinePattern;

// Each step receives the shared context, performs its work, and signals
// whether execution should continue (true) or stop (false).
// Returning false short-circuits the pipeline — remaining steps are skipped.
public interface IPipelineStep<TContext>
{
    string Name { get; }
    bool   Execute(TContext context);
}
