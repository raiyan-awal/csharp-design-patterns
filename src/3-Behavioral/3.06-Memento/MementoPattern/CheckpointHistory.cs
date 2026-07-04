namespace MementoPattern;

// The Caretaker. Holds mementos on a stack — never reads their internals.
public sealed class CheckpointHistory
{
    private readonly Stack<CharacterMemento> _stack = new();

    public bool CanUndo => _stack.Count > 0;
    public int  Count   => _stack.Count;

    public void             Push(CharacterMemento memento) => _stack.Push(memento);
    public CharacterMemento Pop()                          => _stack.Pop();
    public CharacterMemento Peek()                         => _stack.Peek();

    public IReadOnlyList<CharacterMemento> All => [.. _stack];
}
