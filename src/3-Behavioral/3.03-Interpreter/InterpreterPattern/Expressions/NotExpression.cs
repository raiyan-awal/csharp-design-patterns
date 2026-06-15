namespace InterpreterPattern;

// Non-terminal expression — inverts its operand
public sealed class NotExpression(IExpression operand) : IExpression
{
    public bool Evaluate(Context context) => !operand.Evaluate(context);
}
