namespace Suss.TikTok.Client.Models;

/// <summary>
/// A flat, consumer-friendly representation of a TikTok campaign.
/// <para>
/// The TikTok campaign is the top of the delivery hierarchy (campaign → ad group → ad). This model
/// intentionally exposes only the most common fields; the service maps it to/from the API's
/// snake_case payload. <see cref="CampaignId"/> is populated after creation.
/// </para>
/// </summary>
public sealed class TikTokCampaign
{
    /// <summary>The server-assigned campaign id. <c>null</c> before creation; populated afterwards.</summary>
    public string? CampaignId { get; set; }

    /// <summary>A human-readable campaign name (must be unique within the advertiser account).</summary>
    public required string CampaignName { get; set; }

    /// <summary>
    /// The advertising objective (TikTok <c>objective_type</c>), e.g., <c>REACH</c>, <c>TRAFFIC</c>,
    /// <c>VIDEO_VIEWS</c>, <c>CONVERSIONS</c>.
    /// </summary>
    public required string ObjectiveType { get; set; }

    /// <summary>
    /// The budget mode (TikTok <c>budget_mode</c>): <c>BUDGET_MODE_INFINITE</c>,
    /// <c>BUDGET_MODE_DAY</c>, or <c>BUDGET_MODE_TOTAL</c>.
    /// </summary>
    public string BudgetMode { get; set; } = "BUDGET_MODE_DAY";

    /// <summary>
    /// The campaign-level budget amount. Required when <see cref="BudgetMode"/> is a day/total mode.
    /// </summary>
    public double? Budget { get; set; }

    /// <summary>
    /// Operation status to set on creation/update (TikTok <c>operation_status</c>): <c>ENABLE</c> or <c>DISABLE</c>.
    /// </summary>
    public string OperationStatus { get; set; } = "ENABLE";
}
