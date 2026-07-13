using System.Text;

namespace TemplateMethodPattern;

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
        sb.AppendLine(index < total - 1 ? "  }," : "  }");
    }

    protected override void EndExport(StringBuilder sb, IReadOnlyList<SaleRecord> records)
        => sb.AppendLine("]");
}
