namespace InterpreterPattern;

// Terminal expression — always returns the same constant value
public sealed class LiteralExpression(bool value) : IExpression
{
    public bool Evaluate(Context context) => value;
}
