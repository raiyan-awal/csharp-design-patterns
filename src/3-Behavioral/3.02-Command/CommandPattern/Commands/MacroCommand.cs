namespace CommandPattern;

public sealed class MacroCommand : ICommand
{
    private readonly IReadOnlyList<ICommand> _commands;

    public MacroCommand(IEnumerable<ICommand> commands) => _commands = [.. commands];

    // Execute all commands in forward order
    public void Execute()
    {
        foreach (var cmd in _commands)
            cmd.Execute();
    }

    // Undo all commands in reverse order so state unwinds correctly
    public void Undo()
    {
        foreach (var cmd in _commands.Reverse())
            cmd.Undo();
    }
}
