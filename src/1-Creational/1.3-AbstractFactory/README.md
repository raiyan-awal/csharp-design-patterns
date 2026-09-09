# 1.3 — Abstract Factory

## Intent

Provide an interface for creating families of related objects without specifying their concrete classes, ensuring that the objects produced by a factory are always compatible with each other.

## The Problem It Solves

Without an Abstract Factory, nothing prevents a caller from accidentally mixing components from incompatible families:

```csharp
// Without Abstract Factory: nothing enforces family consistency
var button   = new LightButton("Login");      // Light theme
var checkbox = new DarkCheckbox("Remember");  // Dark theme — inconsistent UI!
renderer.Render(button, checkbox);
```

Problems with ad-hoc construction:
- Mixed families produce visually broken or semantically inconsistent results.
- Every call site must know which concrete class to instantiate; adding a third theme requires touching every call site.
- There is no compile-time guarantee that components were produced by the same factory.
- Switching themes at runtime requires rewriting the creation code, not swapping a factory.

## Solution: Family interface + concrete factories

Define one factory interface (`IUIFactory`) with a creation method per product type. Each concrete factory (`LightThemeFactory`, `DarkThemeFactory`) implements all creation methods and returns a consistent family. The client (`UIRenderer`) takes the factory as a dependency and never names a concrete class.

```csharp
// Client receives the factory — knows nothing about Light or Dark:
IUIFactory factory = new LightThemeFactory();
var renderer = new UIRenderer(factory);
renderer.RenderLoginForm("user@example.ca");

// Switching to dark theme: one line change, zero client code changes
factory = new DarkThemeFactory();
renderer = new UIRenderer(factory);
renderer.RenderLoginForm("user@example.ca");
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Abstract factory | `IUIFactory` | Declares `CreateButton` and `CreateCheckbox`; every method returns an abstract product |
| Concrete factory | `LightThemeFactory` | Creates `LightButton` and `LightCheckbox` — guaranteed same family |
| Concrete factory | `DarkThemeFactory` | Creates `DarkButton` and `DarkCheckbox` — guaranteed same family |
| Abstract product A | `IButton` | Contract for all buttons (Theme, Render, Click) |
| Abstract product B | `ICheckbox` | Contract for all checkboxes (Theme, IsChecked, Render, Toggle) |
| Concrete product A | `LightButton`, `DarkButton` | Theme-specific button rendering |
| Concrete product B | `LightCheckbox`, `DarkCheckbox` | Theme-specific checkbox rendering |
| Client | `UIRenderer` | Uses only `IUIFactory`, `IButton`, `ICheckbox` — no concrete type references |

## Structure

```
1.3-AbstractFactory/
├── AbstractFactoryPattern/
│   ├── IUIFactory.cs          ← abstract factory + IButton + ICheckbox interfaces
│   ├── LightThemeFactory.cs   ← LightThemeFactory, LightButton, LightCheckbox
│   ├── DarkThemeFactory.cs    ← DarkThemeFactory, DarkButton, DarkCheckbox
│   ├── UIRenderer.cs          ← client — depends only on interfaces
│   └── Program.cs
└── AbstractFactoryPattern.Tests/
    └── AbstractFactoryPatternTests.cs
```

## Key Code

### Abstract factory interface

```csharp
public interface IUIFactory
{
    IButton   CreateButton(string label);
    ICheckbox CreateCheckbox(string label);
}
```

Both methods return abstract products. The factory never leaks a concrete type. A third theme is added by writing one new class that implements this interface.

### Concrete factory — guaranteed family consistency

```csharp
public class LightThemeFactory : IUIFactory
{
    public IButton   CreateButton(string label)   => new LightButton(label);
    public ICheckbox CreateCheckbox(string label) => new LightCheckbox(label);
}
```

A `LightThemeFactory` can only produce Light components — mixing is structurally impossible when the client receives its factory via dependency injection.

### Client with no concrete dependencies

```csharp
public class UIRenderer(IUIFactory factory)
{
    public void RenderLoginForm(string userEmail)
    {
        var btn      = factory.CreateButton("Login");
        var remember = factory.CreateCheckbox("Remember me");
        btn.Render();
        remember.Render();
        btn.Click();
    }
}
```

`UIRenderer` depends only on `IUIFactory`, `IButton`, and `ICheckbox`. The concrete theme is decided at the composition root.

## Demo Scenarios

```
1. Light theme rendering      — LightThemeFactory produces a consistent light-styled form
2. Dark theme rendering       — DarkThemeFactory produces a consistent dark-styled form
3. Runtime theme switch       — factory swapped at runtime; UIRenderer code unchanged
4. Family consistency         — Light factory cannot produce a Dark component; mixed-family
                                creation shown as the problem the pattern solves
5. Extensibility              — hypothetical "SystemThemeFactory" added with zero changes
                                to UIRenderer or existing factories
```

## When to Use

- The system must work with multiple families of related objects and must not depend on the concrete classes of those objects.
- You need to enforce that objects from one family are always used together and never mixed with another family.
- Switching between product families should be possible at runtime or configuration time without changing client code.
- You want to provide a library of products and reveal only their interfaces, not their implementations.

## When NOT to Use

- When there is only one product family and no plans to add others — the indirection is unnecessary overhead.
- When the products are so simple that factory method or even `new` is sufficient and readable.
- When the number of product types in a family changes frequently — every addition requires updating the factory interface and all concrete factories.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Family consistency | A factory can only produce components from its own family — mismatches are impossible |
| Open/Closed Principle | New themes are added by implementing `IUIFactory`; existing code is untouched |
| Decoupled client | `UIRenderer` never imports `LightButton` or `DarkCheckbox` |
| Configurable at runtime | Pass a different factory to change the entire product family with one substitution |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Interface update burden | Adding a new product type (e.g., `CreateSlider`) requires changing `IUIFactory` and every concrete factory |
| Class proliferation | One factory interface + two factories + two products per type × N themes = many classes |
| Complexity for simple cases | If there is only one theme or the products are trivially constructed, this pattern is overkill |

## Related Patterns

- **Factory Method (1.2)** — Abstract Factory is often implemented using factory methods; a factory method creates one product, while an Abstract Factory creates a whole family.
- **Singleton (1.1)** — Concrete factories are frequently implemented as Singletons because only one instance is needed per theme.
- **Builder (1.4)** — Builder focuses on constructing a single complex object step by step; Abstract Factory focuses on creating families of objects in one call.
- **Dependency Injection (4.05)** — DI containers can be configured to supply the correct concrete factory, effectively replacing the composition-root factory selection with container registration.

## Running the Demo

```bash
cd src/1-Creational/1.3-AbstractFactory/AbstractFactoryPattern
dotnet run
```

## Running the Tests

```bash
cd src/1-Creational/1.3-AbstractFactory/AbstractFactoryPattern.Tests
dotnet test
```
