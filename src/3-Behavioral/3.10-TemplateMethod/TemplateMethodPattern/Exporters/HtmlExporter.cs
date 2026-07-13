using System.Text;

namespace TemplateMethodPattern;

public sealed class HtmlExporter : ReportExporter
{
    protected override void BeginExport(StringBuilder sb, IReadOnlyList<SaleRecord> records)
    {
        sb.AppendLine("<table>");
        sb.AppendLine("  <thead>");
        sb.AppendLine("    <tr><th>Product</th><th>Quantity</th><th>Unit Price</th><th>Total</th></tr>");
        sb.AppendLine("  </thead>");
        sb.AppendLine("  <tbody>");
    }

    protected override void WriteRecord(StringBuilder sb, SaleRecord r, int index, int total)
        => sb.AppendLine($"    <tr><td>{r.Product}</td><td>{r.Quantity}</td><td>${r.UnitPrice:F2}</td><td>${r.Total:F2}</td></tr>");

    protected override void EndExport(StringBuilder sb, IReadOnlyList<SaleRecord> records)
    {
        var grandTotal = records.Sum(r => r.Total);
        sb.AppendLine("  </tbody>");
        sb.AppendLine("  <tfoot>");
        sb.AppendLine($"    <tr><td colspan=\"3\">Grand Total</td><td>${grandTotal:F2}</td></tr>");
        sb.AppendLine("  </tfoot>");
        sb.AppendLine("</table>");
    }
}
