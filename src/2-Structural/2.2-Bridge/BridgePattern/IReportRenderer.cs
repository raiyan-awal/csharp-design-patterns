namespace BridgePattern;

// ============================================================
// IMPLEMENTOR INTERFACE
// ============================================================
// This is the "implementation" side of the bridge.
// It defines how output is produced — completely independent
// of what kind of report is being generated.
//
// Concrete implementors (PdfRenderer, HtmlRenderer, CsvRenderer)
// each fulfil this contract in their own way.
// The report abstraction talks to this interface only — it never
// knows which renderer is underneath.

/// <summary>
/// Defines the rendering operations that every output format must support.
/// This is the implementor interface in the Bridge pattern.
/// </summary>
public interface IReportRenderer
{
    /// <summary>Human-readable name of this output format, e.g. "PDF".</summary>
    string FormatName { get; }

    /// <summary>Emit the document title.</summary>
    void RenderTitle(string title);

    /// <summary>Emit a section heading.</summary>
    void RenderSection(string heading);

    /// <summary>Emit a key-value data row.</summary>
    void RenderRow(string label, string value);

    /// <summary>Emit a plain paragraph of text.</summary>
    void RenderParagraph(string text);

    /// <summary>
    /// Called once before any other render calls.
    /// Use for format-specific preamble (e.g. HTML doctype, PDF header).
    /// </summary>
    void BeginDocument();

    /// <summary>
    /// Called once after all render calls.
    /// Use for format-specific epilogue (e.g. closing HTML tags).
    /// </summary>
    void EndDocument();

    /// <summary>
    /// Returns the complete rendered output as a string.
    /// </summary>
    string GetOutput();
}
