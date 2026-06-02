using System.Text;

namespace BridgePattern.Renderers;

// ============================================================
// CONCRETE IMPLEMENTOR — HTML
// ============================================================
// Produces actual HTML markup. Each render call emits the
// appropriate HTML element. The report abstraction is unaware
// of any of this — it just calls the same IReportRenderer API.

/// <summary>
/// Renders reports as HTML markup.
/// </summary>
public sealed class HtmlRenderer : IReportRenderer
{
    private readonly StringBuilder _output = new();

    public string FormatName => "HTML";

    public void BeginDocument()
    {
        _output.AppendLine("<!DOCTYPE html>");
        _output.AppendLine("<html><body>");
    }

    public void EndDocument()
        => _output.AppendLine("</body></html>");

    public void RenderTitle(string title)
        => _output.AppendLine($"  <h1>{title}</h1>");

    public void RenderSection(string heading)
        => _output.AppendLine($"  <h2>{heading}</h2>");

    public void RenderRow(string label, string value)
        => _output.AppendLine($"  <p><strong>{label}:</strong> {value}</p>");

    public void RenderParagraph(string text)
        => _output.AppendLine($"  <p>{text}</p>");

    public string GetOutput() => _output.ToString();
}
