using TemplateMethodPattern;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

static void PrintOutput(string label, string output)
{
    Console.WriteLine($"  ── {label} ──");
    foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        Console.WriteLine($"  {line}");
}

IReadOnlyList<SaleRecord> records =
[
    new("Laptop",   3, 1_249.99m),
    new("Mouse",   12,    39.99m),
    new("Keyboard", 8,    89.99m),
    new("Monitor",  5,   549.99m),
    new("Headset",  6,   129.99m),
];

Console.WriteLine("=== Template Method Pattern — Sales Report Exporter ===\n");

// Demo 1 — CSV export
Console.WriteLine("── DEMO 1: CSV Export ──\n");
PrintOutput("CsvExporter", new CsvExporter().Export(records));
Pause();

// Demo 2 — JSON export
Console.WriteLine("── DEMO 2: JSON Export ──\n");
PrintOutput("JsonExporter", new JsonExporter().Export(records));
Pause();

// Demo 3 — HTML export
Console.WriteLine("── DEMO 3: HTML Export ──\n");
PrintOutput("HtmlExporter", new HtmlExporter().Export(records));
Pause();

// Demo 4 — Same records, all exporters in a loop (algorithm skeleton is identical)
Console.WriteLine("── DEMO 4: Same data, same step order, three formats ──\n");
ReportExporter[] exporters = [new CsvExporter(), new JsonExporter(), new HtmlExporter()];
foreach (var exporter in exporters)
{
    var output = exporter.Export(records);
    var lines  = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
    Console.WriteLine($"  {exporter.GetType().Name,-20} → {lines} lines");
}
Pause();

// Demo 5 — Edge case: empty record list
Console.WriteLine("── DEMO 5: Empty record list ──\n");
IReadOnlyList<SaleRecord> empty = [];
PrintOutput("CsvExporter  (empty)", new CsvExporter().Export(empty));
PrintOutput("JsonExporter (empty)", new JsonExporter().Export(empty));

Console.WriteLine("\n=== End of Template Method pattern demo ===");
