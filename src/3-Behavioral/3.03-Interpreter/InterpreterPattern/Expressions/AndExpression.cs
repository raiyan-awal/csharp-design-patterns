namespace InterpreterPattern;

// Non-terminal expression — true only when BOTH operands are true
public sealed class AndExpression(IExpression left, IExpression right) : IExpression
{
    public bool Evaluate(Context context)
        => left.Evaluate(context) && right.Evaluate(context);
}
