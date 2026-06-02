namespace BridgePattern;

// ============================================================
// ABSTRACTION (BASE)
// ============================================================
// Report is the "abstraction" side of the bridge.
// It holds a reference to IReportRenderer (the implementor)
// and delegates all output decisions to it.
//
// The critical insight: Report subclasses define WHAT data to show
// and in what logical structure. Renderers define HOW to format it.
// Neither side knows the concrete type of the other.
//
// Without Bridge, combining 3 report types × 3 formats would need
// 9 classes (SummaryPdf, SummaryHtml, SummaryCsv, DetailedPdf...).
// With Bridge: 3 + 3 = 6 classes, and adding a 4th format costs 1 class.

/// <summary>
/// Base abstraction. Subclasses define what data a report contains;
/// the injected <see cref="IReportRenderer"/> decides how to format it.
/// </summary>
public abstract class Report
{
    // The bridge — a reference to the implementor interface, not any concrete class.
    protected readonly IReportRenderer Renderer;

    protected Report(IReportRenderer renderer)
    {
        Renderer = renderer;
    }

    /// <summary>
    /// Generates the full report and returns the rendered output.
    /// Subclasses implement <see cref="BuildContent"/> to supply their data.
    /// </summary>
    public string Generate()
    {
        Renderer.BeginDocument();
        BuildContent();
        Renderer.EndDocument();
        return Renderer.GetOutput();
    }

    /// <summary>
    /// Template method: subclasses call Renderer.Render* methods
    /// to emit their specific content.
    /// </summary>
    protected abstract void BuildContent();
}
