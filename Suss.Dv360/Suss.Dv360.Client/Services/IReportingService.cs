using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides campaign reporting functionality using the Bid Manager API v2.
/// <para>
/// Supports generating, polling, and downloading reports for DV360 campaigns.
/// Reports include delivery metrics (impressions, clicks), performance KPIs
/// (CTR, CPC, CPM), and conversion data (total conversions, CPA, ROAS).
/// </para>
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generates a campaign report synchronously (handles polling internally).
    /// <para>
    /// This method creates a query, runs it, polls for completion,
    /// downloads the resulting CSV from GCS, and parses it into strongly-typed rows.
    /// Use this for simple one-off report requests where you want the full result.
    /// </para>
    /// </summary>
    /// <param name="request">The report request parameters including date range, grouping, and filters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The parsed report result with all data rows.</returns>
    /// <exception cref="TimeoutException">Thrown when the report does not complete within the timeout.</exception>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the Bid Manager API returns an error.</exception>
    Task<CampaignReportResult> GetCampaignReportAsync(
        CampaignReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and runs a query, returning the query ID for async workflows.
    /// <para>
    /// Use this method when you want to fire-and-forget or manage polling yourself.
    /// Call the polling service with the returned ID to wait for completion.
    /// </para>
    /// </summary>
    /// <param name="request">The report request parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The query ID in Bid Manager.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the Bid Manager API returns an error.</exception>
    Task<long> CreateAndRunQueryAsync(
        CampaignReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and parses a completed report from Google Cloud Storage.
    /// </summary>
    /// <param name="downloadUrl">The GCS URL from the report metadata.</param>
    /// <param name="groupBy">The grouping dimension used (for correct parsing).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The parsed report result.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when download fails.</exception>
    Task<CampaignReportResult> DownloadReportAsync(
        string downloadUrl,
        ReportGrouping groupBy,
        CancellationToken cancellationToken = default);
}
