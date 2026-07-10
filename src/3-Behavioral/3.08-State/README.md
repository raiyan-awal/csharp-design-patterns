# 3.08 — State Pattern

## Intent

Allow an object to alter its behaviour when its internal state changes. The object will appear to change its class — the same method call produces different results depending on which state is active.

---

## The Problem It Solves

A vending machine has radically different behaviour at each stage of a transaction. Without the State pattern, every method becomes a wall of conditionals:

```csharp
// WITHOUT State — all logic in one class, every method checks _state:
public void InsertMoney(decimal amount)
{
    if (_state == "idle")         { _balance += amount; _state = "hasMoney"; }
    else if (_state == "hasMoney") { _balance += amount; }
    else if (_state == "outOfStock") { Console.WriteLine("Machine out of stock."); }
    else if (_state == "dispensing")  { Console.WriteLine("Wait..."); }
}

public void SelectProduct(string code)
{
    if (_state == "idle")         { Console.WriteLine("Insert money first."); }
    else if (_state == "hasMoney") { /* lots of logic */ }
    else if (_state == "outOfStock") { Console.WriteLine("Out of stock."); }
    // ...
}
```

Adding a new state means editing every method. The State pattern moves each state's behaviour into its own class, so each state only knows what it should do.

---

## Solution: Vending Machine

Four states capture the machine's lifecycle. Transitioning between them is the responsibility of the states themselves — the `VendingMachine` context just delegates.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **State interface** | `IVendingState` | `InsertMoney`, `SelectProduct`, `ReturnMoney`, `Dispense` |
| **Concrete States** | `IdleState`, `HasMoneyState`, `DispensingState`, `OutOfStockState` | Implement each operation for that specific state |
| **Context** | `VendingMachine` | Holds current state; delegates all operations to it |

### State transitions

```
        ┌──InsertMoney (top-up)──┐
        │                        │
        ▼                        │
Idle ──InsertMoney──► HasMoney ──┘
  ▲                      │  \
  │                 ReturnMoney \──SelectProduct──► Dispensing
  └──────────────────────┘                              │
                                        ┌───────────────┴───────────────┐
                                   (stock > 0)                     (stock == 0)
                                        │                               │
                                        ▼                               ▼
                                      Idle                        OutOfStock
                                        ▲                               │
                                        └──────────RestockProduct───────┘
```

---

## Structure

```
StatePattern/
├── IVendingState.cs       ← State interface
├── Product.cs             ← record: Code, Name, Price, Stock
├── VendingMachine.cs      ← Context: delegates to current IVendingState
└── States/
    ├── IdleState.cs       ← Waiting for money
    ├── HasMoneyState.cs   ← Money inserted; awaiting product selection
    ├── DispensingState.cs ← Product selected; dispensing in progress
    └── OutOfStockState.cs ← No products available
```

---

## Key Code

### Context — delegates to current state

```csharp
public sealed class VendingMachine
{
    private IVendingState _state = IdleState.Instance;

    public void TransitionTo(IVendingState newState)
    {
        Console.WriteLine($"  [State] {_state.Name} → {newState.Name}");
        _state = newState;
    }

    // Every operation is a simple delegation — no conditionals here
    public void InsertMoney(decimal amount) => _state.InsertMoney(this, amount);
    public void SelectProduct(string code)  => _state.SelectProduct(this, code);
    public void ReturnMoney()               => _state.ReturnMoney(this);
    public void Dispense()                  => _state.Dispense(this);
}
```

### State — each class handles only its own behaviour

```csharp
// IdleState: only InsertMoney makes sense; everything else is rejected
public sealed class IdleState : IVendingState
{
    public static readonly IdleState Instance = new();
    private IdleState() { }

    public string Name => "Idle";

    public void InsertMoney(VendingMachine machine, decimal amount)
    {
        machine.Balance += amount;
        machine.TransitionTo(HasMoneyState.Instance);
    }

    public void SelectProduct(VendingMachine machine, string code)
        => Console.WriteLine("Please insert money before selecting a product.");

    public void ReturnMoney(VendingMachine machine)
        => Console.WriteLine("No money to return.");

    public void Dispense(VendingMachine machine)
        => Console.WriteLine("Please insert money and select a product first.");
}
```

