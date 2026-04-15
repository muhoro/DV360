namespace Suss.Dv360.Client.Models;

/// <summary>
/// Contains the results of a campaign report request.
/// <para>
/// Includes metadata about the report (date range, row count) and the
/// flattened row data. All Google SDK types are hidden from consumers.
/// </para>
/// </summary>
public sealed class CampaignReportResult
{
    /// <summary>
    /// The report definition ID in Campaign Manager 360.
    /// Can be used to re-run the same report or download it again.
    /// </summary>
    public long ReportId { get; set; }

    /// <summary>
    /// The report file ID. Used for downloading the report file.
    /// </summary>
    public long FileId { get; set; }

    /// <summary>
    /// The name of the report as stored in Campaign Manager 360.
    /// </summary>
    public string? ReportName { get; set; }

    /// <summary>
    /// The date range that was used for the report.
    /// </summary>
    public ReportDateRange? DateRange { get; set; }

    /// <summary>
    /// The primary grouping dimension used in the report.
    /// </summary>
    public ReportGrouping GroupBy { get; set; }

    /// <summary>
    /// The total number of rows in the report.
    /// </summary>
    public int RowCount => Rows?.Count ?? 0;

    /// <summary>
    /// The report data rows, each containing metrics for the grouping dimension.
    /// </summary>
    public IReadOnlyList<CampaignReportRow> Rows { get; set; } = [];

    /// <summary>
    /// Summary totals across all rows (when available).
    /// </summary>
    public CampaignReportRow? Totals { get; set; }

    /// <summary>
    /// The time zone used for date-based data in the report.
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTimeOffset? GeneratedAt { get; set; }

    /// <summary>
    /// The currency code used for monetary values (e.g., "USD").
    /// </summary>
    public string? CurrencyCode { get; set; }
}
