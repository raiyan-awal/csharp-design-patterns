# 3.02 Command Pattern

## Intent

Encapsulate a request as an object. This lets you parameterise operations, queue or log them, and support undoable operations — without the invoker knowing what the operation actually does.

---

## The Problem It Solves

Without Command, an editor must know how to perform and reverse every possible action:

```csharp
// Without Command — editor handles everything directly
class Editor
{
    void InsertText(int pos, string text) { ... }
    void UndoLastInsert(int pos, int len) { ... }  // caller must track what to undo
    void DeleteText(int pos, int len)     { ... }
    void UndoLastDelete(int pos, string text) { ... }  // more caller state
}
// Adding a new action type means adding more methods and more undo tracking here
```

With Command, every action is a self-contained object that knows how to execute and undo itself:

```csharp
ICommand cmd = new InsertTextCommand(doc, 5, " world");
history.Execute(cmd);   // runs it and pushes to undo stack
history.Undo();         // calls cmd.Undo() — no extra caller state needed
```

---

## Domain: Text Document Editor

| Role | Class | Description |
|------|-------|-------------|
| **Command interface** | `ICommand` | `Execute()` and `Undo()` |
| **Receiver** | `Document` | Holds `Content`; exposes `Insert` and `Delete` |
| **Concrete command** | `InsertTextCommand` | Inserts text; `Undo` deletes the same span |
| **Concrete command** | `DeleteTextCommand` | Captures deleted text at `Execute` time; `Undo` re-inserts it |
| **Composite command** | `MacroCommand` | Executes N commands in order; undoes them in reverse |
| **Invoker** | `CommandHistory` | Manages undo/redo stacks; calls `Execute`/`Undo`/`Redo` |

---

## Structure

```
CommandHistory (invoker)
  │
  │  history.Execute(cmd) ──▶ cmd.Execute() ──▶ Document (receiver)
  │  history.Undo()       ──▶ cmd.Undo()   ──▶ Document
  │
  ├── _undoStack: Stack<ICommand>
  └── _redoStack: Stack<ICommand>

ICommand
  ├── InsertTextCommand  (pos, text)
  ├── DeleteTextCommand  (pos, len) — captures deleted text on Execute
  └── MacroCommand       (list of ICommand)
```

---

## How Undo/Redo Works

```
Execute "A"  → undoStack: [A]       redoStack: []
Execute "B"  → undoStack: [A, B]    redoStack: []
Execute "C"  → undoStack: [A, B, C] redoStack: []

Undo         → undoStack: [A, B]    redoStack: [C]   — C.Undo() called
Undo         → undoStack: [A]       redoStack: [C, B] — B.Undo() called

Redo         → undoStack: [A, B]    redoStack: [C]   — B.Execute() called again
```

A new `Execute` after an undo **clears the redo stack** — the undone branch is gone:

```
...after two undos: undoStack: [A], redoStack: [C, B]

Execute "D"  → undoStack: [A, D]    redoStack: []    — B and C are gone
```

---

## DeleteTextCommand: Capturing State for Undo

`InsertTextCommand` is self-contained — it already holds the text to insert back on undo. `DeleteTextCommand` does not know ahead of time what text will be at the position when `Execute` is called, so it captures it at that moment:

```csharp
public void Execute()
{
    _deletedText = _document.Content.Substring(_position, _length);  // capture
    _document.Delete(_position, _length);
}

public void Undo()
{
    _document.Insert(_position, _deletedText!);  // restore what was captured
}
```

This is why `Undo` must not be called before `Execute` — there is nothing captured yet.

---

## MacroCommand: Multiple Commands as One

`MacroCommand` wraps a list of commands and executes them as a single undoable unit:

```csharp
// Bold a word by inserting "**" before and after — one undo step
var boldMacro = new MacroCommand(
[
    new InsertTextCommand(doc, 5,  "**"),
    new InsertTextCommand(doc, 11, "**"),
]);

history.Execute(boldMacro);  // one entry on the undo stack
history.Undo();              // removes both "**" markers at once
```

Undo runs the sub-commands in **reverse order** to correctly unwind state — the last thing done must be the first thing undone.

---

## When to Use

- You need undoable/redoable operations
- You want to queue, schedule, or log operations
- You want to parameterise an object with an action to perform (e.g., button callbacks, menu items)

## When NOT to Use

- Simple one-shot operations with no need for undo or queuing — the indirection adds complexity with no benefit
- The number of command types is very small and fixed — a direct method call is clearer

---

## Benefits

- Undo/redo is contained in the command itself — the invoker stays simple
- Commands are composable into macros
- New command types can be added without changing the invoker or receiver

## Drawbacks

- Many small command classes — one per operation type
- `DeleteTextCommand` (and similar "capture-on-execute" commands) cannot be undone before they are executed

---

## Running the Demo

```bash
cd src/3-Behavioral/3.02-Command/CommandPattern
dotnet run
```

## Running Tests

```bash
cd src/3-Behavioral/3.02-Command/CommandPattern.Tests
dotnet test
```

---

## Related Patterns

- **Memento** — alternative approach to undo; saves a snapshot of the whole object's state instead of storing the reverse operation in the command
- **Chain of Responsibility** — also routes requests, but to one handler; Command encapsulates the request itself
- **Strategy** — also encapsulates an algorithm, but Strategy is about choosing between interchangeable behaviours, not about recording and reversing operations

---

### Command vs Chain of Responsibility

Both patterns deal with requests, but they answer different questions:

| | Command | Chain of Responsibility |
|---|---|---|
| **Core question** | *What* should happen? | *Who* should handle this? |
| **Request becomes** | An object stored and reused | A message passed along a chain |
| **Who handles** | A specific receiver baked into the command | Decided at runtime by the chain |
| **After handling** | Optionally reversed (Undo) | Chain stops; no reversal concept |
| **Key capability** | Undo, redo, queue, macro | Routing, escalation, filtering |

```csharp
// Command — "do this specific thing to this specific document"
ICommand cmd = new InsertTextCommand(doc, 5, " world");
history.Execute(cmd);   // can be undone, re-executed, queued
history.Undo();

// Chain of Responsibility — "someone in this chain should handle this ticket"
ITicketHandler chain = BuildChain();   // Tier1 → Tier2 → Tier3 → OnCall
chain.Handle(ticket);                  // routed to the right handler; no undo
```

They can also be combined: the request routed by a Chain of Responsibility can itself be a Command object, giving you both routing and undo capability.

---

### Command vs Memento (Undo Strategies)

Both can implement undo, but they take opposite approaches:

| | Command | Memento |
|---|---|---|
| **What is stored** | The reverse operation | A snapshot of object state |
| **Who knows how to undo** | The command itself | The originator (object being saved) |
| **Granularity** | Fine — each command undoes exactly its own change | Coarse — entire state snapshot per save point |
| **Memory cost** | Low — only the delta is stored | High — full state copy per snapshot |

```csharp
// Command approach — delta stored in the command
new DeleteTextCommand(doc, 5, 6);  // captures only the 6 deleted characters

// Memento approach — full snapshot
doc.Save();   // captures entire document content, however large
```

For a text editor, Command is almost always preferred — storing a 6-character delta is far cheaper than copying a 100 KB document for every keystroke.
