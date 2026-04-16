using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Api.Models;

/// <summary>
/// Request body for creating and running a Bid Manager report.
/// The <c>advertiserId</c> is taken from the route parameter.
/// </summary>
public sealed class CreateReportQueryRequest
{
    /// <summary>Date range for the report. Use static helpers: <c>ReportDateRange.Last30Days</c>, etc.</summary>
    public required ReportDateRange DateRange { get; set; }

    /// <summary>Primary dimension to group results by. Defaults to <c>Date</c>.</summary>
    public ReportGrouping GroupBy { get; set; } = ReportGrouping.Date;

    /// <summary>Scope the report to a specific campaign. Null returns all campaigns for the advertiser.</summary>
    public long? CampaignId { get; set; }

    /// <summary>Scope the report to a specific line item.</summary>
    public long? LineItemId { get; set; }

    /// <summary>
    /// Bid Manager metric names to include. Null uses the default set (impressions, clicks,
    /// media cost, revenue, CTR, CPM, CPC, conversions, video metrics).
    /// </summary>
    public IReadOnlyList<string>? Metrics { get; set; }

    /// <summary>
    /// Additional Bid Manager dimension filter names to include as columns,
    /// e.g. <c>"FILTER_DEVICE_TYPE"</c>.
    /// </summary>
    public IReadOnlyList<string>? AdditionalDimensions { get; set; }

    /// <summary>Optional title for the Bid Manager query. Auto-generated with a timestamp if null.</summary>
    public string? QueryTitle { get; set; }
}
