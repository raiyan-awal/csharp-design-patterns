namespace MementoPattern;

// The Originator. Creates mementos from its own state and restores from them.
public sealed class GameCharacter
{
    private readonly List<string> _inventory = [];

    public string              Name      { get; }
    public int                 Health    { get; private set; }
    public int                 Mana      { get; private set; }
    public Position            Position  { get; private set; }
    public int                 Level     { get; private set; }
    public IReadOnlyList<string> Inventory => _inventory;
    public bool                IsAlive   => Health > 0;

    public GameCharacter(string name, int health = 100, int mana = 100)
    {
        Name     = name;
        Health   = health;
        Mana     = mana;
        Level    = 1;
        Position = new Position(0, 0);
    }

    // ── Originator operations ─────────────────────────────────────────────────

    public CharacterMemento Save(string label = "")
        => new(Health, Mana, Position, Level, [.. _inventory], label, DateTime.Now);

    public void Restore(CharacterMemento memento)
    {
        Health   = memento.Health;
        Mana     = memento.Mana;
        Position = memento.Position;
        Level    = memento.Level;
        _inventory.Clear();
        _inventory.AddRange(memento.Inventory);
        Console.WriteLine($"  [{Name}] Restored to checkpoint \"{memento.Label}\" — " +
                          $"Lv{Level} HP:{Health} MP:{Mana} @ {Position}");
    }

    // ── Game actions ──────────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
        Console.WriteLine($"  [{Name}] Took {amount} damage  →  HP: {Health}{(IsAlive ? "" : " (DEAD)")}");
    }

    public void UseMana(int amount)
    {
        Mana = Math.Max(0, Mana - amount);
        Console.WriteLine($"  [{Name}] Used {amount} mana  →  MP: {Mana}");
    }

    public void Heal(int amount)
    {
        Health = Math.Min(100, Health + amount);
        Console.WriteLine($"  [{Name}] Healed {amount}  →  HP: {Health}");
    }

    public void MoveTo(int x, int y)
    {
        Position = new Position(x, y);
        Console.WriteLine($"  [{Name}] Moved to {Position}");
    }

    public void LevelUp()
    {
        Level++;
        Health = 100;
        Mana   = 100;
        Console.WriteLine($"  [{Name}] Level up! → Level {Level}  (HP and MP fully restored)");
    }

    public void PickUp(string item)
    {
        _inventory.Add(item);
        Console.WriteLine($"  [{Name}] Picked up: {item}");
    }

    public void Drop(string item)
    {
        _inventory.Remove(item);
        Console.WriteLine($"  [{Name}] Dropped: {item}");
    }

    public void PrintStatus()
    {
        Console.WriteLine($"  [{Name}] Lv:{Level}  HP:{Health}  MP:{Mana}  @ {Position}");
        Console.WriteLine($"  [{Name}] Inventory: [{string.Join(", ", _inventory)}]");
    }
}
