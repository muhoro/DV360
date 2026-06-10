using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Default <see cref="ITikTokCampaignWorkflowService"/> implementation.
/// <para>
/// Coordinates the resource services in dependency order so consumers can create a fully linked
/// campaign with a single call. Mirrors the DV360 workflow pattern: each step's output (ids) feeds
/// the next step's input. The advertiser id is taken from the request when present, otherwise from
/// configured options.
/// </para>
/// </summary>
/// <param name="assetService">Uploads media assets in step 1.</param>
/// <param name="audienceService">Creates custom audiences in step 2.</param>
/// <param name="campaignService">Creates the campaign in step 3.</param>
/// <param name="adGroupService">Creates the ad group in step 4.</param>
/// <param name="adService">Creates the ads in step 5.</param>
/// <param name="options">Client options providing the default advertiser id.</param>
/// <param name="logger">Logger for workflow progress.</param>
internal sealed class TikTokCampaignWorkflowService(
    ITikTokAssetService assetService,
    ITikTokAudienceService audienceService,
    ITikTokCampaignService campaignService,
    ITikTokAdGroupService adGroupService,
    ITikTokAdService adService,
    IOptions<TikTokClientOptions> options,
    ILogger<TikTokCampaignWorkflowService> logger) : ITikTokCampaignWorkflowService
{
    private readonly TikTokClientOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<TikTokCampaignWorkflowResult> ExecuteAsync(
        TikTokCampaignWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var advertiserId = request.AdvertiserId ?? _options.AdvertiserId;
        logger.LogInformation("Starting TikTok campaign workflow for advertiser {AdvertiserId}.", advertiserId);

        // ---- Step 1: Upload assets so their ids can be referenced by ads. ----
        logger.LogInformation("Step 1/5: Uploading {Count} asset(s).", request.Assets.Count);
        foreach (var asset in request.Assets)
            await assetService.UploadAsync(advertiserId, asset, cancellationToken);

        // ---- Step 2: Create customer-match audiences. ----
        logger.LogInformation("Step 2/5: Creating {Count} audience(s).", request.Audiences.Count);
        foreach (var audience in request.Audiences)
            await audienceService.CreateFromFileAsync(advertiserId, audience, cancellationToken);

        // Auto-wire newly created audience ids into the ad group's included targeting.
        var createdAudienceIds = request.Audiences
            .Where(a => !string.IsNullOrWhiteSpace(a.AudienceId))
            .Select(a => a.AudienceId!)
            .ToList();

        if (createdAudienceIds.Count > 0)
        {
            request.AdGroup.IncludedAudienceIds ??= [];
            foreach (var id in createdAudienceIds)
            {
                if (!request.AdGroup.IncludedAudienceIds.Contains(id))
                    request.AdGroup.IncludedAudienceIds.Add(id);
            }
        }

        // ---- Step 3: Create the campaign. ----
        logger.LogInformation("Step 3/5: Creating campaign '{Name}'.", request.Campaign.CampaignName);
        await campaignService.CreateAsync(advertiserId, request.Campaign, cancellationToken);

        // ---- Step 4: Create the ad group under the campaign. ----
        request.AdGroup.CampaignId = request.Campaign.CampaignId;
        logger.LogInformation("Step 4/5: Creating ad group '{Name}' under campaign {CampaignId}.",
            request.AdGroup.AdGroupName, request.Campaign.CampaignId);
        await adGroupService.CreateAsync(advertiserId, request.AdGroup, cancellationToken);

        // ---- Step 5: Create the ads under the ad group. ----
        foreach (var ad in request.Ads)
            ad.AdGroupId = request.AdGroup.AdGroupId;

        logger.LogInformation("Step 5/5: Creating {Count} ad(s) under ad group {AdGroupId}.",
            request.Ads.Count, request.AdGroup.AdGroupId);
        await adService.CreateAsync(advertiserId, request.AdGroup.AdGroupId!, request.Ads, cancellationToken);

        logger.LogInformation("TikTok campaign workflow completed successfully.");

        return new TikTokCampaignWorkflowResult
        {
            Assets = request.Assets,
            Audiences = request.Audiences,
            Campaign = request.Campaign,
            AdGroup = request.AdGroup,
            Ads = request.Ads
        };
    }
}
