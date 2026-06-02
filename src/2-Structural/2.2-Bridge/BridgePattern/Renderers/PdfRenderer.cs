using System.Text;

namespace BridgePattern.Renderers;

// ============================================================
// CONCRETE IMPLEMENTOR — PDF
// ============================================================
// Simulates PDF output using indented plain text.
// In a real system this would call a PDF library (e.g. iTextSharp).
// The report abstraction never sees this class — it only sees IReportRenderer.

/// <summary>
/// Renders reports in a PDF-like format (simulated as structured plain text).
/// </summary>
public sealed class PdfRenderer : IReportRenderer
{
    private readonly StringBuilder _output = new();

    public string FormatName => "PDF";

    public void BeginDocument()
        => _output.AppendLine("=== [PDF DOCUMENT START] ===");

    public void EndDocument()
        => _output.AppendLine("=== [PDF DOCUMENT END] ===");

    public void RenderTitle(string title)
    {
        _output.AppendLine();
        _output.AppendLine($"  *** {title.ToUpperInvariant()} ***");
        _output.AppendLine(new string('-', 48));
    }

    public void RenderSection(string heading)
    {
        _output.AppendLine();
        _output.AppendLine($"  [ {heading} ]");
    }

    public void RenderRow(string label, string value)
        => _output.AppendLine($"    {label,-24} {value}");

    public void RenderParagraph(string text)
    {
        _output.AppendLine();
        _output.AppendLine($"  {text}");
    }

    public string GetOutput() => _output.ToString();
}
