namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a Display &amp; Video 360 insertion order (IO) with flattened budget and pacing settings.
/// <para>
/// Internally maps to the Google SDK’s <c>InsertionOrder</c> model, which nests budget
/// inside <c>InsertionOrderBudget → InsertionOrderBudgetSegment[]</c> and pacing inside
/// a <c>Pacing</c> object. This class simplifies the model to a single budget segment
/// with a flat date range, which covers the majority of Phase 1 use cases.
/// </para>
/// </summary>
public sealed class Dv360InsertionOrder
{
    /// <summary>
    /// The DV360-assigned insertion order identifier. <c>null</c> until the IO is created.
    /// </summary>
    public long? InsertionOrderId { get; set; }

    /// <summary>
    /// The parent campaign’s identifier that this insertion order belongs to.
    /// Set automatically by the workflow when creating IOs under a newly created campaign.
    /// </summary>
    public long CampaignId { get; set; }

    /// <summary>
    /// Human-readable name of the insertion order shown in the DV360 UI.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The entity lifecycle status. Defaults to <c>"ENTITY_STATUS_DRAFT"</c> so that
    /// newly created IOs do not start spending immediately.
    /// </summary>
    public string EntityStatus { get; set; } = "ENTITY_STATUS_DRAFT";

    /// <summary>
    /// The budget unit type (e.g., <c>"BUDGET_UNIT_CURRENCY"</c> or <c>"BUDGET_UNIT_IMPRESSIONS"</c>).
    /// Defaults to <c>"BUDGET_UNIT_CURRENCY"</c>.
    /// </summary>
    public string BudgetUnit { get; set; } = "BUDGET_UNIT_CURRENCY";

    /// <summary>
    /// The total budget for the budget segment in micros (1/1,000,000 of the currency unit).
    /// For example, 10 000 000 000 micros = $10,000.00.
    /// </summary>
    public long BudgetAmountMicros { get; set; }

    /// <summary>
    /// The start date of the budget segment’s flight window.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// The end date of the budget segment’s flight window.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Controls the pacing time period (e.g., <c>"PACING_PERIOD_DAILY"</c>, <c>"PACING_PERIOD_FLIGHT"</c>).
    /// Defaults to <c>"PACING_PERIOD_DAILY"</c>.
    /// </summary>
    public string PacingPeriod { get; set; } = "PACING_PERIOD_DAILY";

    /// <summary>
    /// Controls the pacing algorithm (e.g., <c>"PACING_TYPE_EVEN"</c>, <c>"PACING_TYPE_AHEAD"</c>).
    /// Defaults to <c>"PACING_TYPE_EVEN"</c>.
    /// </summary>
    public string PacingType { get; set; } = "PACING_TYPE_EVEN";

    /// <summary>
    /// The maximum daily spend in micros when pacing period is <c>"PACING_PERIOD_DAILY"</c>.
    /// </summary>
    public long DailyMaxMicros { get; set; }
}