```csharp
// DispensingState: drives the dispense operation and decides next state
public sealed class DispensingState : IVendingState
{
    public void Dispense(VendingMachine machine)
    {
        var product = machine.Inventory[machine.SelectedProduct!];
        var change  = machine.Balance - product.Price;

        machine.UpdateStock(product.Code, product.Stock - 1);
        machine.Balance = 0;
        machine.SelectedProduct = null;

        Console.WriteLine($"  Dispensing: {product.Name}");
        if (change > 0) Console.WriteLine($"  Change returned: ${change:F2}");

        machine.TransitionTo(
            machine.HasAnyStock() ? IdleState.Instance : OutOfStockState.Instance);
    }
    // ... other operations reject with "please wait"
}
```

### Singleton states

Each concrete state is stateless (all data lives on `VendingMachine`), so every state is implemented as a singleton with a `private` constructor:

```csharp
public static readonly IdleState Instance = new();
private IdleState() { }
```

This avoids allocating a new state object on every transition.

---

## Usage

```csharp
var machine = new VendingMachine();
machine.AddProduct("A1", "Water", 1.00m, stock: 3);
machine.AddProduct("A2", "Cola",  1.50m, stock: 2);

machine.InsertMoney(2.00m);       // Idle → HasMoney
machine.SelectProduct("A2");      // HasMoney → Dispensing → Idle  ($0.50 change returned)

machine.ReturnMoney();            // Would cancel if in HasMoney; no-op from Idle
```

---

## Demo Scenarios

```
DEMO 1 — Happy path: exact change
DEMO 2 — Overpay: change returned
DEMO 3 — Insufficient funds: top up then purchase
DEMO 4 — Cancel purchase: money returned
DEMO 5 — Invalid operations: state guards reject bad calls
DEMO 6 — Out of stock → restock → purchase again
DEMO 7 — Change returned automatically when dispensing empties the machine
```

---

## When to Use

- An object's behaviour depends on its state and must change at runtime
- Methods contain large multi-branch conditionals that switch on an internal status field
- State-specific logic would otherwise scatter across the codebase

---

## When NOT to Use

- Behaviour rarely changes — direct conditionals are simpler
- Only two states — a simple boolean flag may be enough
- The state machine has many states with sparse transitions — a table-driven approach scales better

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Open/Closed** | Add a new state by adding a new class — no changes to existing states |
| **Single Responsibility** | Each state class handles only its own logic |
| **Eliminates conditionals** | Context delegates instead of switching on `_state` |
| **Explicit transitions** | Moving between states is a deliberate, traceable action |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Class proliferation** | Many states → many small classes |
| **Scattered transitions** | Each state can call `TransitionTo`; the full state machine isn't in one place |
| **Coupling** | Concrete states must know about other concrete states to transition to them |

---

## State vs Strategy

Both delegate behaviour through an interface, but the intent differs:

| | State | Strategy |
|---|-------|---------|
| **Transitions** | States trigger their own transitions | Client swaps strategies explicitly |
| **Coupling** | Concrete states often know each other | Strategies are independent |
| **Object lifecycle** | State changes behaviour of one object over time | Strategy selects an algorithm for one operation |
| **Example** | Vending machine, TCP connection, order workflow | Sorting algorithm, payment method, compression format |

---

## Related Patterns

- **Strategy (3.09)** — similar delegation; differs in whether the object or the client drives changes
- **Singleton** — each concrete state is a singleton (stateless, reusable)
- **Flyweight (2.6)** — stateless shared objects; states as singletons follow the same principle
- **Command (3.02)** — state transitions can be recorded as commands for undo/replay

---

## Running the Demo

```bash
cd src/3-Behavioral/3.08-State/StatePattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.08-State/StatePattern.Tests
dotnet test
```
