using TemplateMethodPattern;

namespace TemplateMethodPattern.Tests;

public class ReportExporterTests
{
    private static readonly IReadOnlyList<SaleRecord> Records =
    [
        new("Laptop",    2, 1_249.99m),
        new("Mouse",     5,    39.99m),
        new("Keyboard",  3,    89.99m),
    ];

    private static readonly IReadOnlyList<SaleRecord> Empty = [];

    private static readonly IReadOnlyList<SaleRecord> Single =
    [
        new("Monitor", 1, 549.99m),
    ];

    // ── CSV ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Csv_StartsWithHeaderRow()
    {
        var output = new CsvExporter().Export(Records);
        Assert.StartsWith("Product,Quantity,Unit Price,Total", output);
    }

    [Fact]
    public void Csv_ContainsAllProducts()
    {
        var output = new CsvExporter().Export(Records);
        Assert.Contains("Laptop", output);
        Assert.Contains("Mouse", output);
        Assert.Contains("Keyboard", output);
    }

    [Fact]
    public void Csv_RowFormatsValuesCorrectly()
    {
        var output = new CsvExporter().Export(Records);
        // Laptop: qty 2, unit $1249.99, total $2499.98
        Assert.Contains("Laptop,2,1249.99,2499.98", output);
    }

    [Fact]
    public void Csv_EndsWithGrandTotalRow()
    {
        var output = new CsvExporter().Export(Records);
        // 2*1249.99 + 5*39.99 + 3*89.99 = 2499.98 + 199.95 + 269.97 = 2969.90
        Assert.Contains("Grand Total,2969.90", output);
    }

    [Fact]
    public void Csv_EmptyRecords_OnlyHeaderAndGrandTotal()
    {
        var output = new CsvExporter().Export(Empty);
        Assert.Contains("Product,Quantity,Unit Price,Total", output);
        Assert.Contains("Grand Total,0.00", output);
    }

    // ── JSON ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Json_StartsWithOpenBracket()
    {
        var output = new JsonExporter().Export(Records);
        Assert.StartsWith("[", output.TrimStart());
    }

    [Fact]
    public void Json_EndsWithCloseBracket()
    {
        var output = new JsonExporter().Export(Records);
        Assert.EndsWith("]" + Environment.NewLine, output);
    }

    [Fact]
    public void Json_ContainsAllProducts()
    {
        var output = new JsonExporter().Export(Records);
        Assert.Contains("\"Laptop\"", output);
        Assert.Contains("\"Mouse\"", output);
        Assert.Contains("\"Keyboard\"", output);
    }

    [Fact]
    public void Json_LastRecordHasNoTrailingComma()
    {
        var output = new JsonExporter().Export(Records);
        // Last object line should be "  }" not "  },"
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var lastObjectClose = Array.FindLast(lines, l => l.TrimEnd() is "  }" or "  },");
        Assert.Equal("  }", lastObjectClose?.TrimEnd());
    }

    [Fact]
    public void Json_SingleRecord_NoTrailingComma()
    {
        var output = new JsonExporter().Export(Single);
        Assert.DoesNotContain("},", output);
    }

    [Fact]
    public void Json_EmptyRecords_IsEmptyArray()
    {
        var output = new JsonExporter().Export(Empty);
        Assert.Contains("[", output);
        Assert.Contains("]", output);
        Assert.DoesNotContain("product", output);
    }

    // ── HTML ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Html_WrapsInTableElement()
    {
        var output = new HtmlExporter().Export(Records);
        Assert.Contains("<table>", output);
        Assert.Contains("</table>", output);
    }

    [Fact]
    public void Html_ContainsTheadAndTbody()
    {
        var output = new HtmlExporter().Export(Records);
        Assert.Contains("<thead>", output);
        Assert.Contains("</thead>", output);
        Assert.Contains("<tbody>", output);
        Assert.Contains("</tbody>", output);
    }

    [Fact]
    public void Html_GrandTotalInTfoot()
    {
        var output = new HtmlExporter().Export(Records);
        Assert.Contains("<tfoot>", output);
        Assert.Contains("Grand Total", output);
        Assert.Contains("$2969.90", output);
    }

    [Fact]
    public void Html_ContainsAllProductsInRows()
    {
        var output = new HtmlExporter().Export(Records);
        Assert.Contains("<td>Laptop</td>", output);
        Assert.Contains("<td>Mouse</td>", output);
        Assert.Contains("<td>Keyboard</td>", output);
    }

    // ── Cross-exporter ───────────────────────────────────────────────────────

    [Fact]
    public void AllExporters_ProduceOutputForEveryRecord()
    {
        ReportExporter[] exporters = [new CsvExporter(), new JsonExporter(), new HtmlExporter()];
        foreach (var exporter in exporters)
        {
            var output = exporter.Export(Records);
            Assert.Contains("Laptop",   output);
            Assert.Contains("Mouse",    output);
            Assert.Contains("Keyboard", output);
        }
    }

    [Fact]
    public void AllExporters_HandleEmptyListWithoutThrowing()
    {
        ReportExporter[] exporters = [new CsvExporter(), new JsonExporter(), new HtmlExporter()];
        foreach (var exporter in exporters)
        {
            var ex = Record.Exception(() => exporter.Export(Empty));
            Assert.Null(ex);
        }
    }
}
