// =============================================================================
// Suss.Dv360.Console – Campaign Reporting Demo
//
// Demonstrates the Phase 2 campaign reporting workflow using Bid Manager API:
//   1. Request a campaign report with filters and grouping
//   2. Poll for report completion (handled internally)
//   3. Download and parse the report data from GCS
//   4. Display metrics (impressions, clicks, CTR, conversions, etc.)
//
// Usage: dotnet run -- report [advertiserId] [campaignId]
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Console;

/// <summary>
/// Demonstrates the DV360 Bid Manager reporting workflow.
/// </summary>
public static class ReportingDemo
{
    /// <summary>
    /// Runs the reporting demo with the specified parameters.
    /// </summary>
    public static async Task RunAsync(
        IServiceProvider services,
        long advertiserId,
        long? campaignId = null)
    {
        using var scope = services.CreateScope();
        var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("═══════════════════════════════════════════════════════════════");
        logger.LogInformation("  DV360 Campaign Reporting Demo (Bid Manager API)");
        logger.LogInformation("═══════════════════════════════════════════════════════════════");
        logger.LogInformation("  Advertiser ID: {AdvertiserId}", advertiserId);
        if (campaignId.HasValue)
            logger.LogInformation("  Campaign ID:   {CampaignId}", campaignId);
        logger.LogInformation("═══════════════════════════════════════════════════════════════");

        // ─────────────────────────────────────────────────────────────────────
        // Build report request
        // ─────────────────────────────────────────────────────────────────────
        logger.LogInformation("\n📊 Building campaign report request...");

        var request = new CampaignReportRequest
        {
            AdvertiserId = advertiserId,
            CampaignId   = campaignId,
            DateRange    = new ReportDateRange { RelativeDateRange = "LAST_30_DAYS" },

            // Group by domain to see which websites the campaign ran on.
            // NOTE: FILTER_DOMAIN only supports raw count metrics — CTR and conversions
            // are calculated fields that the API rejects at domain-level granularity.
            // FILTER_APP_URL is the dimension DV360 UI uses for placement/website breakdown.
            // Cost metrics are excluded — all currency metrics require a currency GroupBy
            // dimension that is incompatible with placement-level dimensions.
            GroupBy = ReportGrouping.AppUrl,
            Metrics =
            [
                "METRIC_IMPRESSIONS",
                "METRIC_CLICKS"
            ],
            QueryTitle = $"Campaign Website Report - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
        };

        logger.LogInformation("  Date Range: {DateRange}", request.DateRange.RelativeDateRange);
        logger.LogInformation("  Group By:   {GroupBy}", request.GroupBy);

        // ─────────────────────────────────────────────────────────────────────
        // Execute report (create query, run, poll, download, parse)
        // ─────────────────────────────────────────────────────────────────────
        logger.LogInformation("\n⏳ Executing report (this may take a moment)...");

        try
        {
            var result = await reportingService.GetCampaignReportAsync(request);

            logger.LogInformation("\n✅ Report generated successfully!");
            logger.LogInformation("  Generated At: {GeneratedAt:yyyy-MM-dd HH:mm:ss}", result.GeneratedAt);
            logger.LogInformation("  Row Count:    {RowCount}", result.Rows.Count);

            // ─────────────────────────────────────────────────────────────────
            // Display report data
            // ─────────────────────────────────────────────────────────────────
            if (result.Rows.Count > 0)
            {
                logger.LogInformation("\n🌐 Website Performance Breakdown (top 20 by impressions):");
                logger.LogInformation("┌────────────────────────────────────────────────────┬─────────────┬──────────┐");
                logger.LogInformation("│ App/URL                                            │ Impressions │ Clicks   │");
                logger.LogInformation("├────────────────────────────────────────────────────┼─────────────┼──────────┤");

                foreach (var row in result.Rows.OrderByDescending(r => r.Impressions).Take(20))
                {
                    var site = (row.AppUrl ?? row.Domain ?? "Unknown");
                    var siteDisplay = site.Length > 50 ? site[..50] : site;
                    logger.LogInformation("│ {Site,-50} │ {Impressions,11:N0} │ {Clicks,8:N0} │",
                        siteDisplay,
                        row.Impressions,
                        row.Clicks);
                }

                if (result.Rows.Count > 20)
                    logger.LogInformation("│ ... {More,4} more sites                                                  │", result.Rows.Count - 20);

                logger.LogInformation("└────────────────────────────────────────────────────┴─────────────┴──────────┘");

                var totalImpressions = result.Rows.Sum(r => r.Impressions);
                var totalClicks      = result.Rows.Sum(r => r.Clicks);

                logger.LogInformation("\n📊 Summary Totals:");
                logger.LogInformation("  Total Sites:        {Total:N0}", result.Rows.Count);
                logger.LogInformation("  Total Impressions:  {Total:N0}", totalImpressions);
                logger.LogInformation("  Total Clicks:       {Total:N0}", totalClicks);
                logger.LogInformation("  Overall CTR:        {CTR:P2}", totalClicks / (double)Math.Max(totalImpressions, 1));
            }
            else
            {
                logger.LogWarning("No data returned for the specified campaign and date range.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate report");
            throw;
        }

        logger.LogInformation("\n═══════════════════════════════════════════════════════════════");
        logger.LogInformation("  Reporting Demo Complete");
        logger.LogInformation("═══════════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Demonstrates the async workflow where you create/run a query and poll separately.
    /// Useful for long-running reports or when you want more control.
    /// </summary>
    public static async Task RunAsyncWorkflowAsync(
        IServiceProvider services,
        long advertiserId,
        long? campaignId = null)
    {
        using var scope = services.CreateScope();
        var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
        var pollingService = scope.ServiceProvider.GetRequiredService<IReportPollingService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("📊 Async Reporting Workflow Demo");

        var request = new CampaignReportRequest
        {
            AdvertiserId = advertiserId,
            CampaignId = campaignId,
            DateRange = new ReportDateRange { RelativeDateRange = "LAST_7_DAYS" },
            GroupBy = ReportGrouping.LineItem
        };

        // Step 1: Create and run the query (returns query ID immediately)
        logger.LogInformation("Creating and running query...");
        var queryId = await reportingService.CreateAndRunQueryAsync(request);
        logger.LogInformation("Query created with ID: {QueryId}", queryId);

        // Step 2: Poll for completion (you could do other work here)
        logger.LogInformation("Polling for completion...");
        
        try
        {
            var pollResult = await pollingService.WaitForReportAsync(queryId);
            
            // Step 3: Download when ready
            logger.LogInformation("Report ready! Downloading from GCS...");
            var result = await reportingService.DownloadReportAsync(
                pollResult.DownloadUrl, request.GroupBy);

            logger.LogInformation("Downloaded {RowCount} rows", result.Rows.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report generation failed");
        }
    }
}
