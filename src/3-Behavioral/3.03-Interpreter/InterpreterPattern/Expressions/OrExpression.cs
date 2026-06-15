namespace InterpreterPattern;

// Non-terminal expression — true when EITHER operand is true
public sealed class OrExpression(IExpression left, IExpression right) : IExpression
{
    public bool Evaluate(Context context)
        => left.Evaluate(context) || right.Evaluate(context);
}
