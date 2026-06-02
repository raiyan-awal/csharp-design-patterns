# 2.2 Bridge Pattern

## Intent

Decouple an abstraction from its implementation so that the two can vary independently.

---

## The Problem It Solves

You need to support multiple report types (Sales Summary, Inventory Detail, User Activity) and multiple output formats (PDF, HTML, CSV). If you model this with inheritance alone, you end up with a class for every combination:

```
SalesSummaryPdf      SalesSummaryHtml      SalesSummaryCsv
InventoryDetailPdf   InventoryDetailHtml   InventoryDetailCsv
UserActivityPdf      UserActivityHtml      UserActivityCsv
```

That's **M × N classes**. Adding a 4th format (e.g. Excel) means 3 new classes. Adding a 4th report type means 3 more. This is the combinatorial explosion the Bridge pattern solves.

Bridge separates the two dimensions into independent hierarchies and connects them with a reference (the "bridge"):

```
Report types:   3 classes
Output formats: 3 classes
Total:          6 classes — any combination works at runtime
```

---

## Domain: Reporting System

| Role | Class | Description |
|------|-------|-------------|
| **Implementor Interface** | `IReportRenderer` | Defines how output is produced |
| **Concrete Implementor** | `PdfRenderer` | Produces PDF-style structured text |
| **Concrete Implementor** | `HtmlRenderer` | Produces HTML markup |
| **Concrete Implementor** | `CsvRenderer` | Produces comma-separated values |
| **Abstraction** | `Report` | Base class; holds renderer reference; defines `Generate()` |
| **Refined Abstraction** | `SalesSummaryReport` | Defines what sales data to show |
| **Refined Abstraction** | `InventoryDetailReport` | Defines what inventory data to show |
| **Refined Abstraction** | `UserActivityReport` | Defines what activity data to show |

---

## Structure

```
Report  ──────────────────────────────►  IReportRenderer
  │  (holds reference to implementor)        ▲
  │                                   ┌──────┼──────┐
  ▼                               PdfRenderer  HtmlRenderer  CsvRenderer
SalesSummaryReport
InventoryDetailReport
UserActivityReport
```

The arrow from `Report` to `IReportRenderer` is the bridge. `Report` subclasses call `Renderer.RenderTitle(...)`, `Renderer.RenderRow(...)` etc. — they never reference a concrete renderer.

---

## Key Design Decision

```csharp
// Abstraction base class — holds the bridge reference
public abstract class Report
{
    protected readonly IReportRenderer Renderer;   // ← the bridge

    protected Report(IReportRenderer renderer) => Renderer = renderer;

    public string Generate()
    {
        Renderer.BeginDocument();
        BuildContent();            // ← defined by each report subclass
        Renderer.EndDocument();
        return Renderer.GetOutput();
    }

    protected abstract void BuildContent();
}

// Refined abstraction — only knows WHAT data to emit, not HOW
public sealed class SalesSummaryReport : Report
{
    protected override void BuildContent()
    {
        Renderer.RenderTitle("Sales Summary");   // delegates to whatever renderer was injected
        Renderer.RenderRow("Revenue", "$500,000");
        // ...
    }
}
```

Any renderer injected at construction time changes the format — no report class needs to change.

---

## When to Use

- You have two orthogonal dimensions that both need to vary independently (type × format, platform × feature, transport × protocol)
- You want to avoid a combinatorial explosion of subclasses
- You want to switch implementations at runtime without touching abstractions
- Both the abstraction and implementation should be extensible via subclassing

## When NOT to Use

- You only have one concrete implementation — composition is simpler and cleaner
- The two dimensions are unlikely to ever grow — the indirection adds complexity for no gain
- The abstraction and implementation are tightly coupled and won't vary independently

---

## Bridge vs Adapter

These look similar (both use a reference to another object) but their purposes are different:

| | Bridge | Adapter |
|---|---|---|
| **Purpose** | Decouple two hierarchies so both can grow | Make an incompatible interface work with yours |
| **Design time** | Designed upfront — both sides are yours | Often applied after the fact to a third-party class |
| **Interface relationship** | Both sides are designed together | Adaptee interface is a mismatch you're fixing |

---

## Benefits

- Eliminates M × N class explosion — adding a dimension costs 1 class, not N
- Abstraction and implementation can evolve independently
- Runtime flexibility — swap renderers without changing report classes
- Follows Open/Closed Principle on both dimensions

## Drawbacks

- More upfront design — you must identify the two varying dimensions early
- Indirection makes the code slightly harder to follow for simple cases

---

## Running the Demo

```bash
cd src/2-Structural/2.2-Bridge/BridgePattern
dotnet run
```

## Running Tests

```bash
cd src/2-Structural/2.2-Bridge/BridgePattern.Tests
dotnet test
```
