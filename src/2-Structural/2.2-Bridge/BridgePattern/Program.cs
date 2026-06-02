using BridgePattern;
using BridgePattern.Renderers;
using BridgePattern.Reports;

// ============================================================
// BRIDGE PATTERN — DEMO
// ============================================================
// Shows how 3 report types × 3 output formats work in any
// combination without a class for each pair.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

// ── Shared sample data ────────────────────────────────────────

var salesData = new SalesSummaryData
{
    Period            = "Q2 2026",
    TotalRevenue      = 1_248_750.00m,
    TotalOrders       = 4_312,
    AverageOrderValue = 289.60m,
    ConversionRate    = 0.034,
    TopProduct        = "ProPlan Annual",
    TopRegion         = "North America",
    ExecutiveSummary  = "Q2 exceeded targets by 12%. Renewal rate improved to 87%."
};

var inventoryData = new InventoryData
{
    AsOfDate = new DateTime(2026, 6, 1),
    Items =
    [
        new() { ProductName = "Widget A",    Quantity = 1_200, ReorderLevel = 200 },
        new() { ProductName = "Widget B",    Quantity = 85,    ReorderLevel = 100 },
        new() { ProductName = "Gadget X",    Quantity = 0,     ReorderLevel = 50  },
        new() { ProductName = "Component Z", Quantity = 450,   ReorderLevel = 150 },
        new() { ProductName = "Module Q",    Quantity = 30,    ReorderLevel = 75  },
    ]
};

var activityData = new UserActivityData
{
    Period                = "May 2026",
    DailyActiveUsers      = 18_432,
    MonthlyActiveUsers    = 94_100,
    AverageSessionMinutes = 12.4,
    RetentionRate         = 0.71,
    ChurnRiskUsers        = 3_200,
    TopFeatures =
    [
        new() { Name = "Dashboard",    UsageCount = 312_000 },
        new() { Name = "Reports",      UsageCount = 198_400 },
        new() { Name = "Integrations", UsageCount = 87_300  },
    ]
};

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║              BRIDGE PATTERN — Reports                ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("3 report types × 3 output formats = 9 combinations.");
Console.WriteLine("Bridge achieves this with 3 + 3 = 6 classes, not 9.");
Console.WriteLine("Each report type works with any renderer — independently.");

Pause();

// ── DEMO 1: Sales Summary in all three formats ────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Sales Summary Report in PDF, HTML, and CSV");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

IReportRenderer[] renderers = [new PdfRenderer(), new HtmlRenderer(), new CsvRenderer()];

foreach (var renderer in renderers)
{
    var report = new SalesSummaryReport(renderer, salesData);
    Console.WriteLine($"\n--- {renderer.FormatName} Output ---");
    Console.WriteLine(report.Generate());
}

Pause();

// ── DEMO 2: Inventory Detail Report ──────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Inventory Detail Report (PDF and CSV)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Same report class — swap renderer to change format.");

foreach (var renderer in new IReportRenderer[] { new PdfRenderer(), new CsvRenderer() })
{
    var report = new InventoryDetailReport(renderer, inventoryData);
    Console.WriteLine($"\n--- {renderer.FormatName} Output ---");
    Console.WriteLine(report.Generate());
}

Pause();

// ── DEMO 3: User Activity Report ─────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — User Activity Report (HTML)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

var activityReport = new UserActivityReport(new HtmlRenderer(), activityData);
Console.WriteLine(activityReport.Generate());

Pause();

// ── DEMO 4: Runtime renderer swap ────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Runtime renderer selection (user preference)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Imagine a user picking their preferred export format.");
Console.WriteLine("The report type stays fixed; the renderer is swapped.");
Console.WriteLine();

string[] userPreferences = ["PDF", "CSV", "HTML"];

foreach (string preference in userPreferences)
{
    IReportRenderer selectedRenderer = preference switch
    {
        "PDF"  => new PdfRenderer(),
        "HTML" => new HtmlRenderer(),
        _      => new CsvRenderer()
    };

    var report = new SalesSummaryReport(selectedRenderer, salesData);
    string output = report.Generate();

    // Show just the first 3 lines to keep demo concise
    string preview = string.Join('\n', output.Split('\n').Take(4));
    Console.WriteLine($"  User chose {preference}:");
    Console.WriteLine(preview);
    Console.WriteLine();
}

Pause();

// ── DEMO 5: The combinatorial explosion without Bridge ────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — Why Bridge? The combinatorial problem.");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Without Bridge (inheritance only):");
Console.WriteLine("  3 report types × 3 formats = 9 classes");
Console.WriteLine("  SalesSummaryPdf, SalesSummaryHtml, SalesSummaryCsv");
Console.WriteLine("  InventoryDetailPdf, InventoryDetailHtml, InventoryDetailCsv");
Console.WriteLine("  UserActivityPdf,   UserActivityHtml,   UserActivityCsv");
Console.WriteLine();
Console.WriteLine("  Adding a 4th format (e.g. Excel) = 3 more classes.");
Console.WriteLine("  Adding a 4th report type         = 3 more classes.");
Console.WriteLine("  Formula: M × N classes.");
Console.WriteLine();
Console.WriteLine("With Bridge:");
Console.WriteLine("  3 report classes + 3 renderer classes = 6 classes");
Console.WriteLine("  Adding a 4th format = 1 new renderer class.");
Console.WriteLine("  Adding a 4th report = 1 new report class.");
Console.WriteLine("  Formula: M + N classes.");
Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • Report subclasses define WHAT data to show");
Console.WriteLine("  • Renderer implementations define HOW to format it");
Console.WriteLine("  • Neither side knows the concrete type of the other");
Console.WriteLine("  • Any report works with any renderer — at runtime");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
