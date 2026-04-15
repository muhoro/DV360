namespace Suss.Dv360.Client.Models;

/// <summary>
/// Primary GroupBy dimension for a Bid Manager report query.
/// </summary>
/// <remarks>
/// Each value maps to a Bid Manager <c>FILTER_*</c> dimension. The API also automatically
/// appends <c>FILTER_ADVERTISER_CURRENCY</c> to every query (required when cost metrics are present).
/// Use <see cref="CampaignReportRequest.AdditionalDimensions"/> to add further dimensions
/// beyond the primary one.
/// <para><b>Example — line item breakdown with device type:</b></para>
/// <code>
/// var request = new CampaignReportRequest
/// {
///     GroupBy              = ReportGrouping.LineItem,           // FILTER_LINE_ITEM
///     AdditionalDimensions = ["FILTER_DEVICE_TYPE", "FILTER_BROWSER"]
/// };
/// </code>
/// </remarks>
public enum ReportGrouping
{
    /// <summary>
    /// Group by date (one row per day).
    /// Bid Manager filter: <c>FILTER_DATE</c> → CSV column: <c>Date</c>
    /// </summary>
    Date,

    /// <summary>
    /// Group by campaign (media plan).
    /// Bid Manager filter: <c>FILTER_MEDIA_PLAN</c> → CSV columns: <c>Campaign ID</c>, <c>Campaign</c>
    /// </summary>
    Campaign,

    /// <summary>
    /// Group by advertiser.
    /// Bid Manager filter: <c>FILTER_ADVERTISER</c> → CSV columns: <c>Advertiser ID</c>, <c>Advertiser</c>
    /// </summary>
    Advertiser,

    /// <summary>
    /// Group by insertion order.
    /// Bid Manager filter: <c>FILTER_INSERTION_ORDER</c> → CSV columns: <c>Insertion Order ID</c>, <c>Insertion Order</c>
    /// </summary>
    InsertionOrder,

    /// <summary>
    /// Group by line item (most granular delivery unit in DV360).
    /// Bid Manager filter: <c>FILTER_LINE_ITEM</c> → CSV columns: <c>Line Item ID</c>, <c>Line Item</c>
    /// </summary>
    LineItem,

    /// <summary>
    /// Group by creative asset.
    /// Bid Manager filter: <c>FILTER_CREATIVE_ID</c> → CSV columns: <c>Creative ID</c>, <c>Creative</c>
    /// </summary>
    Creative,

    /// <summary>
    /// Group by ad exchange / SSP.
    /// Bid Manager filter: <c>FILTER_EXCHANGE</c> → CSV column: <c>Exchange</c>
    /// </summary>
    Exchange
}
