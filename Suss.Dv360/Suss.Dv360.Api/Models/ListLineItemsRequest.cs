namespace Suss.Dv360.Api.Models;

/// <summary>Query parameters for listing line items.</summary>
public sealed class ListLineItemsRequest
{
    /// <summary>Maximum number of line items to return (1–200).</summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Field and direction to order by, e.g. <c>displayName desc</c>.
    /// Supported fields: <c>displayName</c>, <c>entityStatus</c>, <c>updateTime</c>,
    /// <c>flight.dateRange.endDate</c>.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// DV360 filter expression, e.g. <c>insertionOrderId="12345"</c>.
    /// </summary>
    public string? Filter { get; set; }
}
