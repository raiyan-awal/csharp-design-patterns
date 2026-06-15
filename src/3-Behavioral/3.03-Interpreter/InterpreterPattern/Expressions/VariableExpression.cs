namespace InterpreterPattern;

// Terminal expression — looks up a named variable in the context
public sealed class VariableExpression(string name) : IExpression
{
    public bool Evaluate(Context context) => context.Lookup(name);
}
