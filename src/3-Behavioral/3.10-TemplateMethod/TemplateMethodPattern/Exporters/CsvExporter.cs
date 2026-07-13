using System.Text;

namespace TemplateMethodPattern;

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
