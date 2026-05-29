using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Suss.TikTok.Client.Infrastructure;
using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Default <see cref="ITikTokAdGroupService"/> implementation.
/// <para>
/// Maps the flat <see cref="TikTokAdGroup"/> model — including targeting, budget, schedule, bidding,
/// placements, and audience inclusion/exclusion — to TikTok's <c>adgroup/create/</c> request body
/// and maps the returned <c>adgroup_id</c> back onto the model.
/// </para>
/// </summary>
/// <param name="apiClient">The transport abstraction used to call TikTok.</param>
/// <param name="logger">Logger for diagnostics.</param>
internal sealed class TikTokAdGroupService(
    ITikTokApiClient apiClient,
    ILogger<TikTokAdGroupService> logger) : ITikTokAdGroupService
{
    /// <inheritdoc />
    public async Task<TikTokAdGroup> CreateAsync(
        string advertiserId,
        TikTokAdGroup adGroup,
        CancellationToken cancellationToken = default)
    {
        // The ad group cannot exist without a parent campaign.
        if (string.IsNullOrWhiteSpace(adGroup.CampaignId))
            throw new InvalidOperationException("TikTokAdGroup.CampaignId must be set before creating an ad group.");

        logger.LogInformation(
            "Creating ad group '{AdGroupName}' under campaign {CampaignId} for advertiser {AdvertiserId}.",
            adGroup.AdGroupName, adGroup.CampaignId, advertiserId);

        var body = new CreateAdGroupBody
        {
            AdvertiserId = advertiserId,
            CampaignId = adGroup.CampaignId,
            AdGroupName = adGroup.AdGroupName,
            PlacementType = adGroup.PlacementType,
            Placements = adGroup.Placements,
            BillingEvent = adGroup.BillingEvent,
            OptimizationGoal = adGroup.OptimizationGoal,
            BudgetMode = adGroup.BudgetMode,
            Budget = adGroup.Budget,
            BidPrice = adGroup.BidPrice,
            ScheduleType = adGroup.ScheduleType,
            ScheduleStartTime = adGroup.ScheduleStartTime,
            ScheduleEndTime = adGroup.ScheduleEndTime,
            LocationIds = adGroup.LocationIds,
            AudienceIds = adGroup.IncludedAudienceIds,
            ExcludedAudienceIds = adGroup.ExcludedAudienceIds,
            OperationStatus = adGroup.OperationStatus
        };

        var data = await apiClient.PostAsync<CreateAdGroupData>("adgroup/create/", body, cancellationToken);

        adGroup.AdGroupId = data.AdGroupId;
        return adGroup;
    }

    /// <summary>The <c>adgroup/create/</c> request body in TikTok's expected snake_case shape.</summary>
    private sealed class CreateAdGroupBody
    {
        [JsonPropertyName("advertiser_id")] public required string AdvertiserId { get; set; }
        [JsonPropertyName("campaign_id")] public required string CampaignId { get; set; }
        [JsonPropertyName("adgroup_name")] public required string AdGroupName { get; set; }
        [JsonPropertyName("placement_type")] public string? PlacementType { get; set; }
        [JsonPropertyName("placements")] public List<string>? Placements { get; set; }
        [JsonPropertyName("billing_event")] public string? BillingEvent { get; set; }
        [JsonPropertyName("optimization_goal")] public string? OptimizationGoal { get; set; }
        [JsonPropertyName("budget_mode")] public string? BudgetMode { get; set; }
        [JsonPropertyName("budget")] public double? Budget { get; set; }
        [JsonPropertyName("bid_price")] public double? BidPrice { get; set; }
        [JsonPropertyName("schedule_type")] public string? ScheduleType { get; set; }
        [JsonPropertyName("schedule_start_time")] public string? ScheduleStartTime { get; set; }
        [JsonPropertyName("schedule_end_time")] public string? ScheduleEndTime { get; set; }
        [JsonPropertyName("location_ids")] public List<string>? LocationIds { get; set; }
        [JsonPropertyName("audience_ids")] public List<string>? AudienceIds { get; set; }
        [JsonPropertyName("excluded_audience_ids")] public List<string>? ExcludedAudienceIds { get; set; }
        [JsonPropertyName("operation_status")] public string? OperationStatus { get; set; }
    }

    /// <summary>The <c>data</c> payload returned by <c>adgroup/create/</c>.</summary>
    private sealed class CreateAdGroupData
    {
        [JsonPropertyName("adgroup_id")] public string? AdGroupId { get; set; }
    }
}
