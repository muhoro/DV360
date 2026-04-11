namespace Suss.Dv360.Client.Models;

/// <summary>
/// Options for listing DV360 line items, supporting server-side filtering, sorting, and page size.
/// <para>
/// These options map directly to the query parameters of the
/// <c>advertisers.lineItems.list</c> API method. When no options are specified,
/// the API defaults apply.
/// </para>
/// </summary>
public sealed class LineItemListOptions
{
    /// <summary>
    /// Requested page size per API call. Must be between 1 and 200.
    /// If <c>null</c>, the API default of 100 is used.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Field by which to sort the list. Acceptable values include:
    /// <c>"displayName"</c> (default), <c>"entityStatus"</c>, <c>"updateTime"</c>,
    /// <c>"flight.dateRange.endDate"</c>.
    /// <para>
    /// The default sorting order is ascending. To specify descending order,
    /// append <c>" desc"</c> to the field name (e.g., <c>"updateTime desc"</c>).
    /// </para>
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// A filter expression that restricts results using DV360 list filter syntax.
    /// <para>
    /// Supported fields include: <c>campaignId</c>, <c>displayName</c>,
    /// <c>insertionOrderId</c>, <c>entityStatus</c>, <c>lineItemId</c>,
    /// <c>lineItemType</c>, <c>updateTime</c>.
    /// The <c>updateTime</c> field supports <c>&gt;=</c> and <c>&lt;=</c>;
    /// all other fields use <c>=</c>. Restrictions can be combined with <c>AND</c> / <c>OR</c>.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><c>insertionOrderId="12345"</c></item>
    ///   <item><c>entityStatus="ENTITY_STATUS_ACTIVE"</c></item>
    ///   <item><c>(entityStatus="ENTITY_STATUS_ACTIVE" OR entityStatus="ENTITY_STATUS_PAUSED")</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// If no <c>entityStatus</c> filter is specified, line items with
    /// <c>ENTITY_STATUS_ARCHIVED</c> are excluded by default.
    /// The length of this field should be no more than 500 characters.
    /// </para>
    /// </summary>
    public string? Filter { get; set; }
}
