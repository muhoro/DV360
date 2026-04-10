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
    /// The entity lifecycle status (e.g., <c>"ENTITY_STATUS_ACTIVE"</c>, <c>"ENTITY_STATUS_PAUSED"</c>).
    /// Defaults to <c>"ENTITY_STATUS_ACTIVE"</c>.
    /// </summary>
    public string EntityStatus { get; set; } = "ENTITY_STATUS_ACTIVE";

    /// <summary>
    /// The high-level campaign goal type (e.g., <c>"CAMPAIGN_GOAL_TYPE_BRAND_AWARENESS"</c>).
    /// </summary>
    public required string GoalType { get; set; }

    /// <summary>
    /// Optional performance goal type that further qualifies the campaign goal
    /// (e.g., <c>"PERFORMANCE_GOAL_TYPE_CPM"</c>). <c>null</c> if no performance goal is set.
    /// </summary>
    public string? PerformanceGoalType { get; set; }

    /// <summary>
    /// The target performance goal amount in micros (1/1,000,000 of the currency unit).
    /// For example, 1 000 000 micros = $1.00. <c>null</c> when no performance goal is defined.
    /// </summary>
    public long? PerformanceGoalAmountMicros { get; set; }

    /// <summary>
    /// The planned start date of the campaign flight. <c>null</c> for open-ended campaigns.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// The planned end date of the campaign flight. <c>null</c> for open-ended campaigns.
    /// </summary>
    public DateOnly? EndDate { get; set; }
}
