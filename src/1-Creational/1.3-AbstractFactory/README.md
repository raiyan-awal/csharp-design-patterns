# Abstract Factory Pattern

## 📖 Pattern Category
**Creational Pattern**

## 🎯 Intent
Provide an interface for creating **families of related objects** without specifying their concrete classes.

## 🤔 Problem
You're building a UI framework that supports multiple themes (Light, Dark, High Contrast). Every theme needs its own Button, Checkbox, Dialog, etc. — and they must be visually consistent with each other.

The problem is *family consistency*: you can't accidentally pair a Light-themed button with a Dark-themed checkbox. And you need to be able to swap the entire theme at once — not component by component.

If you use `new LightButton()` and `new DarkCheckbox()` directly, nothing stops mixing and the code is hardcoded to concrete types forever.

## ✅ Solution
Abstract Factory adds one layer above Factory Method:

1. Define an **abstract factory interface** (`IUIFactory`) with one creation method per product type
2. Each **concrete factory** (`LightThemeFactory`, `DarkThemeFactory`) implements the interface and produces only its own family
3. The **client** (`UIRenderer`) receives a factory at construction — it creates all components through the interface and never touches concrete types
4. Swapping the factory out re-creates the entire family consistently

## 🏗️ Structure

```
         «interface»
         IUIFactory
    ┌──────────────────┐
    │ CreateButton()   │──────────────────────────────────────┐
    │ CreateCheckbox() │──────────────────────────────┐       │
    └──────────────────┘                              │       │
              ▲                                       │       │
    ┌─────────┴──────────┐                    «interface»  «interface»
    │                    │                    ICheckbox    IButton
LightThemeFactory   DarkThemeFactory              ▲            ▲
    │                    │                   ┌────┴────┐  ┌────┴────┐
    │ creates            │ creates            │         │  │         │
    ▼                    ▼               LightChk  DarkChk LightBtn DarkBtn
LightButton          DarkButton
LightCheckbox        DarkCheckbox

              UIRenderer (Client)
         ┌──────────────────────┐
         │ - _factory: IUIFactory│
         │ RenderLoginForm()    │  ← uses only IUIFactory, IButton, ICheckbox
         │ RenderSettingsPanel()│    never knows about Light or Dark
         └──────────────────────┘
```

## 💻 Implementation in This Example

### Files:
- **IUIFactory.cs** — Abstract Factory interface + `IButton` and `ICheckbox` product interfaces
- **LightThemeFactory.cs** — `LightThemeFactory` + `LightButton` + `LightCheckbox`
- **DarkThemeFactory.cs** — `DarkThemeFactory` + `DarkButton` + `DarkCheckbox`
- **UIRenderer.cs** — Client; depends only on interfaces
- **Program.cs** — Demo with 5 demonstrations

### Key Implementation Points:

**1. Abstract Factory interface** — one method per product type:
```csharp
public interface IUIFactory
{
    IButton   CreateButton(string label);
    ICheckbox CreateCheckbox(string label);
}
```

**2. Concrete factory** — creates only its own family:
```csharp
public class DarkThemeFactory : IUIFactory
{
    public IButton   CreateButton(string label)   => new DarkButton(label);
    public ICheckbox CreateCheckbox(string label) => new DarkCheckbox(label);
}
```

**3. Client** — receives factory at construction, never references concrete types:
```csharp
public class UIRenderer(IUIFactory factory)
{
    public void RenderLoginForm()
    {
        var btn = factory.CreateButton("Login");   // returns IButton — Light or Dark
        var chk = factory.CreateCheckbox("Remember me"); // same theme, guaranteed
        btn.Render();
        chk.Render();
    }
}
```

**4. Swapping themes** — one line at the call site, nothing else changes:
```csharp
var renderer = new UIRenderer(new DarkThemeFactory());  // swap to light: just change this
renderer.RenderLoginForm();
renderer.RenderSettingsPanel();
```

## 🔍 Abstract Factory vs Factory Method

