namespace InterpreterPattern;

// Grammar:
//   Expression ::= Literal | Variable | And | Or | Not
//   Literal    ::= true | false
//   Variable   ::= identifier (looked up in Context)
//   And        ::= Expression AND Expression
//   Or         ::= Expression OR  Expression
//   Not        ::= NOT Expression

public interface IExpression
{
    bool Evaluate(Context context);
}
