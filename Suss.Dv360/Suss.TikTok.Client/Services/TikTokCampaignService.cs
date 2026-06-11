using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Suss.TikTok.Client.Auth;
using Suss.TikTok.Client.Infrastructure;
using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Default <see cref="ITikTokCampaignService"/> implementation.
/// <para>
/// Maps the flat <see cref="TikTokCampaign"/> model to TikTok's snake_case <c>campaign/create/</c>
/// request body and maps the returned <c>campaign_id</c> back onto the model. Transport, auth, and
/// envelope handling are delegated to <see cref="ITikTokApiClient"/>.
/// </para>
/// </summary>
/// <param name="apiClient">The transport abstraction used to call TikTok.</param>
/// <param name="logger">Logger for diagnostics.</param>
internal sealed class TikTokCampaignService(
    ITikTokApiClient apiClient,
    ILogger<TikTokCampaignService> logger) : ITikTokCampaignService
{
    /// <inheritdoc />
    public async Task<TikTokCampaign> CreateAsync(
        string advertiserId,
        TikTokCampaign campaign,
        CancellationToken cancellationToken = default)
        => await CreateAsync(null, advertiserId, campaign, cancellationToken);

    /// <inheritdoc />
    public async Task<TikTokCampaign> CreateAsync(
        TikTokExecutionContext executionContext,
        TikTokCampaign campaign,
        CancellationToken cancellationToken = default)
        => await CreateAsync(executionContext, executionContext.AdvertiserId, campaign, cancellationToken);

    private async Task<TikTokCampaign> CreateAsync(
        TikTokExecutionContext? executionContext,
        string advertiserId,
        TikTokCampaign campaign,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating campaign '{CampaignName}' (objective {Objective}) for advertiser {AdvertiserId}.",
            campaign.CampaignName, campaign.ObjectiveType, advertiserId);

        // Map flat model -> TikTok request body. Null properties are omitted by the serializer.
        var body = new CreateCampaignBody
        {
            AdvertiserId = advertiserId,
            CampaignName = campaign.CampaignName,
            ObjectiveType = campaign.ObjectiveType,
            BudgetMode = campaign.BudgetMode,
            Budget = campaign.Budget,
            OperationStatus = campaign.OperationStatus
        };

        var data = executionContext is null
            ? await apiClient.PostAsync<CreateCampaignData>("campaign/create/", body, cancellationToken)
            : await apiClient.PostAsync<CreateCampaignData>(executionContext, "campaign/create/", body, cancellationToken);

        // Map response -> model.
        campaign.CampaignId = data.CampaignId;
        return campaign;
    }

    /// <summary>The <c>campaign/create/</c> request body in TikTok's expected snake_case shape.</summary>
    private sealed class CreateCampaignBody
    {
        [JsonPropertyName("advertiser_id")] public required string AdvertiserId { get; set; }
        [JsonPropertyName("campaign_name")] public required string CampaignName { get; set; }
        [JsonPropertyName("objective_type")] public required string ObjectiveType { get; set; }
        [JsonPropertyName("budget_mode")] public string? BudgetMode { get; set; }
        [JsonPropertyName("budget")] public double? Budget { get; set; }
        [JsonPropertyName("operation_status")] public string? OperationStatus { get; set; }
    }

    /// <summary>The <c>data</c> payload returned by <c>campaign/create/</c>.</summary>
    private sealed class CreateCampaignData
    {
        [JsonPropertyName("campaign_id")] public string? CampaignId { get; set; }
    }
}