| | Factory Method | Abstract Factory |
|---|---|---|
| **Creates** | One product type | A family of related product types |
| **Mechanism** | Subclass overrides one method | Implement an interface with multiple methods |
| **Use when** | You need to vary ONE type | You need to vary a GROUP of types together |
| **Example** | `CreateProcessor()` → one payment processor | `CreateButton()` + `CreateCheckbox()` → entire theme |

Abstract Factory is often implemented using multiple Factory Methods internally.

## 🚀 How to Run

```bash
cd src/1-Creational/1.3-AbstractFactory/AbstractFactoryPattern
dotnet run
```

## 🧪 Running Tests

```bash
cd src/1-Creational/1.3-AbstractFactory/AbstractFactoryPattern.Tests
dotnet test
```

## 🧪 What the Demo Shows

1. **Light Theme** — full login form and settings panel rendered in light style
2. **Dark Theme** — same client code, same UIRenderer, completely different output
3. **Runtime selection** — factory chosen at runtime from a preference value
4. **Family consistency guarantee** — why mixing is impossible by design
5. **Extensibility** — how to add a High Contrast theme with zero changes to existing code

## ✅ Benefits

| Benefit | Description |
|---------|-------------|
| **Family Consistency** | Impossible to mix products from different factories by accident |
| **Open/Closed Principle** | Add new families by adding new factory classes — nothing existing changes |
| **Decoupling** | Client depends only on interfaces; concrete types are invisible to it |
| **Single Responsibility** | Each concrete factory owns exactly one family |

## ❌ Drawbacks

| Drawback | Description |
|----------|-------------|
| **Adding new product types is hard** | Adding `IDialog` to the family requires changing `IUIFactory` and ALL concrete factories |
| **More classes** | Every new family = new factory + new product classes for each type |
| **Complexity** | Overkill if you only have one family or the products don't need to be consistent |

## 🎓 When to Use

✅ **Good Candidates:**
- UI theming / platform-specific UI (Windows vs Mac vs Web)
- Cloud provider abstraction (AWS vs Azure vs GCP)
- Database provider abstraction (SQL Server vs PostgreSQL vs SQLite)
- Test doubles — swap a real infrastructure factory for an in-memory factory in tests

❌ **Bad Candidates:**
- When you only have one family (just use direct instantiation or Factory Method)
- When products don't need to be consistent with each other
- When you need to frequently add new product types (every addition forces changes to the factory interface)

## 🔀 Alternatives

| Alternative | When to Use Instead |
|-------------|---------------------|
| **Factory Method (1.2)** | You only vary one product type, not a family |
| **Builder (1.4)** | The object is complex to construct (many steps/options), not a consistency problem |
| **Dependency Injection** | You want a container to handle wiring instead of manual factory selection |
| **Strategy (3.09)** | The variation is in runtime behaviour, not in object construction |

## 📚 Related Patterns

- **Factory Method (1.2)** — Abstract Factory uses Factory Methods internally; this pattern is one level above it
- **Singleton (1.1)** — Concrete factories are often Singletons (stateless, one instance is sufficient)
- **Prototype (1.5)** — Can be used instead when cloning an existing product family is easier than constructing from scratch

## 🔑 Key Takeaways

1. **The "abstract" in the name refers to the factory interface** — not to abstract classes
2. **Family consistency is the core guarantee** — that's what separates it from multiple Factory Methods
3. **Adding product types is the pain point** — adding a new product to the interface breaks all factories
4. **The client wires up the factory once** — usually at application startup or via dependency injection
5. **Real-world shortcut**: in .NET you often skip the abstract factory and register the correct concrete types in the DI container instead

## 📖 Further Reading

- "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four) — Chapter 3
- "Head First Design Patterns" — Chapter 4

---

← **Previous Pattern:** [1.2 - Factory Method](../1.2-FactoryMethod/)
→ **Next Pattern:** [1.4 - Builder](../1.4-Builder/)
