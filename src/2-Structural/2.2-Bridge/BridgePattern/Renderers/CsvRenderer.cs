using System.Text;

namespace BridgePattern.Renderers;

// ============================================================
// CONCRETE IMPLEMENTOR — CSV
// ============================================================
// Produces comma-separated values. Titles and sections become
// comment rows (# prefix). Rows become label,value pairs.
// Adding this format required zero changes to any report class.

/// <summary>
/// Renders reports as CSV. Structural elements (title, section)
/// are emitted as comment rows prefixed with #.
/// </summary>
public sealed class CsvRenderer : IReportRenderer
{
    private readonly StringBuilder _output = new();

    public string FormatName => "CSV";

    public void BeginDocument()
        => _output.AppendLine("# Report Export");

    public void EndDocument()
        => _output.AppendLine("# End of report");

    public void RenderTitle(string title)
        => _output.AppendLine($"# TITLE: {title}");

    public void RenderSection(string heading)
        => _output.AppendLine($"# SECTION: {heading}");

    public void RenderRow(string label, string value)
        => _output.AppendLine($"{EscapeCsv(label)},{EscapeCsv(value)}");

    public void RenderParagraph(string text)
        => _output.AppendLine($"# NOTE: {text}");

    public string GetOutput() => _output.ToString();

    // Wrap in quotes if the value contains a comma or quote.
    private static string EscapeCsv(string value)
        => value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
