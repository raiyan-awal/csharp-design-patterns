using System.Text;

namespace TemplateMethodPattern;

public abstract class ReportExporter
{
    // Template method — non-virtual so no subclass can override or reorder the steps
    public string Export(IReadOnlyList<SaleRecord> records)
    {
        var sb = new StringBuilder();
        BeginExport(sb, records);
        for (var i = 0; i < records.Count; i++)
            WriteRecord(sb, records[i], index: i, total: records.Count);
        EndExport(sb, records);
        return sb.ToString();
    }

    protected abstract void BeginExport(StringBuilder sb, IReadOnlyList<SaleRecord> records);
    protected abstract void WriteRecord(StringBuilder sb, SaleRecord record, int index, int total);

    // Hook — default is a no-op; subclasses may override to add a summary or close the document
    protected virtual void EndExport(StringBuilder sb, IReadOnlyList<SaleRecord> records) { }
}
