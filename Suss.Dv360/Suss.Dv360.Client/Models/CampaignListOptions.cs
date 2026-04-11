namespace Suss.Dv360.Client.Models;

/// <summary>
/// Options for listing DV360 campaigns, supporting server-side filtering, sorting, and page size.
/// <para>
/// These options map directly to the query parameters of the
/// <c>advertisers.campaigns.list</c> API method. When no options are specified,
/// the API defaults apply (page size 100, ordered by <c>displayName</c> ascending,
/// archived campaigns excluded).
/// </para>
/// </summary>
public sealed class CampaignListOptions
{
    /// <summary>
    /// Requested page size per API call. Must be between 1 and 200.
    /// If <c>null</c>, the API default of 100 is used.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Field by which to sort the list. Acceptable values are:
    /// <c>"displayName"</c> (default), <c>"entityStatus"</c>, <c>"updateTime"</c>.
    /// <para>
    /// The default sorting order is ascending. To specify descending order,
    /// append <c>" desc"</c> to the field name (e.g., <c>"updateTime desc"</c>).
    /// </para>
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// A filter expression that restricts results using DV360 list filter syntax.
    /// <para>
    /// Supported fields: <c>campaignId</c>, <c>displayName</c>, <c>entityStatus</c>,
    /// <c>updateTime</c>. The <c>updateTime</c> field supports <c>&gt;=</c> and <c>&lt;=</c>;
    /// all other fields use <c>=</c>. Restrictions can be combined with <c>AND</c> / <c>OR</c>.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><c>entityStatus="ENTITY_STATUS_ACTIVE"</c></item>
    ///   <item><c>(entityStatus="ENTITY_STATUS_ACTIVE" OR entityStatus="ENTITY_STATUS_PAUSED")</c></item>
    ///   <item><c>updateTime&gt;="2024-01-01T00:00:00Z"</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// If no <c>entityStatus</c> filter is specified, campaigns with
    /// <c>ENTITY_STATUS_ARCHIVED</c> are excluded by default.
    /// The length of this field should be no more than 500 characters.
    /// </para>
    /// </summary>
    public string? Filter { get; set; }
}
