namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a Display &amp; Video 360 campaign with a flattened structure.
/// <para>
/// Internally maps to the Google SDK’s nested <c>Campaign</c> model, which contains
/// separate <c>CampaignGoal</c> and <c>CampaignFlight</c> objects. This class exposes
/// those nested values as top-level properties so consumers never interact with
/// Google SDK types.
/// </para>
/// </summary>
public sealed class Dv360Campaign
{
    /// <summary>
    /// The DV360-assigned campaign identifier. <c>null</c> until the campaign is created.
    /// </summary>
    public long? CampaignId { get; set; }

    /// <summary>
    /// Human-readable name of the campaign shown in the DV360 UI.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The entity lifecycle status (e.g., <c>"ENTITY_STATUS_PAUSED"</c>, <c>"ENTITY_STATUS_ACTIVE"</c>).
    /// Defaults to <c>"ENTITY_STATUS_PAUSED"</c> because the DV360 API does not allow creating
    /// campaigns directly in active status.
    /// </summary>
    public string EntityStatus { get; set; } = "ENTITY_STATUS_PAUSED";

    /// <summary>
    /// The high-level campaign goal type (e.g., <c>"CAMPAIGN_GOAL_TYPE_BRAND_AWARENESS"</c>).
    /// </summary>
    public required string GoalType { get; set; }

    /// <summary>
    /// The performance goal type that qualifies the campaign goal
    /// (e.g., <c>"PERFORMANCE_GOAL_TYPE_CPM"</c>).
    /// Required by the DV360 API. Acceptable values include:
    /// <c>PERFORMANCE_GOAL_TYPE_CPM</c>, <c>PERFORMANCE_GOAL_TYPE_CPC</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_CPA</c>, <c>PERFORMANCE_GOAL_TYPE_CPIAVC</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_CTR</c>, <c>PERFORMANCE_GOAL_TYPE_VIEWABILITY</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_OTHER</c>.
    /// </summary>
    public required string PerformanceGoalType { get; set; }

    /// <summary>
    /// The target performance goal amount in micros (1/1,000,000 of the currency unit).
    /// Applicable when <see cref="PerformanceGoalType"/> is one of:
    /// <c>PERFORMANCE_GOAL_TYPE_CPM</c>, <c>PERFORMANCE_GOAL_TYPE_CPC</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_CPA</c>, <c>PERFORMANCE_GOAL_TYPE_CPIAVC</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_VCPM</c>.
    /// For example, 1 500 000 represents 1.5 standard units of the currency.
    /// Mutually exclusive with <see cref="PerformanceGoalPercentageMicros"/> and <see cref="PerformanceGoalString"/>.
    /// </summary>
    public long? PerformanceGoalAmountMicros { get; set; }

    /// <summary>
    /// The decimal representation of the goal percentage in micros.
    /// Applicable when <see cref="PerformanceGoalType"/> is one of:
    /// <c>PERFORMANCE_GOAL_TYPE_CTR</c>, <c>PERFORMANCE_GOAL_TYPE_VIEWABILITY</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_CLICK_CVR</c>, <c>PERFORMANCE_GOAL_TYPE_IMPRESSION_CVR</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_VTR</c>, <c>PERFORMANCE_GOAL_TYPE_AUDIO_COMPLETION_RATE</c>,
    /// <c>PERFORMANCE_GOAL_TYPE_VIDEO_COMPLETION_RATE</c>.
    /// For example, 70000 represents 7% (decimal 0.07).
    /// Mutually exclusive with <see cref="PerformanceGoalAmountMicros"/> and <see cref="PerformanceGoalString"/>.
    /// </summary>
    public long? PerformanceGoalPercentageMicros { get; set; }

    /// <summary>
    /// A key performance indicator (KPI) string, which can be empty. Must be UTF-8 encoded
    /// with a length of no more than 100 characters.
    /// Applicable when <see cref="PerformanceGoalType"/> is <c>PERFORMANCE_GOAL_TYPE_OTHER</c>.
    /// Mutually exclusive with <see cref="PerformanceGoalAmountMicros"/> and <see cref="PerformanceGoalPercentageMicros"/>.
    /// </summary>
    public string? PerformanceGoalString { get; set; }

    /// <summary>
    /// The display name of the campaign budget. Required by the DV360 API.
    /// Must be UTF-8 encoded with a maximum size of 240 bytes.
    /// Defaults to <c>"Total Campaign Budget"</c>.
    /// </summary>
    public string BudgetDisplayName { get; set; } = "Total Campaign Budget";

    /// <summary>
    /// The budget unit type for the campaign budget
    /// (e.g., <c>"BUDGET_UNIT_CURRENCY"</c>, <c>"BUDGET_UNIT_IMPRESSIONS"</c>).
    /// Defaults to <c>"BUDGET_UNIT_CURRENCY"</c>.
    /// </summary>
    public string BudgetUnit { get; set; } = "BUDGET_UNIT_CURRENCY";

    /// <summary>
    /// The total campaign budget amount in micros (1/1,000,000 of the currency unit).
    /// For example, 10 000 000 000 micros = $10,000.00.
    /// </summary>
    public long BudgetAmountMicros { get; set; }

    /// <summary>
    /// The amount the campaign is expected to spend for its planned dates, in micros.
    /// Used for tracking spend in the DV360 UI (does not limit serving).
    /// Must be greater than or equal to 0. <c>null</c> if not specified.
    /// For example, 500 000 000 represents 500 standard units of the currency.
    /// </summary>
    public long? PlannedSpendAmountMicros { get; set; }

    /// <summary>
    /// The planned start date of the campaign flight. <c>null</c> for open-ended campaigns.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// The planned end date of the campaign flight. <c>null</c> for open-ended campaigns.
    /// </summary>
    public DateOnly? EndDate { get; set; }
}
