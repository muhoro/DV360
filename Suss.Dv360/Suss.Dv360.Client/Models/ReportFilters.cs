namespace Suss.Dv360.Client.Models;

/// <summary>
/// ID-based filters that narrow report data in Bid Manager queries.
/// </summary>
/// <remarks>
/// These filters are sent as <c>FilterPair</c> objects in the Bid Manager API request.
/// They restrict which rows appear in the report but do <b>not</b> add columns —
/// use <see cref="CampaignReportRequest.AdditionalDimensions"/> to add grouping columns.
/// <para>
/// Filters at the <see cref="CampaignReportRequest"/> level (<c>AdvertiserId</c>,
/// <c>CampaignId</c>, <c>LineItemId</c>) are always applied first. Use this class
/// to add multi-value filters on top of those.
/// </para>
/// <para><b>Example — report scoped to two insertion orders:</b></para>
/// <code>
/// var request = new CampaignReportRequest
/// {
///     AdvertiserId = 8101501893,
///     DateRange    = new ReportDateRange { RelativeDateRange = "LAST_30_DAYS" },
///     GroupBy      = ReportGrouping.InsertionOrder,
///     Filters = new ReportFilters
///     {
///         InsertionOrderIds = [111111, 222222]
///     }
/// };
/// </code>
/// </remarks>
public sealed class ReportFilters
{
    /// <summary>
    /// Restrict the report to specific campaign IDs.
    /// Bid Manager filter: <c>FILTER_MEDIA_PLAN</c>
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="CampaignReportRequest.CampaignId"/> for single-campaign reports.
    /// Use this property when you need to filter by multiple campaigns in one query.
    /// </remarks>
    public IReadOnlyList<long>? CampaignIds { get; set; }

    /// <summary>
    /// Restrict the report to specific advertiser IDs.
    /// Bid Manager filter: <c>FILTER_ADVERTISER</c>
    /// </summary>
    /// <remarks>
    /// <see cref="CampaignReportRequest.AdvertiserId"/> is always applied as a mandatory
    /// filter. Use this property only when you need to add <em>additional</em> advertiser IDs.
    /// </remarks>
    public IReadOnlyList<long>? AdvertiserIds { get; set; }

    /// <summary>
    /// Restrict the report to specific line item IDs.
    /// Bid Manager filter: <c>FILTER_LINE_ITEM</c>
    /// </summary>
    public IReadOnlyList<long>? LineItemIds { get; set; }

    /// <summary>
    /// Restrict the report to specific insertion order IDs.
    /// Bid Manager filter: <c>FILTER_INSERTION_ORDER</c>
    /// </summary>
    public IReadOnlyList<long>? InsertionOrderIds { get; set; }

    /// <summary>
    /// Restrict the report to specific creative IDs.
    /// Bid Manager filter: <c>FILTER_CREATIVE_ID</c>
    /// </summary>
    public IReadOnlyList<long>? CreativeIds { get; set; }

    /// <summary>
    /// Restrict the report to specific exchange IDs.
    /// Bid Manager filter: <c>FILTER_EXCHANGE</c>
    /// </summary>
    /// <remarks>
    /// Common exchange IDs: Google Ad Exchange = 10, OpenX = 28, PubMatic = 22,
    /// Rubicon = 31, Index Exchange = 20, AppNexus = 2.
    /// </remarks>
    public IReadOnlyList<long>? ExchangeIds { get; set; }
}
