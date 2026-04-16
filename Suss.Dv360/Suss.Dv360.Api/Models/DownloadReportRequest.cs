using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Api.Models;

/// <summary>Request body for downloading and parsing a completed Bid Manager report from GCS.</summary>
public sealed class DownloadReportRequest
{
    /// <summary>The Google Cloud Storage URL of the completed report CSV.</summary>
    public required string DownloadUrl { get; set; }

    /// <summary>Grouping dimension used when the report was created (needed for correct CSV parsing).</summary>
    public ReportGrouping GroupBy { get; set; } = ReportGrouping.Date;
}
