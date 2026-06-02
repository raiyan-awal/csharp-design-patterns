using BridgePattern;
using BridgePattern.Renderers;
using BridgePattern.Reports;

namespace BridgePattern.Tests;

// ============================================================
// BRIDGE PATTERN TESTS
// ============================================================
// Tests verify two things:
//   1. Each renderer produces format-correct output
//      (HTML has tags, CSV has commas, PDF has plain text markers)
//   2. Each report emits the correct logical content
//      regardless of which renderer is used
// ============================================================

// ── Shared sample data ────────────────────────────────────────

file static class SampleData
{
    public static SalesSummaryData Sales => new()
    {
        Period            = "Q1 2026",
        TotalRevenue      = 500_000m,
        TotalOrders       = 1_000,
        AverageOrderValue = 500m,
        ConversionRate    = 0.05,
        TopProduct        = "BasicPlan",
        TopRegion         = "Europe",
        ExecutiveSummary  = "Strong quarter overall."
    };

    public static InventoryData Inventory => new()
    {
        AsOfDate = new DateTime(2026, 1, 1),
        Items =
        [
            new() { ProductName = "Alpha",  Quantity = 500, ReorderLevel = 100 },
            new() { ProductName = "Beta",   Quantity = 20,  ReorderLevel = 50  },
            new() { ProductName = "Gamma",  Quantity = 0,   ReorderLevel = 30  },
        ]
    };

    public static UserActivityData Activity => new()
    {
        Period                = "Jan 2026",
        DailyActiveUsers      = 10_000,
        MonthlyActiveUsers    = 50_000,
        AverageSessionMinutes = 8.5,
        RetentionRate         = 0.65,
        ChurnRiskUsers        = 1_000,
        TopFeatures           = [new() { Name = "Search", UsageCount = 80_000 }]
    };
}

// ── Renderer format tests ─────────────────────────────────────

