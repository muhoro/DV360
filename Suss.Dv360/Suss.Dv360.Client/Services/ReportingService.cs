using System.Net.Http;
using Google;
using Google.Apis.DoubleClickBidManager.v2.Data;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Implementation of <see cref="IReportingService"/> using Bid Manager API v2.
/// <para>
/// Handles the full report lifecycle: creating queries, running them,
/// polling for completion, downloading CSV files from GCS, and parsing into typed results.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory for authenticated Bid Manager service instances.</param>
/// <param name="pollingService">Service for waiting on report completion.</param>
/// <param name="reportParser">Parser for converting CSV to typed rows.</param>
/// <param name="httpClientFactory">Factory for creating HTTP clients for GCS downloads.</param>
/// <param name="logger">Logger for diagnostic output.</param>
internal sealed class ReportingService(
    IBidManagerServiceFactory serviceFactory,
    IReportPollingService pollingService,
    IReportParser reportParser,
    IHttpClientFactory httpClientFactory,
    ILogger<ReportingService> logger) : IReportingService
{
    /// <summary>
    /// Default metrics included when <see cref="CampaignReportRequest.Metrics"/> is not set.
    /// Covers the core delivery, performance, and cost KPIs for standard display campaigns.
    /// </summary>
    /// <remarks>
    /// Override by setting <see cref="CampaignReportRequest.Metrics"/> on the request.
    /// Full metric reference: https://developers.google.com/bid-manager/reference/rest/v2/filters-metrics#metrics
    /// <list type="table">
    ///   <listheader><term>Metric</term><description>CSV column</description></listheader>
    ///   <item><term>METRIC_IMPRESSIONS</term><description>Impressions</description></item>
    ///   <item><term>METRIC_CLICKS</term><description>Clicks</description></item>
    ///   <item><term>METRIC_CTR</term><description>Click Rate (CTR)</description></item>
    ///   <item><term>METRIC_TOTAL_CONVERSIONS</term><description>Total Conversions</description></item>
    ///   <item><term>METRIC_MEDIA_COST_ADVERTISER</term><description>Media Cost (Advertiser Currency)</description></item>
    /// </list>
    /// </remarks>
    private static readonly string[] DefaultMetrics =
    [
        "METRIC_IMPRESSIONS",
        "METRIC_CLICKS",
        "METRIC_CTR",
        "METRIC_TOTAL_CONVERSIONS",
        "METRIC_MEDIA_COST_ADVERTISER"
    ];

    /// <inheritdoc />
    public async Task<CampaignReportResult> GetCampaignReportAsync(
        CampaignReportRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating campaign report for advertiser {AdvertiserId}", request.AdvertiserId);

        // Create and run the query
        var queryId = await CreateAndRunQueryAsync(request, cancellationToken);

        // Poll for completion
        var pollResult = await pollingService.WaitForReportAsync(queryId, cancellationToken);

        // Download and parse
        return await DownloadReportAsync(
            pollResult.DownloadUrl,
            request.GroupBy,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CreateAndRunQueryAsync(
        CampaignReportRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating query for advertiser {AdvertiserId}", request.AdvertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);

            // Build the query definition
            var query = BuildQueryDefinition(request);

            // Log the exact query being sent to aid debugging invalid-combination errors
            logger.LogDebug("Query type:    {Type}", query.Params__?.Type);
            logger.LogDebug("Query groupBys: [{GroupBys}]", string.Join(", ", query.Params__?.GroupBys ?? []));
            logger.LogDebug("Query metrics:  [{Metrics}]", string.Join(", ", query.Params__?.Metrics ?? []));
            logger.LogDebug("Query filters:  [{Filters}]", string.Join(", ", query.Params__?.Filters?.Select(f => $"{f.Type}={f.Value}") ?? []));
            logger.LogDebug("Query range:    {Range}", query.Metadata?.DataRange?.Range);

            // Create the query
            var createRequest = service.Queries.Create(query);
            var createdQuery = await createRequest.ExecuteAsync(cancellationToken);
            var queryId = createdQuery.QueryId!.Value;

            logger.LogInformation("Created query {QueryId}, running...", queryId);

            // Run the query
            var runRequest = service.Queries.Run(new RunQueryRequest
            {
                DataRange = BuildDataRange(request.DateRange)
            }, queryId);
            await runRequest.ExecuteAsync(cancellationToken);

            logger.LogInformation("Query {QueryId} started", queryId);
            return queryId;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to create/run query for advertiser {AdvertiserId}", request.AdvertiserId);
            throw new Dv360ApiException($"Failed to create query for advertiser {request.AdvertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CampaignReportResult> DownloadReportAsync(
        string downloadUrl,
        ReportGrouping groupBy,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading report from {Url}", downloadUrl);

        try
        {
            // Download the CSV from GCS using the provided URL
            var httpClient = httpClientFactory.CreateClient("BidManagerReport");

            // GCS URLs from Bid Manager require authentication
            var service = await serviceFactory.CreateAsync(cancellationToken);

            using var response = await httpClient.GetAsync(downloadUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var csvContent = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogDebug("Downloaded {Length} characters from report", csvContent.Length);

            // Parse the CSV
            var rows = reportParser.Parse(csvContent, groupBy);

            // Build result
            var result = new CampaignReportResult
            {
                ReportId = 0, // Not applicable for URL-based downloads
                FileId = 0,
                GroupBy = groupBy,
                Rows = rows,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            // Calculate totals
            if (rows.Count > 0)
            {
                result.Totals = CalculateTotals(rows);
            }

            logger.LogInformation("Report completed with {RowCount} rows", result.RowCount);

            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to download report from {Url}", downloadUrl);
            throw new Dv360ApiException($"Failed to download report from {downloadUrl}.", ex);
        }
    }

    /// <summary>
    /// Builds a Bid Manager query definition from the request.
    /// </summary>
    private Query BuildQueryDefinition(CampaignReportRequest request)
    {
        var queryTitle = request.QueryTitle
            ?? $"Campaign Report {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

        var query = new Query
        {
            Metadata = new QueryMetadata
            {
                Title = queryTitle,
                DataRange = BuildDataRange(request.DateRange),
                Format = "CSV"
            },
            Params__ = new Parameters
            {
                Type = "STANDARD",
                GroupBys = BuildGroupBys(request),
                Metrics = BuildMetrics(request),
                Filters = BuildFilters(request)
            },
            Schedule = new QuerySchedule
            {
                Frequency = "ONE_TIME"
            }
        };

        return query;
    }

    /// <summary>
    /// Builds the data range for the query.
    /// </summary>
    private static DataRange BuildDataRange(ReportDateRange dateRange)
    {
        var dataRange = new DataRange();

        if (!string.IsNullOrEmpty(dateRange.RelativeDateRange))
        {
            // Map common relative ranges to Bid Manager format
            dataRange.Range = dateRange.RelativeDateRange.ToUpperInvariant() switch
            {
                // Friendly aliases
                "CURRENT_DAY"      => "CURRENT_DAY",
                "PREVIOUS_DAY"     => "PREVIOUS_DAY",
                "CURRENT_WEEK"     => "WEEK_TO_DATE",
                "CURRENT_MONTH"    => "MONTH_TO_DATE",
                "CURRENT_QUARTER"  => "QUARTER_TO_DATE",
                "CURRENT_YEAR"     => "YEAR_TO_DATE",
                // Direct API values (pass through)
                "WEEK_TO_DATE"     => "WEEK_TO_DATE",
                "MONTH_TO_DATE"    => "MONTH_TO_DATE",
                "QUARTER_TO_DATE"  => "QUARTER_TO_DATE",
                "YEAR_TO_DATE"     => "YEAR_TO_DATE",
                "PREVIOUS_WEEK"    => "PREVIOUS_WEEK",
                "PREVIOUS_MONTH"   => "PREVIOUS_MONTH",
                "PREVIOUS_QUARTER" => "PREVIOUS_QUARTER",
                "PREVIOUS_YEAR"    => "PREVIOUS_YEAR",
                "LAST_7_DAYS"      => "LAST_7_DAYS",
                "LAST_14_DAYS"     => "LAST_14_DAYS",
                "LAST_30_DAYS"     => "LAST_30_DAYS",
                "LAST_60_DAYS"     => "LAST_60_DAYS",
                "LAST_90_DAYS"     => "LAST_90_DAYS",
                "LAST_365_DAYS"    => "LAST_365_DAYS",
                "ALL_TIME"         => "ALL_TIME",
                _                  => "LAST_7_DAYS"
            };
        }
        else if (dateRange.StartDate.HasValue && dateRange.EndDate.HasValue)
        {
            dataRange.Range = "CUSTOM_DATES";
            dataRange.CustomStartDate = new Date
            {
                Year = dateRange.StartDate.Value.Year,
                Month = dateRange.StartDate.Value.Month,
                Day = dateRange.StartDate.Value.Day
            };
            dataRange.CustomEndDate = new Date
            {
                Year = dateRange.EndDate.Value.Year,
                Month = dateRange.EndDate.Value.Month,
                Day = dateRange.EndDate.Value.Day
            };
        }
        else
        {
            dataRange.Range = "LAST_7_DAYS";
        }

        return dataRange;
    }

    /// <summary>
    /// Builds the GroupBy dimensions list sent in the Bid Manager query <c>Parameters.groupBys</c> field.
    /// </summary>
    /// <remarks>
    /// Always includes:
    /// <list type="number">
    ///   <item>The primary dimension from <see cref="CampaignReportRequest.GroupBy"/></item>
    ///   <item><c>FILTER_ADVERTISER_CURRENCY</c> — required by the API when cost metrics are requested</item>
    ///   <item>Any additional dimensions from <see cref="CampaignReportRequest.AdditionalDimensions"/></item>
    /// </list>
    /// Full dimension reference: https://developers.google.com/bid-manager/reference/rest/v2/filters-metrics#filters
    /// </remarks>
    private static IList<string> BuildGroupBys(CampaignReportRequest request)
    {
        var groupBys = new List<string>();

        // Primary grouping dimension — controls the main breakdown of report rows.
        // ReportGrouping enum → Bid Manager FILTER_* mapping:
        //   Date           → FILTER_DATE               (CSV: "Date")
        //   Campaign       → FILTER_MEDIA_PLAN          (CSV: "Campaign ID", "Campaign")
        //   Advertiser     → FILTER_ADVERTISER          (CSV: "Advertiser ID", "Advertiser")
        //   InsertionOrder → FILTER_INSERTION_ORDER     (CSV: "Insertion Order ID", "Insertion Order")
        //   LineItem       → FILTER_LINE_ITEM           (CSV: "Line Item ID", "Line Item")
        //   Creative       → FILTER_CREATIVE_ID         (CSV: "Creative ID", "Creative")
        //   Exchange       → FILTER_EXCHANGE            (CSV: "Exchange")
        var primaryDimension = request.GroupBy switch
        {
            ReportGrouping.Date           => "FILTER_DATE",
            ReportGrouping.Campaign       => "FILTER_MEDIA_PLAN",
            ReportGrouping.Advertiser     => "FILTER_ADVERTISER",
            ReportGrouping.InsertionOrder => "FILTER_INSERTION_ORDER",
            ReportGrouping.LineItem       => "FILTER_LINE_ITEM",
            ReportGrouping.Creative       => "FILTER_CREATIVE_ID",
            ReportGrouping.Exchange       => "FILTER_EXCHANGE",
            ReportGrouping.Domain         => "FILTER_DOMAIN",
            ReportGrouping.AppUrl         => "FILTER_APP_URL",
            _                             => "FILTER_DATE"
        };

        groupBys.Add(primaryDimension);

        // FILTER_ADVERTISER_CURRENCY is required when cost metrics are requested, BUT only
        // for entity/time-based dimensions (Date, Campaign, IO, LineItem, etc.).
        // Placement-level dimensions (Domain, AppUrl, Exchange) are incompatible with it —
        // the API returns BadRequest if it is included alongside those dimensions.
        var requiresCurrencyDimension = request.GroupBy is not (ReportGrouping.Domain or ReportGrouping.AppUrl or ReportGrouping.Exchange);
        if (requiresCurrencyDimension)
            groupBys.Add("FILTER_ADVERTISER_CURRENCY");

        // Additional dimensions from the request (deduplicated against primary).
        // Accepts friendly aliases or raw FILTER_* names — see MapDimensionName().
        // Example additional dimensions:
        //   "FILTER_DEVICE_TYPE"         → CSV: "Device Type"
        //   "FILTER_BROWSER"             → CSV: "Browser"
        //   "FILTER_COUNTRY"             → CSV: "Country"
        //   "FILTER_REGION_NAME"         → CSV: "Region"
        //   "FILTER_CITY_NAME"           → CSV: "City"
        //   "FILTER_OS"                  → CSV: "Operating System"
        //   "FILTER_PAGE_LAYOUT"         → CSV: "Environment"
        //   "FILTER_DAY_OF_WEEK"         → CSV: "Day of Week"
        //   "FILTER_WEEK"                → CSV: "Week"
        //   "FILTER_MONTH"               → CSV: "Month"
        //   "FILTER_MEDIA_PLAN_NAME"     → CSV: "Campaign"    (use when GroupBy != Campaign)
        //   "FILTER_LINE_ITEM_NAME"      → CSV: "Line Item"   (use when GroupBy != LineItem)
        //   "FILTER_INSERTION_ORDER_NAME"→ CSV: "Insertion Order"
        if (request.AdditionalDimensions is not null)
        {
            foreach (var dim in request.AdditionalDimensions)
            {
                var mapped = MapDimensionName(dim);
                if (!string.Equals(mapped, primaryDimension, StringComparison.OrdinalIgnoreCase))
                    groupBys.Add(mapped);
            }
        }

        return groupBys;
    }

    /// <summary>
    /// Maps friendly short names to Bid Manager <c>FILTER_*</c> values.
    /// Raw <c>FILTER_*</c> values are passed through unchanged.
    /// </summary>
    /// <remarks>
    /// Friendly aliases accepted:
    /// <list type="table">
    ///   <listheader><term>Alias</term><description>Mapped to</description></listheader>
    ///   <item><term>DATE</term><description>FILTER_DATE</description></item>
    ///   <item><term>CAMPAIGN</term><description>FILTER_MEDIA_PLAN</description></item>
    ///   <item><term>ADVERTISER</term><description>FILTER_ADVERTISER</description></item>
    ///   <item><term>INSERTION_ORDER / INSERTIONORDER</term><description>FILTER_INSERTION_ORDER</description></item>
    ///   <item><term>LINE_ITEM / LINEITEM</term><description>FILTER_LINE_ITEM</description></item>
    ///   <item><term>CREATIVE</term><description>FILTER_CREATIVE_ID</description></item>
    ///   <item><term>EXCHANGE</term><description>FILTER_EXCHANGE</description></item>
    /// </list>
    /// Any value already prefixed with <c>FILTER_</c> is returned as-is, allowing callers
    /// to pass any dimension from the full API reference directly.
    /// </remarks>
    private static string MapDimensionName(string dimension)
    {
        return dimension.ToUpperInvariant() switch
        {
            "DATE"                          => "FILTER_DATE",
            "CAMPAIGN"                      => "FILTER_MEDIA_PLAN",
            "ADVERTISER"                    => "FILTER_ADVERTISER",
            "INSERTION_ORDER"
                or "INSERTIONORDER"         => "FILTER_INSERTION_ORDER",
            "LINE_ITEM"
                or "LINEITEM"               => "FILTER_LINE_ITEM",
            "CREATIVE"                      => "FILTER_CREATIVE_ID",
            "EXCHANGE"                      => "FILTER_EXCHANGE",
            _ when dimension.StartsWith("FILTER_", StringComparison.OrdinalIgnoreCase)
                                            => dimension, // already a valid API filter name
            _                               => dimension  // pass through unknown values
        };
    }

    /// <summary>
    /// Builds the metrics list from the request.
    /// </summary>
    private static IList<string> BuildMetrics(CampaignReportRequest request)
    {
        if (request.Metrics is not null && request.Metrics.Count > 0)
        {
            // Ensure metrics are in METRIC_ format
            return request.Metrics
                .Select(m => m.StartsWith("METRIC_") ? m : $"METRIC_{m.ToUpperInvariant()}")
                .ToList();
        }

        return [..DefaultMetrics];
    }

    /// <summary>
    /// Builds filter pairs from the request filters.
    /// </summary>
    private static IList<FilterPair>? BuildFilters(CampaignReportRequest request)
    {
        var filters = new List<FilterPair>();

        // Add advertiser filter (required)
        filters.Add(new FilterPair
        {
            Type = "FILTER_ADVERTISER",
            Value = request.AdvertiserId.ToString()
        });

        // Add campaign filter if specified
        if (request.CampaignId.HasValue)
        {
            filters.Add(new FilterPair
            {
                Type = "FILTER_MEDIA_PLAN",
                Value = request.CampaignId.Value.ToString()
            });
        }

        // Add line item filter if specified
        if (request.LineItemId.HasValue)
        {
            filters.Add(new FilterPair
            {
                Type = "FILTER_LINE_ITEM",
                Value = request.LineItemId.Value.ToString()
            });
        }

        // Add filters from ReportFilters
        if (request.Filters is not null)
        {
            AddIdFilters(filters, "FILTER_MEDIA_PLAN", request.Filters.CampaignIds);
            AddIdFilters(filters, "FILTER_ADVERTISER", request.Filters.AdvertiserIds);
            AddIdFilters(filters, "FILTER_LINE_ITEM", request.Filters.LineItemIds);
            AddIdFilters(filters, "FILTER_INSERTION_ORDER", request.Filters.InsertionOrderIds);
            AddIdFilters(filters, "FILTER_CREATIVE_ID", request.Filters.CreativeIds);
            AddIdFilters(filters, "FILTER_EXCHANGE", request.Filters.ExchangeIds);
        }

        return filters.Count > 0 ? filters : null;
    }

    /// <summary>
    /// Adds ID-based filter pairs for a list of IDs.
    /// </summary>
    private static void AddIdFilters(
        List<FilterPair> filters,
        string filterType,
        IReadOnlyList<long>? ids)
    {
        if (ids is null || ids.Count == 0)
            return;

        foreach (var id in ids)
        {
            filters.Add(new FilterPair
            {
                Type = filterType,
                Value = id.ToString()
            });
        }
    }

    /// <summary>
    /// Calculates summary totals across all report rows.
    /// </summary>
    private static CampaignReportRow CalculateTotals(IReadOnlyList<CampaignReportRow> rows)
    {
        var totals = new CampaignReportRow();

        foreach (var row in rows)
        {
            totals.Impressions += row.Impressions;
            totals.Clicks += row.Clicks;
            totals.MediaCostMicros += row.MediaCostMicros;
            totals.RevenueMicros += row.RevenueMicros;
            totals.TotalConversions += row.TotalConversions;
            totals.PostClickConversions += row.PostClickConversions;
            totals.PostViewConversions += row.PostViewConversions;
            totals.PostClickRevenueMicros += row.PostClickRevenueMicros;
            totals.PostViewRevenueMicros += row.PostViewRevenueMicros;
            totals.VideoViews += row.VideoViews;
            totals.VideoCompletions += row.VideoCompletions;
        }

        // Calculate derived metrics for totals
        if (totals.Impressions > 0)
        {
            totals.Ctr = (decimal)totals.Clicks / totals.Impressions;
            totals.CpmMicros = totals.MediaCostMicros * 1000 / totals.Impressions;
        }

        if (totals.Clicks > 0)
        {
            totals.CpcMicros = totals.MediaCostMicros / totals.Clicks;
        }

        if (totals.TotalConversions > 0)
        {
            totals.CpaMicros = totals.MediaCostMicros / totals.TotalConversions;
        }

        return totals;
    }
}
