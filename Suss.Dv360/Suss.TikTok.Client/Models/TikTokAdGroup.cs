namespace Suss.TikTok.Client.Models;

/// <summary>
/// A flat representation of a TikTok ad group (the middle tier: campaign → ad group → ad).
/// <para>
/// The ad group carries targeting, budget, schedule, bidding, and placement settings. Only common
/// fields are surfaced here; <see cref="AdGroupId"/> is populated after creation and
/// <see cref="CampaignId"/> is wired automatically by the workflow.
/// </para>
/// </summary>
public sealed class TikTokAdGroup
{
    /// <summary>The server-assigned ad group id. <c>null</c> before creation.</summary>
    public string? AdGroupId { get; set; }

    /// <summary>The parent campaign id. Set automatically by the workflow after the campaign is created.</summary>
    public string? CampaignId { get; set; }

    /// <summary>A human-readable ad group name.</summary>
    public required string AdGroupName { get; set; }

    /// <summary>
    /// Where ads are shown (TikTok <c>placement_type</c>): <c>PLACEMENT_TYPE_AUTOMATIC</c> or
    /// <c>PLACEMENT_TYPE_NORMAL</c> (with explicit <see cref="Placements"/>).
    /// </summary>
    public string PlacementType { get; set; } = "PLACEMENT_TYPE_AUTOMATIC";

    /// <summary>Explicit placements (e.g., <c>PLACEMENT_TIKTOK</c>) when <see cref="PlacementType"/> is normal.</summary>
    public List<string>? Placements { get; set; }

    /// <summary>The optimization goal/billing event mapping (TikTok <c>billing_event</c>), e.g., <c>CPC</c>, <c>CPM</c>, <c>OCPM</c>.</summary>
    public string BillingEvent { get; set; } = "CPC";

    /// <summary>The optimization goal (TikTok <c>optimization_goal</c>), e.g., <c>CLICK</c>, <c>CONVERT</c>, <c>REACH</c>.</summary>
    public string OptimizationGoal { get; set; } = "CLICK";

    /// <summary>The budget mode (TikTok <c>budget_mode</c>): infinite/day/total.</summary>
    public string BudgetMode { get; set; } = "BUDGET_MODE_DAY";

    /// <summary>The ad group budget amount; required for day/total budget modes.</summary>
    public double? Budget { get; set; }

    /// <summary>The bid amount, when a manual bidding strategy is used.</summary>
    public double? BidPrice { get; set; }

    /// <summary>Delivery start time in the TikTok-expected format ("yyyy-MM-dd HH:mm:ss").</summary>
    public string? ScheduleStartTime { get; set; }

    /// <summary>Optional delivery end time in the TikTok-expected format ("yyyy-MM-dd HH:mm:ss").</summary>
    public string? ScheduleEndTime { get; set; }

    /// <summary>Schedule type (TikTok <c>schedule_type</c>): <c>SCHEDULE_FROM_NOW</c> or <c>SCHEDULE_START_END</c>.</summary>
    public string ScheduleType { get; set; } = "SCHEDULE_FROM_NOW";

    /// <summary>Target location ids (TikTok <c>location_ids</c>) for geo targeting.</summary>
    public List<string>? LocationIds { get; set; }

    /// <summary>
    /// Custom audience ids to include in targeting (TikTok <c>audience_ids</c>). These are the ids
    /// returned when creating/uploading an audience — see <see cref="TikTokAudience"/>.
    /// </summary>
    public List<string>? IncludedAudienceIds { get; set; }

    /// <summary>Custom audience ids to exclude from targeting (TikTok <c>excluded_audience_ids</c>).</summary>
    public List<string>? ExcludedAudienceIds { get; set; }

    /// <summary>Operation status (TikTok <c>operation_status</c>): <c>ENABLE</c> or <c>DISABLE</c>.</summary>
    public string OperationStatus { get; set; } = "ENABLE";
}
