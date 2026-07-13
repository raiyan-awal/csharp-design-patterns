# 3.10 — Template Method Pattern

## Intent

Define the skeleton of an algorithm in a base class, deferring specific steps to subclasses. Template Method lets subclasses fill in those steps without changing the algorithm's structure or order.

---

## The Problem It Solves

A reporting system needs to export sales data in multiple formats. Without Template Method, either the steps are duplicated across exporters or a single class grows a large conditional:

```csharp
// WITHOUT Template Method — duplicated skeleton across every exporter:
public class CsvExporter
{
    public string Export(IReadOnlyList<SaleRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Product,Quantity,..."); // header
        foreach (var r in records) sb.AppendLine(...);
        sb.AppendLine(",,Grand Total,...");    // footer
        return sb.ToString();
    }
}

public class JsonExporter
{
    public string Export(IReadOnlyList<SaleRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");                    // header
        for (var i = 0; i < records.Count; i++) sb.AppendLine(...);
        sb.AppendLine("]");                    // footer
        return sb.ToString();
    }
}
// The "BeginExport → WriteRecord × N → EndExport" structure is copy-pasted everywhere.
```

Template Method moves the skeleton into a single base class. Subclasses only supply what varies.

---

## Solution: Sales Report Exporter

Three exporters each produce a different format from the same sales data. The base class `ReportExporter` owns the loop and the step order — subclasses implement the format-specific details.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **Abstract Class** | `ReportExporter` | Defines `Export` (template method) and abstract/virtual step methods |
| **Concrete Classes** | `CsvExporter`, `JsonExporter`, `HtmlExporter` | Implement each step for their specific format |

### Algorithm steps

| Step | Modifier | Meaning |
|------|----------|---------|
| `BeginExport` | `abstract` | Must override — open the document (CSV header, JSON `[`, HTML `<table>`) |
| `WriteRecord` | `abstract` | Must override — format one record |
| `EndExport` | `virtual` | Optional override — close the document + summary (grand total row, `]`, `</table>`) |

`EndExport` is a **hook** — its default is a no-op. CSV and HTML use it for a grand total row; JSON uses it to close the array. A subclass that needs no footer just inherits the no-op.

---

## Structure

```
TemplateMethodPattern/
├── SaleRecord.cs          ← record: Product, Quantity, UnitPrice, Total
├── ReportExporter.cs      ← Abstract base class with template method Export()
└── Exporters/
    ├── CsvExporter.cs
    ├── JsonExporter.cs
    └── HtmlExporter.cs
```

---

## Key Code

### Abstract class — skeleton fixed, steps deferred

```csharp
public abstract class ReportExporter
{
    // Template method — non-virtual, so no subclass can override or reorder the steps
    public string Export(IReadOnlyList<SaleRecord> records)
    {
        var sb = new StringBuilder();
        BeginExport(sb, records);                                      // step 1
        for (var i = 0; i < records.Count; i++)
            WriteRecord(sb, records[i], index: i, total: records.Count); // step 2 × N
        EndExport(sb, records);                                        // step 3 (hook)
        return sb.ToString();
    }

    protected abstract void BeginExport(StringBuilder sb, IReadOnlyList<SaleRecord> records);
    protected abstract void WriteRecord(StringBuilder sb, SaleRecord record, int index, int total);
    protected virtual  void EndExport(StringBuilder sb, IReadOnlyList<SaleRecord> records) { }
}
```

`Export` is not `virtual`, so subclasses cannot override or reorder it. In C#, a non-virtual method is already non-overridable — `sealed` is only needed when preventing further overriding of an already-virtual method.

### Concrete class — fills in the steps, ignores the structure

```csharp
public sealed class CsvExporter : ReportExporter
{
    protected override void BeginExport(StringBuilder sb, IReadOnlyList<SaleRecord> records)
        => sb.AppendLine("Product,Quantity,Unit Price,Total");

    protected override void WriteRecord(StringBuilder sb, SaleRecord r, int index, int total)
        => sb.AppendLine($"{r.Product},{r.Quantity},{r.UnitPrice:F2},{r.Total:F2}");

    protected override void EndExport(StringBuilder sb, IReadOnlyList<SaleRecord> records)
    {
        var grandTotal = records.Sum(r => r.Total);
        sb.AppendLine($",,Grand Total,{grandTotal:F2}");
    }
}
```

