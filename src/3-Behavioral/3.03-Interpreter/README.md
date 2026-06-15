# 3.03 Interpreter Pattern

## Intent

Define a grammar for a language and provide an interpreter to evaluate sentences in that language. Each rule in the grammar maps to one class; rules compose into a tree that is walked to produce a result.

---

## The Problem It Solves

Without Interpreter, complex conditional logic lives in one place and must be hardcoded:

```csharp
// Without Interpreter — every rule is a new if/else
bool CanModerate(User user)
    => (user.IsAdmin || user.IsModerator) && !user.IsBanned;

bool CanAccessFeature(User user)
    => user.IsPremium || (user.IsTrialUser && !user.TrialExpired);
// Adding a new rule means adding a new method; rules cannot be composed or stored
```

With Interpreter, rules are first-class objects built from reusable pieces:

```csharp
// Rule is data — built once, evaluated against any context
IExpression moderationRule = new AndExpression(
    new OrExpression(new VariableExpression("isAdmin"),
                     new VariableExpression("isModerator")),
    new NotExpression(new VariableExpression("isBanned")));

bool result = moderationRule.Evaluate(userContext);
// Same tree evaluated for every user — no new method needed per rule
```

---

## Grammar

```
Expression ::= Literal | Variable | And | Or | Not
Literal    ::= true | false
Variable   ::= identifier         (looked up in Context at evaluation time)
And        ::= Expression AND Expression
Or         ::= Expression OR  Expression
Not        ::= NOT Expression
```

---

## Domain: Boolean Access Control Rules

| Role | Class | Description |
|------|-------|-------------|
| **Abstract expression** | `IExpression` | Single method: `bool Evaluate(Context)` |
| **Terminal** | `LiteralExpression` | Always returns `true` or `false` — no context needed |
| **Terminal** | `VariableExpression` | Looks up a named variable in the context |
| **Non-terminal** | `AndExpression` | True only when both operands are true |
| **Non-terminal** | `OrExpression` | True when either operand is true |
| **Non-terminal** | `NotExpression` | Inverts its single operand |
| **Context** | `Context` | `Dictionary<string, bool>` — variable bindings per evaluation |

---

## Expression Tree

The rule `(isAdmin OR isModerator) AND NOT isBanned` becomes:

```
         AndExpression
        /             \
   OrExpression     NotExpression
   /         \           \
Var(isAdmin) Var(isModerator) Var(isBanned)
```

`Evaluate` is called on the root and recurses down the tree. Each node calls `Evaluate` on its children and combines the results according to its rule.

---

## Terminal vs Non-terminal Expressions

**Terminal expressions** are the leaves of the tree — they produce a value without delegating to children:

```csharp
// LiteralExpression — always the same value, ignores context
public bool Evaluate(Context context) => value;

// VariableExpression — asks context for the current value of name
public bool Evaluate(Context context) => context.Lookup(name);
```

**Non-terminal expressions** are the internal nodes — they evaluate their children and combine the results:

```csharp
// AndExpression — short-circuits: right side not evaluated if left is false
public bool Evaluate(Context context)
    => left.Evaluate(context) && right.Evaluate(context);
```

---

## The Context

The `Context` holds the variable bindings for one evaluation. The same rule tree is re-used — only the context changes per user or per request:

```csharp
// Build the rule once
IExpression rule = new AndExpression(
    new VariableExpression("isLoggedIn"),
    new VariableExpression("hasPermission"));

// Evaluate for different users by providing different contexts
rule.Evaluate(new Context(new Dictionary<string, bool>
    { ["isLoggedIn"] = true, ["hasPermission"] = true }));   // true

rule.Evaluate(new Context(new Dictionary<string, bool>
    { ["isLoggedIn"] = true, ["hasPermission"] = false }));  // false
```

---

## When to Use

- The grammar is small and stable — you can afford one class per rule
- You need to evaluate the same expression against many different inputs at runtime
- Rules need to be composed dynamically (e.g., loaded from a database or config file)

## When NOT to Use

- The grammar is large or changes frequently — the class-per-rule approach becomes unmanageable; use a parser generator (ANTLR, Roslyn) instead
- Performance is critical — recursive tree traversal is slower than compiled code
- The "language" is just a few fixed conditions — hardcoded if/else is simpler and clearer

---

## Benefits

- Each grammar rule is in one class — easy to add a new operator
- Rules are composable — complex expressions are built from simple pieces
- The same rule tree can be evaluated against any context without rebuilding

## Drawbacks

- One class per grammar rule — quickly becomes many classes for large grammars
- The tree must be built in code (or parsed from a string separately — parsing is not part of this pattern)
- Deeply nested trees can be hard to debug

---

## Running the Demo

```bash
cd src/3-Behavioral/3.03-Interpreter/InterpreterPattern
dotnet run
```

## Running Tests

```bash
cd src/3-Behavioral/3.03-Interpreter/InterpreterPattern.Tests
dotnet test
```

---

## Related Patterns

- **Composite** — the expression tree is a Composite structure; the Interpreter pattern is Composite applied to grammar rules
- **Visitor** — an alternative for evaluating an expression tree; instead of `Evaluate` on each node, a Visitor traverses the tree and applies operations externally
- **Flyweight** — terminal expressions (`LiteralExpression(true)`, `LiteralExpression(false)`) are natural candidates for sharing since they carry no mutable state

---

### Interpreter vs Composite

The expression tree *is* a Composite structure — `IExpression` is the component, terminal expressions are leaves, and non-terminals are composites. The difference is intent:

| | Composite | Interpreter |
|---|---|---|
| **Intent** | Treat individual objects and compositions uniformly | Evaluate sentences in a grammar |
| **Operation** | Generic tree operations (size, draw, render) | Language-specific `Evaluate` against a context |
| **Children** | N children (zero to many) | Fixed arity per rule (And=2, Not=1, Literal=0) |

Composite tells you *how* to build the tree. Interpreter tells you *what* the tree means.
