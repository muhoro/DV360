namespace Suss.Dv360.Client.Models;

/// <summary>
/// Request parameters for generating a campaign report via the Bid Manager API v2.
/// </summary>
/// <remarks>
/// <para>
/// The report lifecycle is: create query → run query → poll for completion → download CSV → parse rows.
/// <see cref="Services.IReportingService.GetCampaignReportAsync"/> handles the full lifecycle.
/// </para>
/// <para><b>Minimal example — last 30 days, grouped by date:</b></para>
/// <code>
/// var request = new CampaignReportRequest
/// {
///     AdvertiserId = 8101501893,
///     CampaignId   = 56793523,
///     DateRange    = new ReportDateRange { RelativeDateRange = "LAST_30_DAYS" },
///     GroupBy      = ReportGrouping.Date
/// };
/// </code>
/// <para><b>Custom metrics + additional dimensions:</b></para>
/// <code>
/// var request = new CampaignReportRequest
/// {
///     AdvertiserId = 8101501893,
///     DateRange    = new ReportDateRange { RelativeDateRange = "LAST_7_DAYS" },
///     GroupBy      = ReportGrouping.LineItem,
///     AdditionalDimensions = ["FILTER_DEVICE_TYPE", "FILTER_BROWSER"],
///     Metrics =
///     [
///         "METRIC_IMPRESSIONS",
///         "METRIC_CLICKS",
///         "METRIC_CTR",
///         "METRIC_MEDIA_COST_ADVERTISER",
///         "METRIC_TOTAL_CONVERSIONS",
///         "METRIC_ACTIVE_VIEW_PCT_VIEWABLE_IMPRESSIONS"
///     ]
/// };
/// </code>
/// </remarks>
public sealed class CampaignReportRequest
{
    /// <summary>
    /// The DV360 advertiser ID to scope the report to. Required.
    /// Corresponds to <c>FILTER_ADVERTISER</c> in the Bid Manager API.
    /// </summary>
    public required long AdvertiserId { get; set; }

    /// <summary>
    /// Narrows the report to a single campaign (media plan).
    /// Corresponds to <c>FILTER_MEDIA_PLAN</c> in the Bid Manager API.
    /// </summary>
    /// <remarks>
    /// When set, the campaign name and ID are automatically backfilled onto every
    /// parsed row even when <c>FILTER_MEDIA_PLAN_NAME</c> is not a GroupBy dimension.
    /// </remarks>
    public long? CampaignId { get; set; }

    /// <summary>
    /// Narrows the report to a single line item.
    /// Corresponds to <c>FILTER_LINE_ITEM</c> in the Bid Manager API.
    /// </summary>
    public long? LineItemId { get; set; }

    /// <summary>
    /// The date range for the report data. Required.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ReportDateRange.RelativeDateRange"/> for preset ranges or
    /// <see cref="ReportDateRange.StartDate"/> / <see cref="ReportDateRange.EndDate"/>
    /// for a custom window.
    /// <para><b>Valid preset values (Bid Manager <c>Range</c> enum):</b></para>
    /// <list type="bullet">
    ///   <item><c>CURRENT_DAY</c> — today</item>
    ///   <item><c>PREVIOUS_DAY</c> — yesterday</item>
    ///   <item><c>WEEK_TO_DATE</c> / <c>CURRENT_WEEK</c></item>
    ///   <item><c>MONTH_TO_DATE</c> / <c>CURRENT_MONTH</c></item>
    ///   <item><c>QUARTER_TO_DATE</c> / <c>CURRENT_QUARTER</c></item>
    ///   <item><c>YEAR_TO_DATE</c> / <c>CURRENT_YEAR</c></item>
    ///   <item><c>PREVIOUS_WEEK</c>, <c>PREVIOUS_MONTH</c>, <c>PREVIOUS_QUARTER</c>, <c>PREVIOUS_YEAR</c></item>
    ///   <item><c>LAST_7_DAYS</c>, <c>LAST_14_DAYS</c>, <c>LAST_30_DAYS</c>, <c>LAST_60_DAYS</c>, <c>LAST_90_DAYS</c>, <c>LAST_365_DAYS</c></item>
    ///   <item><c>ALL_TIME</c></item>
    /// </list>
    /// </remarks>
    public required ReportDateRange DateRange { get; set; }

    /// <summary>
    /// The primary dimension to group report rows by.
    /// Defaults to <see cref="ReportGrouping.Date"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ReportGrouping"/> maps to these Bid Manager filter types:
    /// <list type="table">
    ///   <listheader><term>Enum value</term><description>Bid Manager filter</description></listheader>
    ///   <item><term>Date</term><description>FILTER_DATE</description></item>
    ///   <item><term>Campaign</term><description>FILTER_MEDIA_PLAN</description></item>
    ///   <item><term>Advertiser</term><description>FILTER_ADVERTISER</description></item>
    ///   <item><term>InsertionOrder</term><description>FILTER_INSERTION_ORDER</description></item>
    ///   <item><term>LineItem</term><description>FILTER_LINE_ITEM</description></item>
    ///   <item><term>Creative</term><description>FILTER_CREATIVE_ID</description></item>
    ///   <item><term>Exchange</term><description>FILTER_EXCHANGE</description></item>
    /// </list>
    /// <c>FILTER_ADVERTISER_CURRENCY</c> is always included automatically (required by the API
    /// whenever cost metrics such as <c>METRIC_MEDIA_COST_ADVERTISER</c> are requested).
    /// </remarks>
    public ReportGrouping GroupBy { get; set; } = ReportGrouping.Date;

