using CommandPattern;

// ============================================================
// COMMAND PATTERN — DEMO
// ============================================================
// A text document editor where every edit is a command object.
// Commands can be executed, undone, redone, and grouped into
// macros — all without the document knowing about history.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static void PrintState(Document doc, CommandHistory history)
{
    Console.WriteLine($"  Document : \"{doc.Content}\"");
    Console.WriteLine($"  CanUndo  : {history.CanUndo}  (stack depth: {history.UndoCount})");
    Console.WriteLine($"  CanRedo  : {history.CanRedo}  (stack depth: {history.RedoCount})");
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       COMMAND PATTERN — Text Document Editor         ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Every edit is a Command object with Execute() and Undo().");
Console.WriteLine("CommandHistory manages undo/redo stacks; Document is the receiver.");
Console.WriteLine("MacroCommand groups multiple commands into one undoable unit.");

Pause();

// ── DEMO 1: Insert and undo ───────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Insert text, then undo");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var doc     = new Document();
var history = new CommandHistory();

Console.WriteLine("Initial state:");
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("Execute: Insert \"Hello\" at position 0");
history.Execute(new InsertTextCommand(doc, 0, "Hello"));
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("Execute: Insert \", world\" at position 5");
history.Execute(new InsertTextCommand(doc, 5, ", world"));
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("Execute: Insert \"!\" at position 12");
history.Execute(new InsertTextCommand(doc, 12, "!"));
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("Undo (removes \"!\"):");
history.Undo();
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("Undo (removes \", world\"):");
history.Undo();
PrintState(doc, history);

Pause();

// ── DEMO 2: Redo ──────────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Redo restores what was undone");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

Console.WriteLine("Current state (after two undos from Demo 1):");
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("Redo (re-inserts \", world\"):");
history.Redo();
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("Redo (re-inserts \"!\"):");
history.Redo();
PrintState(doc, history);

Pause();

// ── DEMO 3: New command clears redo stack ─────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — New command after undo clears the redo stack");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

Console.WriteLine("Undo (removes \"!\"):");
history.Undo();
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("CanRedo is true — redo stack has one entry.");
Console.WriteLine();
Console.WriteLine("Execute a NEW command instead: Insert \" :)\" at end");
history.Execute(new InsertTextCommand(doc, doc.Content.Length, " :)"));
PrintState(doc, history);

Console.WriteLine();
Console.WriteLine("CanRedo is now false — new command cleared the redo stack.");
Console.WriteLine("The \"!\" can no longer be redone; the branch is gone.");

Pause();

// ── DEMO 4: Delete and undo ───────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Delete captures the removed text for undo");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var doc2     = new Document("The quick brown fox");
var history2 = new CommandHistory();

Console.WriteLine("Initial document:");
PrintState(doc2, history2);

Console.WriteLine();
Console.WriteLine("Execute: Delete 10 characters at position 4 (removes \"quick \")");
history2.Execute(new DeleteTextCommand(doc2, 4, 6));
PrintState(doc2, history2);

Console.WriteLine();
Console.WriteLine("Undo (restores \"quick \"):");
history2.Undo();
PrintState(doc2, history2);

Pause();

// ── DEMO 5: MacroCommand ──────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — MacroCommand: wrap a word in ** as one undoable action");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var doc3     = new Document("Make this word bold");
var history3 = new CommandHistory();

Console.WriteLine("Initial document:");
PrintState(doc3, history3);

// "this" starts at position 5, length 4
// To bold: insert "**" before position 5, then insert "**" after position 7 (5+4+2 = offset by first insert)
var boldMacro = new MacroCommand(
[
    new InsertTextCommand(doc3, 5,  "**"),
    new InsertTextCommand(doc3, 11, "**"),
]);

Console.WriteLine();
Console.WriteLine("Execute MacroCommand: surround \"this\" with **");
history3.Execute(boldMacro);
PrintState(doc3, history3);

Console.WriteLine();
Console.WriteLine("One Undo removes BOTH ** markers (the whole macro is one undo step):");
history3.Undo();
PrintState(doc3, history3);

Console.WriteLine();
Console.WriteLine("Redo re-applies both at once:");
history3.Redo();
PrintState(doc3, history3);

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • ICommand (Execute + Undo) — encapsulates one reversible action");
Console.WriteLine("  • InsertTextCommand — stores position + text; Undo deletes same span");
Console.WriteLine("  • DeleteTextCommand — captures deleted text at Execute time for Undo");
Console.WriteLine("  • MacroCommand      — executes N commands; undoes them in reverse");
Console.WriteLine("  • CommandHistory    — undo/redo stacks; new command clears redo");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
