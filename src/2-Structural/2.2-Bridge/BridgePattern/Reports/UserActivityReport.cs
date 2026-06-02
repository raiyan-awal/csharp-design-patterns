namespace BridgePattern.Reports;

// ============================================================
// REFINED ABSTRACTION #3 — User Activity Report
// ============================================================
// Tracks user engagement metrics: active users, sessions,
// feature usage. A third independent report type that works
// with all three renderers without modification.

/// <summary>
/// A report on user engagement and activity metrics.
/// </summary>
public sealed class UserActivityReport : Report
{
    private readonly UserActivityData _data;

    public UserActivityReport(IReportRenderer renderer, UserActivityData data)
        : base(renderer)
    {
        _data = data;
    }

    protected override void BuildContent()
    {
        Renderer.RenderTitle($"User Activity Report — {_data.Period}");

        Renderer.RenderSection("Engagement Metrics");
        Renderer.RenderRow("Daily Active Users",   _data.DailyActiveUsers.ToString("N0"));
        Renderer.RenderRow("Monthly Active Users", _data.MonthlyActiveUsers.ToString("N0"));
        Renderer.RenderRow("Avg Session Duration", $"{_data.AverageSessionMinutes:F1} min");
        Renderer.RenderRow("Retention Rate",       $"{_data.RetentionRate:P1}");

        Renderer.RenderSection("Top Features");
        foreach (var feature in _data.TopFeatures)
            Renderer.RenderRow(feature.Name, $"{feature.UsageCount:N0} uses");

        Renderer.RenderParagraph($"Churn risk: {_data.ChurnRiskUsers:N0} users inactive for 30+ days.");
    }
}

/// <summary>Data containers for <see cref="UserActivityReport"/>.</summary>
public sealed class UserActivityData
{
    public string Period                 { get; init; } = string.Empty;
    public int    DailyActiveUsers       { get; init; }
    public int    MonthlyActiveUsers     { get; init; }
    public double AverageSessionMinutes  { get; init; }
    public double RetentionRate          { get; init; }
    public int    ChurnRiskUsers         { get; init; }
    public List<FeatureUsage> TopFeatures { get; init; } = [];
}

public sealed class FeatureUsage
{
    public string Name       { get; init; } = string.Empty;
    public int    UsageCount { get; init; }
}