```csharp
public sealed class JsonExporter : ReportExporter
{
    protected override void BeginExport(StringBuilder sb, IReadOnlyList<SaleRecord> records)
        => sb.AppendLine("[");

    protected override void WriteRecord(StringBuilder sb, SaleRecord r, int index, int total)
    {
        sb.AppendLine("  {");
        sb.AppendLine($"    \"product\": \"{r.Product}\",");
        sb.AppendLine($"    \"quantity\": {r.Quantity},");
        sb.AppendLine($"    \"unitPrice\": {r.UnitPrice:F2},");
        sb.AppendLine($"    \"total\": {r.Total:F2}");
        // index and total let the last record omit its trailing comma
        sb.AppendLine(index < total - 1 ? "  }," : "  }");
    }

    protected override void EndExport(StringBuilder sb, IReadOnlyList<SaleRecord> records)
        => sb.AppendLine("]");
}
```

### Usage

```csharp
IReadOnlyList<SaleRecord> records = [ new("Laptop", 3, 1249.99m), ... ];

string csv  = new CsvExporter().Export(records);
string json = new JsonExporter().Export(records);
string html = new HtmlExporter().Export(records);
// All three call the same Export skeleton — only the output differs
```

---

## Demo Scenarios

```
DEMO 1 — CSV export: header row, data rows, grand total footer
DEMO 2 — JSON export: array with objects, no trailing comma on last element
DEMO 3 — HTML export: <table> with <thead>, <tbody>, <tfoot> grand total
DEMO 4 — All three exporters on the same data, line-count comparison
DEMO 5 — Empty record list: each exporter handles gracefully
```

---

## When to Use

- Multiple classes share the same algorithm structure but differ in specific steps
- You want to enforce a fixed sequence and allow variation only within it
- You want to eliminate duplicate skeleton code while keeping each variant's logic isolated

---

## When NOT to Use

- The algorithm steps vary too much to fit a common skeleton
- You need to change the algorithm entirely at runtime — use Strategy instead
- Deep inheritance hierarchies make the flow hard to follow

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Eliminates duplicate skeleton** | The loop and step order live once in the base class |
| **Open/Closed** | New formats add a subclass without touching existing code |
| **Hollywood Principle** | "Don't call us, we'll call you" — the base class calls the subclass steps, not the other way |
| **Hooks are optional** | Virtual hooks let subclasses opt in to extra behaviour without being forced to |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Inheritance coupling** | Subclasses are tightly coupled to the base class skeleton |
| **Hard to follow** | Execution jumps between base and subclass — harder to trace than flat code |
| **Liskov risk** | A subclass can technically shadow `Export` with `new` — discipline required |
| **Can't reorder** | If a subclass legitimately needs a different step order, Template Method doesn't fit |

---

## Template Method vs Strategy

| | Template Method | Strategy |
|---|---------|-------|
| **Mechanism** | Inheritance — skeleton in base class | Composition — algorithm injected as an object |
| **Variation scope** | Only specific steps vary | The entire algorithm is swappable |
| **Runtime swap** | No — format is fixed at construction | Yes — call `SetStrategy` any time |
| **Coupling** | Subclass depends on base class | Context and strategy are independent |
| **Use when** | Formats share 80% structure, differ in 20% | Algorithms are entirely different |

---

## Related Patterns

- **Strategy (3.09)** — replaces the whole algorithm via composition; Template Method replaces specific steps via inheritance
- **Factory Method (1.02)** — is itself a Template Method specialisation: the skeleton calls `CreateProduct()`, which subclasses override
- **Hook Method** — the `virtual` no-op steps (`EndExport`) are hooks; a common companion to Template Method

---

## Running the Demo

```bash
cd src/3-Behavioral/3.10-TemplateMethod/TemplateMethodPattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.10-TemplateMethod/TemplateMethodPattern.Tests
dotnet test
```
