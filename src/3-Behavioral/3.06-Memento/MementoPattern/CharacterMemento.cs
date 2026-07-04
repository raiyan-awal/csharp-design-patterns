namespace MementoPattern;

// The Memento. Stores a frozen snapshot of GameCharacter's state.
// Constructor is internal — only GameCharacter (same assembly) can create snapshots.
// The Caretaker holds mementos but must treat them as opaque tokens.
public sealed class CharacterMemento
{
    internal CharacterMemento(
        int health, int mana, Position position, int level,
        IReadOnlyList<string> inventory, string label, DateTime savedAt)
    {
        Health    = health;
        Mana      = mana;
        Position  = position;
        Level     = level;
        Inventory = inventory;
        Label     = label;
        SavedAt   = savedAt;
    }

    public int                  Health    { get; }
    public int                  Mana      { get; }
    public Position             Position  { get; }
    public int                  Level     { get; }
    public IReadOnlyList<string> Inventory { get; }
    public string               Label     { get; }
    public DateTime             SavedAt   { get; }

    public override string ToString()
        => $"[{Label}] Lv{Level} HP:{Health} MP:{Mana} @ {Position}  ({Inventory.Count} items)  {SavedAt:HH:mm:ss}";
}