    /// <summary>
    /// Metrics to include in the report. When <see langword="null"/> the default set is used.
    /// </summary>
    /// <remarks>
    /// Pass Bid Manager metric names exactly as defined in the API reference.
    /// <para><b>Commonly used metrics:</b></para>
    /// <list type="table">
    ///   <listheader><term>Metric name</term><description>Report column</description></listheader>
    ///   <item><term>METRIC_IMPRESSIONS</term><description>Impressions</description></item>
    ///   <item><term>METRIC_CLICKS</term><description>Clicks</description></item>
    ///   <item><term>METRIC_CTR</term><description>Click Rate (CTR)</description></item>
    ///   <item><term>METRIC_TOTAL_CONVERSIONS</term><description>Total Conversions</description></item>
    ///   <item><term>METRIC_MEDIA_COST_ADVERTISER</term><description>Media Cost (Advertiser Currency)</description></item>
    ///   <item><term>METRIC_TOTAL_MEDIA_COST_ADVERTISER</term><description>Total Media Cost (Advertiser Currency)</description></item>
    ///   <item><term>METRIC_REVENUE_ADVERTISER</term><description>Revenue (Advertiser Currency)</description></item>
    ///   <item><term>METRIC_ACTIVE_VIEW_PCT_VIEWABLE_IMPRESSIONS</term><description>Active View: % Viewable Impressions</description></item>
    ///   <item><term>METRIC_ACTIVE_VIEW_VIEWABLE_IMPRESSIONS</term><description>Active View: Viewable Impressions</description></item>
    ///   <item><term>METRIC_RICH_MEDIA_VIDEO_COMPLETIONS</term><description>Complete Views (Video)</description></item>
    ///   <item><term>METRIC_VIDEO_COMPLETION_RATE</term><description>Completion Rate (Video)</description></item>
    ///   <item><term>METRIC_TRUEVIEW_VIEWS</term><description>TrueView: Views</description></item>
    ///   <item><term>METRIC_LAST_CLICKS</term><description>Post-Click Conversions</description></item>
    ///   <item><term>METRIC_LAST_IMPRESSIONS</term><description>Post-View Conversions</description></item>
    ///   <item><term>METRIC_BILLABLE_IMPRESSIONS</term><description>Billable Impressions</description></item>
    ///   <item><term>METRIC_UNIQUE_REACH_IMPRESSION_REACH</term><description>Unique Reach: Impression Reach</description></item>
    /// </list>
    /// The full list is available at:
    /// https://developers.google.com/bid-manager/reference/rest/v2/filters-metrics#metrics
    /// </remarks>
    public IReadOnlyList<string>? Metrics { get; set; }

    /// <summary>
    /// Additional GroupBy dimensions beyond the primary <see cref="GroupBy"/>.
    /// </summary>
    /// <remarks>
    /// Accepts either friendly aliases (e.g. <c>"DATE"</c>, <c>"LINE_ITEM"</c>) or
    /// raw Bid Manager filter names (e.g. <c>"FILTER_DEVICE_TYPE"</c>).
    /// <para><b>Commonly used additional dimensions:</b></para>
    /// <list type="table">
    ///   <listheader><term>Filter name</term><description>Report column</description></listheader>
    ///   <item><term>FILTER_MEDIA_PLAN_NAME</term><description>Campaign</description></item>
    ///   <item><term>FILTER_INSERTION_ORDER_NAME</term><description>Insertion Order</description></item>
    ///   <item><term>FILTER_LINE_ITEM_NAME</term><description>Line Item</description></item>
    ///   <item><term>FILTER_CREATIVE</term><description>Creative</description></item>
    ///   <item><term>FILTER_DEVICE_TYPE</term><description>Device Type</description></item>
    ///   <item><term>FILTER_BROWSER</term><description>Browser</description></item>
    ///   <item><term>FILTER_COUNTRY</term><description>Country</description></item>
    ///   <item><term>FILTER_REGION_NAME</term><description>Region</description></item>
    ///   <item><term>FILTER_CITY_NAME</term><description>City</description></item>
    ///   <item><term>FILTER_OS</term><description>Operating System</description></item>
    ///   <item><term>FILTER_EXCHANGE</term><description>Exchange</description></item>
    ///   <item><term>FILTER_PAGE_LAYOUT</term><description>Environment</description></item>
    ///   <item><term>FILTER_DIGITAL_CONTENT_LABEL</term><description>Digital Content Label</description></item>
    ///   <item><term>FILTER_DAY_OF_WEEK</term><description>Day of Week</description></item>
    ///   <item><term>FILTER_WEEK</term><description>Week</description></item>
    ///   <item><term>FILTER_MONTH</term><description>Month</description></item>
    /// </list>
    /// The full list is available at:
    /// https://developers.google.com/bid-manager/reference/rest/v2/filters-metrics#filters
    /// </remarks>
    public IReadOnlyList<string>? AdditionalDimensions { get; set; }

    /// <summary>
    /// Additional ID-based filters to narrow the report beyond <see cref="CampaignId"/>
    /// and <see cref="LineItemId"/>. Useful for multi-campaign or multi-IO reports.
    /// </summary>
    public ReportFilters? Filters { get; set; }

    /// <summary>
    /// Display name for the Bid Manager query. Appears in the DV360 UI and in
    /// generated report filenames. Auto-generated with a timestamp when not set.
    /// </summary>
    public string? QueryTitle { get; set; }
}
