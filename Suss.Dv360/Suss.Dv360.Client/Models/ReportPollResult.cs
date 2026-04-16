namespace Suss.Dv360.Client.Models;

/// <summary>
/// Result of polling for a Bid Manager report completion.
/// </summary>
public sealed record ReportPollResult
{
    /// <summary>
    /// The Bid Manager report ID.
    /// </summary>
    public required long ReportId { get; init; }

    /// <summary>
    /// The Google Cloud Storage URL where the report file can be downloaded.
    /// </summary>
    public required string DownloadUrl { get; init; }
}
