using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides polling functionality for waiting on asynchronous report generation.
/// <para>
/// Report generation in Bid Manager API is asynchronous. After running a query,
/// the service must poll the Queries.Reports resource to check when the report
/// is ready for download. This interface abstracts the polling logic for testability.
/// </para>
/// </summary>
public interface IReportPollingService
{
    /// <summary>
    /// Polls for a Bid Manager report to become available for download.
    /// </summary>
    /// <param name="queryId">The Bid Manager query ID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// The result containing the Google Cloud Storage download URL when the report is ready.
    /// </returns>
    /// <exception cref="TimeoutException">
    /// Thrown when the report does not complete within the configured timeout.
    /// </exception>
    /// <exception cref="Exceptions.Dv360ApiException">
    /// Thrown when the report fails or an API error occurs.
    /// </exception>
    Task<ReportPollResult> WaitForReportAsync(
        long queryId,
        CancellationToken cancellationToken = default);
}
