using MementoPattern;

// ============================================================
// MEMENTO PATTERN — DEMO
// ============================================================
// A game character saves checkpoints before dangerous encounters.
// If the character dies, the player restores the last checkpoint.
// GameCharacter (Originator) creates and consumes mementos.
// CheckpointHistory (Caretaker) holds the stack without looking inside.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static void Divider(string title)
{
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine(title);
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();
}

var hero    = new GameCharacter("Aria");
var history = new CheckpointHistory();

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       MEMENTO PATTERN — Game Checkpoint System       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Roles:");
Console.WriteLine("  • GameCharacter     → Originator  (creates & restores snapshots)");
Console.WriteLine("  • CharacterMemento  → Memento     (frozen state, opaque to Caretaker)");
Console.WriteLine("  • CheckpointHistory → Caretaker   (holds the stack, never reads it)");

Pause();

// ── DEMO 1: Basic save / restore ──────────────────────────────
Divider("DEMO 1 — Basic save and restore");

Console.WriteLine("Initial state:");
hero.PrintStatus();
Console.WriteLine();

Console.WriteLine("Saving checkpoint 'Town entrance'...");
history.Push(hero.Save("Town entrance"));
Console.WriteLine();

Console.WriteLine("Entering the dungeon — taking heavy damage:");
hero.MoveTo(5, 3);
hero.TakeDamage(60);
hero.UseMana(40);
Console.WriteLine();
Console.WriteLine("Current state:");
hero.PrintStatus();

Console.WriteLine();
Console.WriteLine("Restoring last checkpoint:");
hero.Restore(history.Pop());
Console.WriteLine();
Console.WriteLine("State after restore:");
hero.PrintStatus();

Pause();

// ── DEMO 2: Multiple checkpoints (save points through a dungeon) ──
Divider("DEMO 2 — Multiple checkpoints through a dungeon");

Console.WriteLine("Checkpoint 1 — Dungeon entrance:");
hero.MoveTo(10, 0);
hero.PickUp("Iron Sword");
hero.PickUp("Health Potion");
history.Push(hero.Save("Dungeon entrance"));
hero.PrintStatus();
Console.WriteLine();

Console.WriteLine("Checkpoint 2 — After first boss:");
hero.MoveTo(10, 8);
hero.TakeDamage(20);
hero.UseMana(30);
hero.LevelUp();
hero.PickUp("Boss Key");
history.Push(hero.Save("After first boss"));
hero.PrintStatus();
Console.WriteLine();

Console.WriteLine("Checkpoint 3 — Before final boss:");
hero.MoveTo(10, 15);
hero.PickUp("Ancient Relic");
history.Push(hero.Save("Before final boss"));
hero.PrintStatus();
Console.WriteLine();

Console.WriteLine($"Checkpoints saved: {history.Count}");
Console.WriteLine();
Console.WriteLine("Final boss kills the hero:");
hero.TakeDamage(100);
Console.WriteLine();

Console.WriteLine("Restoring 'Before final boss':");
hero.Restore(history.Pop());
hero.PrintStatus();

Pause();

// ── DEMO 3: Rolling back through multiple checkpoints ─────────
Divider("DEMO 3 — Rolling back through all checkpoints");

Console.WriteLine($"Checkpoints remaining: {history.Count}");
Console.WriteLine();

Console.WriteLine("Restore 'After first boss':");
hero.Restore(history.Pop());
hero.PrintStatus();
Console.WriteLine();

Console.WriteLine("Restore 'Dungeon entrance':");
hero.Restore(history.Pop());
hero.PrintStatus();
Console.WriteLine();

Console.WriteLine($"Checkpoints remaining: {history.Count}  (stack empty)");

Pause();

// ── DEMO 4: Deep copy — inventory is independent ──────────────
Divider("DEMO 4 — Deep copy: memento is independent of live state");

var fighter = new GameCharacter("Rex");
fighter.PickUp("Magic Shield");
fighter.PickUp("Fire Staff");

var snapshot = fighter.Save("before trade");
Console.WriteLine($"Snapshot inventory : [{string.Join(", ", snapshot.Inventory)}]");

fighter.Drop("Magic Shield");
fighter.PickUp("Rusty Dagger");
Console.WriteLine($"Live inventory now : [{string.Join(", ", fighter.Inventory)}]");
Console.WriteLine($"Snapshot unchanged : [{string.Join(", ", snapshot.Inventory)}]");
Console.WriteLine();
Console.WriteLine("Restoring to snapshot:");
fighter.Restore(snapshot);
fighter.PrintStatus();

Pause();

// ── DEMO 5: Checkpoint labels and metadata ────────────────────
Divider("DEMO 5 — Checkpoint labels");

var explorer = new GameCharacter("Kira");
explorer.MoveTo(0, 0);   history.Push(explorer.Save("Start"));
explorer.MoveTo(3, 0);   history.Push(explorer.Save("Crossroads"));
explorer.MoveTo(3, 5);   history.Push(explorer.Save("Cave entrance"));
explorer.MoveTo(3, 10);  history.Push(explorer.Save("Deep cave"));

Console.WriteLine("All saved checkpoints (newest first):");
foreach (var cp in history.All)
    Console.WriteLine($"  {cp}");

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • GameCharacter.Save()     — creates a deep-copy snapshot (CharacterMemento)");
Console.WriteLine("  • GameCharacter.Restore()  — overwrites live state from a snapshot");
Console.WriteLine("  • CharacterMemento         — immutable; internal constructor (Originator only)");
Console.WriteLine("  • CheckpointHistory        — holds the stack; never reads memento internals");
Console.WriteLine("  • Deep copy                — modifying live state after Save() doesn't touch the snapshot");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
