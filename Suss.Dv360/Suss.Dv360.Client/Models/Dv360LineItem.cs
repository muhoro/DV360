namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a Display &amp; Video 360 line item with flattened budget, flight, and pacing settings.
/// <para>
/// Internally maps to the Google SDK’s <c>LineItem</c> model, which nests budget in
/// <c>LineItemBudget</c>, flight dates in <c>LineItemFlight</c>, and pacing in <c>Pacing</c>.
/// Line items sit under an insertion order and define the targeting, creative delivery,
/// and budget allocation for ad serving.
/// </para>
/// </summary>
public sealed class Dv360LineItem
{
    /// <summary>
    /// The DV360-assigned line item identifier. <c>null</c> until the line item is created.
    /// </summary>
    public long? LineItemId { get; set; }

    /// <summary>
    /// The parent campaign’s identifier. Set automatically by the workflow.
    /// </summary>
    public long CampaignId { get; set; }

    /// <summary>
    /// The parent insertion order’s identifier. Set automatically by the workflow.
    /// </summary>
    public long InsertionOrderId { get; set; }

    /// <summary>
    /// Human-readable name of the line item shown in the DV360 UI.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The type of line item (e.g., <c>"LINE_ITEM_TYPE_DISPLAY_DEFAULT"</c>,
    /// <c>"LINE_ITEM_TYPE_VIDEO_DEFAULT"</c>).
    /// </summary>
    public required string LineItemType { get; set; }

    /// <summary>
    /// The entity lifecycle status. Defaults to <c>"ENTITY_STATUS_DRAFT"</c>.
    /// </summary>
    public string EntityStatus { get; set; } = "ENTITY_STATUS_DRAFT";

    /// <summary>
    /// How the budget is allocated (e.g., <c>"LINE_ITEM_BUDGET_ALLOCATION_TYPE_FIXED"</c>).
    /// Defaults to fixed allocation.
    /// </summary>
    public string BudgetAllocationType { get; set; } = "LINE_ITEM_BUDGET_ALLOCATION_TYPE_FIXED";

    /// <summary>
    /// The budget unit type. Defaults to <c>"BUDGET_UNIT_CURRENCY"</c>.
    /// </summary>
    public string BudgetUnit { get; set; } = "BUDGET_UNIT_CURRENCY";

    /// <summary>
    /// The maximum budget amount in micros for this line item’s lifetime.
    /// </summary>
    public long MaxBudgetAmountMicros { get; set; }

    /// <summary>
    /// The start date of the line item’s custom flight window.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// The end date of the line item’s custom flight window.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Controls the pacing time period. Defaults to <c>"PACING_PERIOD_DAILY"</c>.
    /// </summary>
    public string PacingPeriod { get; set; } = "PACING_PERIOD_DAILY";

    /// <summary>
    /// Controls the pacing algorithm. Defaults to <c>"PACING_TYPE_EVEN"</c>.
    /// </summary>
    public string PacingType { get; set; } = "PACING_TYPE_EVEN";

    /// <summary>
    /// The maximum daily spend in micros when pacing period is daily.
    /// </summary>
    public long DailyMaxMicros { get; set; }
}
