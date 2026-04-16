namespace Suss.Dv360.Api.Models;

/// <summary>Query parameters for listing campaigns.</summary>
public sealed class ListCampaignsRequest
{
    /// <summary>Maximum number of campaigns to return (1–200). Defaults to 100.</summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Field and direction to order by, e.g. <c>displayName</c> or <c>updateTime desc</c>.
    /// Supported fields: <c>displayName</c>, <c>entityStatus</c>, <c>updateTime</c>.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// DV360 filter expression, e.g. <c>entityStatus="ENTITY_STATUS_ACTIVE"</c>.
    /// Maximum 500 characters.
    /// </summary>
    public string? Filter { get; set; }
}
