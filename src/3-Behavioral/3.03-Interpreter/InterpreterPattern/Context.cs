namespace InterpreterPattern;

public sealed class Context
{
    private readonly Dictionary<string, bool> _variables;

    public Context(Dictionary<string, bool>? variables = null)
        => _variables = variables is not null ? new(variables) : [];

    public bool Lookup(string name)
        => _variables.TryGetValue(name, out var value)
            ? value
            : throw new KeyNotFoundException($"Variable '{name}' is not defined in context.");

    public void Set(string name, bool value) => _variables[name] = value;
}
