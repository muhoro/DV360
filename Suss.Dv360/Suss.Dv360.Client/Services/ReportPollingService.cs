using Google;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Suss.Dv360.Client.Configuration;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Polls Bid Manager API for report completion using exponential backoff.
/// <para>
/// After a query is run, the generated report is not immediately available.
/// This service polls the Queries.Reports endpoint until the report status
/// changes to <c>DONE</c>, indicating the file is ready for download from GCS.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory for creating authenticated Bid Manager service instances.</param>
/// <param name="options">Polling configuration (delays, max attempts, backoff).</param>
/// <param name="logger">Logger for diagnostic output.</param>
internal sealed class ReportPollingService(
    IBidManagerServiceFactory serviceFactory,
    IOptions<ReportPollingOptions> options,
    ILogger<ReportPollingService> logger) : IReportPollingService
{
    private readonly ReportPollingOptions _options = options.Value;

    /// <summary>
    /// Report status indicating the report is ready for download.
    /// </summary>
    private const string StatusDone = "DONE";

    /// <summary>
    /// Report status indicating the report failed to generate.
    /// </summary>
    private const string StatusFailed = "FAILED";

    /// <inheritdoc />
    public async Task<ReportPollResult> WaitForReportAsync(
        long queryId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Polling for query {QueryId} report completion", queryId);

        var service = await serviceFactory.CreateAsync(cancellationToken);
        var startTime = DateTimeOffset.UtcNow;
        var currentDelay = _options.InitialDelay;
        var attempt = 0;

        while (attempt < _options.MaxAttempts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if we've exceeded the maximum total wait time.
            var elapsed = DateTimeOffset.UtcNow - startTime;
            if (elapsed > _options.MaxTotalWait)
            {
                logger.LogWarning("Query {QueryId} report timed out after {Elapsed}", queryId, elapsed);
                throw new TimeoutException(
                    $"Query {queryId} report did not complete within {_options.MaxTotalWait.TotalMinutes} minutes.");
            }

            attempt++;
            logger.LogDebug("Polling attempt {Attempt} for query {QueryId}", attempt, queryId);

            try
            {
                // List reports for this query, get the latest one.
                var listRequest = service.Queries.Reports.List(queryId);
                listRequest.OrderBy = "key.reportId desc";
                listRequest.PageSize = 1;

                var reportList = await listRequest.ExecuteAsync(cancellationToken);

                if (reportList.Reports is { Count: > 0 })
                {
                    var report = reportList.Reports[0];
                    var status = report.Metadata?.Status?.State;

                    logger.LogDebug("Query {QueryId} report status: {Status}", queryId, status);

                    switch (status)
                    {
                        case StatusDone:
                            var downloadUrl = report.Metadata?.GoogleCloudStoragePath;
                            if (string.IsNullOrEmpty(downloadUrl))
                            {
                                throw new Dv360ApiException(
                                    $"Query {queryId} completed but no download URL available.");
                            }

                            var reportId = report.Key?.ReportId ?? 0;
                            logger.LogInformation(
                                "Query {QueryId} completed, report {ReportId} ready for download",
                                queryId, reportId);

                            return new ReportPollResult
                            {
                                ReportId = reportId,
                                DownloadUrl = downloadUrl
                            };

                        case StatusFailed:
                            throw new Dv360ApiException($"Query {queryId} report failed to generate.");

                        // For RUNNING or QUEUED states, continue polling.
                    }
                }
            }
            catch (GoogleApiException ex)
            {
                logger.LogError(ex, "Error polling query {QueryId} report", queryId);
                throw new Dv360ApiException($"Failed to poll query {queryId} report status.", ex);
            }

            // Wait before next attempt with exponential backoff.
            logger.LogDebug("Waiting {Delay} before next poll attempt", currentDelay);
            await Task.Delay(currentDelay, cancellationToken);

            // Increase delay for next iteration, up to the maximum.
            currentDelay = TimeSpan.FromMilliseconds(
                Math.Min(currentDelay.TotalMilliseconds * _options.BackoffMultiplier,
                         _options.MaxDelay.TotalMilliseconds));
        }

        throw new TimeoutException(
            $"Query {queryId} report did not complete within {_options.MaxAttempts} polling attempts.");
    }
}