public class HtmlRendererTests
{
    [Fact]
    public void HtmlRenderer_Output_ContainsDoctype()
    {
        var report = new SalesSummaryReport(new HtmlRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("<!DOCTYPE html>", output);
    }

    [Fact]
    public void HtmlRenderer_Title_WrappedInH1()
    {
        var report = new SalesSummaryReport(new HtmlRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("<h1>", output);
        Assert.Contains("</h1>", output);
    }

    [Fact]
    public void HtmlRenderer_Section_WrappedInH2()
    {
        var report = new SalesSummaryReport(new HtmlRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("<h2>", output);
        Assert.Contains("</h2>", output);
    }

    [Fact]
    public void HtmlRenderer_Row_ContainsStrongTag()
    {
        var report = new SalesSummaryReport(new HtmlRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("<strong>", output);
    }

    [Fact]
    public void HtmlRenderer_Output_ContainsClosingBodyTag()
    {
        var report = new SalesSummaryReport(new HtmlRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("</body></html>", output);
    }

    [Fact]
    public void HtmlRenderer_FormatName_IsHtml()
    {
        Assert.Equal("HTML", new HtmlRenderer().FormatName);
    }
}

public class CsvRendererTests
{
    [Fact]
    public void CsvRenderer_Title_EmittedAsComment()
    {
        var report = new SalesSummaryReport(new CsvRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("# TITLE:", output);
    }

    [Fact]
    public void CsvRenderer_Section_EmittedAsComment()
    {
        var report = new SalesSummaryReport(new CsvRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("# SECTION:", output);
    }

    [Fact]
    public void CsvRenderer_Row_ContainsComma()
    {
        var report = new SalesSummaryReport(new CsvRenderer(), SampleData.Sales);
        string output = report.Generate();

        // Every data row should have at least one comma
        var dataLines = output.Split('\n')
            .Where(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l));
        Assert.All(dataLines, line => Assert.Contains(",", line));
    }

    [Fact]
    public void CsvRenderer_FormatName_IsCsv()
    {
        Assert.Equal("CSV", new CsvRenderer().FormatName);
    }
}

public class PdfRendererTests
{
    [Fact]
    public void PdfRenderer_Output_ContainsDocumentStartMarker()
    {
        var report = new SalesSummaryReport(new PdfRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("[PDF DOCUMENT START]", output);
    }

    [Fact]
    public void PdfRenderer_Output_ContainsDocumentEndMarker()
    {
        var report = new SalesSummaryReport(new PdfRenderer(), SampleData.Sales);
        string output = report.Generate();
        Assert.Contains("[PDF DOCUMENT END]", output);
    }

    [Fact]
    public void PdfRenderer_FormatName_IsPdf()
    {
        Assert.Equal("PDF", new PdfRenderer().FormatName);
    }
}

// ── Report content tests ──────────────────────────────────────

public class SalesSummaryReportTests
{
    [Fact]
    public void SalesSummaryReport_ContainsPeriod()
    {
        var report = new SalesSummaryReport(new PdfRenderer(), SampleData.Sales);
        Assert.Contains("Q1 2026", report.Generate());
    }

    [Fact]
    public void SalesSummaryReport_ContainsTotalRevenue()
    {
        var report = new SalesSummaryReport(new PdfRenderer(), SampleData.Sales);
        Assert.Contains("500,000", report.Generate());
    }

    [Fact]
    public void SalesSummaryReport_ContainsTopProduct()
    {
        var report = new SalesSummaryReport(new CsvRenderer(), SampleData.Sales);
        Assert.Contains("BasicPlan", report.Generate());
    }

    [Fact]
    public void SalesSummaryReport_ContainsExecutiveSummary()
    {
        var report = new SalesSummaryReport(new HtmlRenderer(), SampleData.Sales);
        Assert.Contains("Strong quarter overall.", report.Generate());
    }

    [Theory]
    [InlineData(typeof(PdfRenderer))]
    [InlineData(typeof(HtmlRenderer))]
    [InlineData(typeof(CsvRenderer))]
    public void SalesSummaryReport_AllRenderers_ContainTopRegion(Type rendererType)
    {
        var renderer = (IReportRenderer)Activator.CreateInstance(rendererType)!;
        var report   = new SalesSummaryReport(renderer, SampleData.Sales);
        Assert.Contains("Europe", report.Generate());
    }
}

public class InventoryDetailReportTests
{
    [Fact]
    public void InventoryReport_ContainsProductNames()
    {
        var report = new InventoryDetailReport(new PdfRenderer(), SampleData.Inventory);
        string output = report.Generate();
        Assert.Contains("Alpha", output);
        Assert.Contains("Beta", output);
        Assert.Contains("Gamma", output);
    }

    [Fact]
    public void InventoryReport_OutOfStockItem_MarkedCorrectly()
    {
        var report = new InventoryDetailReport(new PdfRenderer(), SampleData.Inventory);
        Assert.Contains("OUT OF STOCK", report.Generate());
    }

    [Fact]
    public void InventoryReport_LowStockItem_MarkedCorrectly()
    {
        var report = new InventoryDetailReport(new PdfRenderer(), SampleData.Inventory);
        Assert.Contains("LOW STOCK", report.Generate());
    }

    [Fact]
    public void InventoryReport_ReorderActionMessage_PresentWhenItemsLow()
    {
        var report = new InventoryDetailReport(new HtmlRenderer(), SampleData.Inventory);
        Assert.Contains("reorder", report.Generate());
    }

    [Fact]
    public void InventoryReport_CsvFormat_ContainsProductNamesWithCommas()
    {
        var report = new InventoryDetailReport(new CsvRenderer(), SampleData.Inventory);
        string output = report.Generate();
        // CSV rows: "Alpha,Qty: ..."
        Assert.Contains("Alpha,", output);
    }
}

public class UserActivityReportTests
{
    [Fact]
    public void UserActivityReport_ContainsPeriod()
    {
        var report = new UserActivityReport(new HtmlRenderer(), SampleData.Activity);
        Assert.Contains("Jan 2026", report.Generate());
    }

    [Fact]
    public void UserActivityReport_ContainsDailyActiveUsers()
    {
        var report = new UserActivityReport(new PdfRenderer(), SampleData.Activity);
        Assert.Contains("10,000", report.Generate());
    }

    [Fact]
    public void UserActivityReport_ContainsTopFeature()
    {
        var report = new UserActivityReport(new CsvRenderer(), SampleData.Activity);
        Assert.Contains("Search", report.Generate());
    }

    [Fact]
    public void UserActivityReport_ContainsChurnRiskMessage()
    {
        var report = new UserActivityReport(new HtmlRenderer(), SampleData.Activity);
        Assert.Contains("Churn risk", report.Generate());
    }
}

// ── Bridge independence test ──────────────────────────────────

public class BridgeIndependenceTests
{
    [Fact]
    public void AnyReportCombinedWithAnyRenderer_DoesNotThrow()
    {
        IReportRenderer[] renderers =
            [new PdfRenderer(), new HtmlRenderer(), new CsvRenderer()];

        foreach (var renderer in renderers)
        {
            // All three report types must work with all three renderers
            var r1 = new SalesSummaryReport(renderer, SampleData.Sales);
            var r2 = new InventoryDetailReport(renderer, SampleData.Inventory);
            var r3 = new UserActivityReport(renderer, SampleData.Activity);

            Assert.NotEmpty(r1.Generate());
            Assert.NotEmpty(r2.Generate());
            Assert.NotEmpty(r3.Generate());
        }
    }

    [Fact]
    public void SameReportType_DifferentRenderers_ProduceDifferentOutput()
    {
        var pdfOutput  = new SalesSummaryReport(new PdfRenderer(),  SampleData.Sales).Generate();
        var htmlOutput = new SalesSummaryReport(new HtmlRenderer(), SampleData.Sales).Generate();
        var csvOutput  = new SalesSummaryReport(new CsvRenderer(),  SampleData.Sales).Generate();

        Assert.NotEqual(pdfOutput, htmlOutput);
        Assert.NotEqual(htmlOutput, csvOutput);
        Assert.NotEqual(pdfOutput, csvOutput);
    }

    [Fact]
    public void SameRenderer_DifferentReportTypes_ProduceDifferentOutput()
    {
        var renderer = new PdfRenderer();
        // Each report must use its own renderer instance
        var salesOutput     = new SalesSummaryReport(new PdfRenderer(), SampleData.Sales).Generate();
        var inventoryOutput = new InventoryDetailReport(new PdfRenderer(), SampleData.Inventory).Generate();

        Assert.NotEqual(salesOutput, inventoryOutput);
    }
}
