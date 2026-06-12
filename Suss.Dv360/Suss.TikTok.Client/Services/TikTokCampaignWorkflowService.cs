using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Suss.TikTok.Client.Auth;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Exceptions;
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
/// <param name="oauthService">Performs stateless TikTok OAuth calls used by workflow-level linking.</param>
/// <param name="options">Client options providing the default advertiser id.</param>
/// <param name="logger">Logger for workflow progress.</param>
internal sealed class TikTokCampaignWorkflowService(
    ITikTokAssetService assetService,
    ITikTokAudienceService audienceService,
    ITikTokCampaignService campaignService,
    ITikTokAdGroupService adGroupService,
    ITikTokAdService adService,
    ITikTokOAuthService oauthService,
    IOptions<TikTokClientOptions> options,
    ILogger<TikTokCampaignWorkflowService> logger) : ITikTokCampaignWorkflowService
{
    private readonly TikTokClientOptions _options = options.Value;

    /// <inheritdoc />
    public string BuildAuthorizationUrl(
        TikTokAuthMode authMode,
        string redirectUri,
        string state,
        IEnumerable<string>? scopes = null,
        IReadOnlyDictionary<string, string?>? additionalParameters = null)
        => oauthService.BuildAuthorizationUrl(authMode, redirectUri, state, scopes, additionalParameters);

    /// <inheritdoc />
    public Task<TikTokOAuthTokenResult> ExchangeAuthorizationCodeAsync(
        string authCode,
        CancellationToken cancellationToken = default)
        => oauthService.ExchangeAuthorizationCodeAsync(authCode, cancellationToken);

    /// <inheritdoc />
    public TikTokClientAdvertiserConnection CreateClientAdvertiserConnection(
        TikTokOAuthTokenResult token,
        string advertiserId)
    {
        EnsureUsableToken(token);

        if (string.IsNullOrWhiteSpace(advertiserId))
            throw new TikTokApiException("A customer advertiser id is required to create a client advertiser connection.");

        if (token.AdvertiserIds.Count > 0 && !token.AdvertiserIds.Contains(advertiserId))
        {
            throw new TikTokApiException(
                $"Advertiser id '{advertiserId}' was not present in the TikTok OAuth token response.");
        }

        var advertiser = token.Advertisers.FirstOrDefault(advertiser => advertiser.AdvertiserId == advertiserId);
        return new TikTokClientAdvertiserConnection
        {
            AdvertiserId = advertiserId,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresIn = token.ExpiresIn,
            RefreshExpiresIn = token.RefreshExpiresIn,
            Scope = token.Scope,
            AccessTokenExpiresAt = token.ExpiresIn is null ? null : DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn.Value),
            RefreshTokenExpiresAt = token.RefreshExpiresIn is null ? null : DateTimeOffset.UtcNow.AddSeconds(token.RefreshExpiresIn.Value),
            AdvertiserName = advertiser?.Name,
            AdvertiserStatus = advertiser?.Status,
            Currency = advertiser?.Currency,
            Timezone = advertiser?.Timezone,
            Country = advertiser?.Country
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<TikTokClientAdvertiserConnection> CreateClientAdvertiserConnections(
        TikTokOAuthTokenResult token)
    {
        EnsureUsableToken(token);

        if (token.AdvertiserIds.Count == 0)
        {
            throw new TikTokApiException(
                "TikTok did not return advertiser ids in the OAuth token response. " +
                "Call the advertiser-list endpoint for this token, then create a connection for the selected advertiser id.");
        }

        return token.AdvertiserIds
            .Where(advertiserId => !string.IsNullOrWhiteSpace(advertiserId))
            .Select(advertiserId => CreateClientAdvertiserConnection(token, advertiserId))
            .ToArray();
    }

    /// <inheritdoc />
    public TikTokManagedAdvertiserConnection GetManagedAdvertiserConnection()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new TikTokApiException(
                $"Managed advertiser account mode requires '{nameof(TikTokClientOptions.AccessToken)}' " +
                "to contain your platform-owned long-lived TikTok access token.");
        }

        if (string.IsNullOrWhiteSpace(_options.AdvertiserId))
        {
            throw new TikTokApiException(
                $"Managed advertiser account mode requires '{nameof(TikTokClientOptions.AdvertiserId)}' " +
                "to contain your platform-owned TikTok advertiser id.");
        }

        return new TikTokManagedAdvertiserConnection
        {
            AdvertiserId = _options.AdvertiserId,
            AccessToken = _options.AccessToken,
            IdentityId = _options.ManagedIdentityId,
            IdentityType = _options.ManagedIdentityType
        };
    }

    /// <inheritdoc />
    public async Task<TikTokCampaignWorkflowResult> ExecuteAsync(
        TikTokCampaignWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var executionContext = request.ExecutionContext;
        var advertiserId = request.AdvertiserId ?? executionContext?.AdvertiserId ?? _options.AdvertiserId;
        logger.LogInformation("Starting TikTok campaign workflow for advertiser {AdvertiserId}.", advertiserId);

        // ---- Step 1: Upload local/in-memory assets so their ids can be referenced by ads. URL assets
        // are passed directly to ad creation instead.
        var uploadAssets = request.Assets
            .Where(asset => GetAssetUrl(asset) is null)
            .ToList();
        logger.LogInformation("Step 1/5: Uploading {Count} asset(s).", uploadAssets.Count);
        foreach (var asset in uploadAssets)
        {
            if (executionContext is null)
                await assetService.UploadAsync(advertiserId, asset, cancellationToken);
            else
                await assetService.UploadAsync(executionContext, asset, cancellationToken);
        }

        // ---- Step 2: Create customer-match audiences. ----
        logger.LogInformation("Step 2/5: Creating {Count} audience(s).", request.Audiences.Count);
        foreach (var audience in request.Audiences)
        {
            if (executionContext is null)
                await audienceService.CreateFromFileAsync(advertiserId, audience, cancellationToken);
            else
                await audienceService.CreateFromFileAsync(executionContext, audience, cancellationToken);
        }

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
        if (executionContext is null)
            await campaignService.CreateAsync(advertiserId, request.Campaign, cancellationToken);
        else
            await campaignService.CreateAsync(executionContext, request.Campaign, cancellationToken);

        // ---- Step 4: Create the ad group under the campaign. ----
        request.AdGroup.CampaignId = request.Campaign.CampaignId;
        logger.LogInformation("Step 4/5: Creating ad group '{Name}' under campaign {CampaignId}.",
            request.AdGroup.AdGroupName, request.Campaign.CampaignId);
        if (executionContext is null)
            await adGroupService.CreateAsync(advertiserId, request.AdGroup, cancellationToken);
        else
            await adGroupService.CreateAsync(executionContext, request.AdGroup, cancellationToken);

        // ---- Step 5: Create the ads under the ad group. ----
        foreach (var ad in request.Ads)
            ad.AdGroupId = request.AdGroup.AdGroupId;

        WireCampaignAssetsToAds(request.Assets, request.Ads);

        logger.LogInformation("Step 5/5: Creating {Count} ad(s) under ad group {AdGroupId}.",
            request.Ads.Count, request.AdGroup.AdGroupId);
        if (executionContext is null)
            await adService.CreateAsync(advertiserId, request.AdGroup.AdGroupId!, request.Ads, cancellationToken);
        else
            await adService.CreateAsync(executionContext, request.AdGroup.AdGroupId!, request.Ads, cancellationToken);

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

    private static void WireCampaignAssetsToAds(IReadOnlyList<TikTokAsset> assets, IReadOnlyList<TikTokAd> ads)
    {
        foreach (var ad in ads)
        {
            if (ad.Format == TikTokAdFormat.SingleVideo)
            {
                var videoAsset = assets.FirstOrDefault(asset => asset.AssetType == TikTokAssetType.Video);
                var imageAsset = assets.FirstOrDefault(asset => asset.AssetType == TikTokAssetType.Image);

                ad.VideoId ??= videoAsset?.VideoId;
                ad.VideoUrl ??= GetAssetUrl(videoAsset);
                ad.CoverImageId ??= imageAsset?.ImageId;
                ad.CoverImageUrl ??= GetAssetUrl(imageAsset);
            }

            if (ad.Format == TikTokAdFormat.SingleImage)
            {
                var imageIds = assets
                    .Where(asset => asset.AssetType == TikTokAssetType.Image && !string.IsNullOrWhiteSpace(asset.ImageId))
                    .Select(asset => asset.ImageId!)
                    .ToList();
                var imageUrls = assets
                    .Where(asset => asset.AssetType == TikTokAssetType.Image)
                    .Select(GetAssetUrl)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => url!)
                    .ToList();

                if (ad.ImageIds is null && imageIds.Count > 0)
                    ad.ImageIds = imageIds;
                if (ad.ImageUrls is null && imageUrls.Count > 0)
                    ad.ImageUrls = imageUrls;
            }
        }
    }

    private static string? GetAssetUrl(TikTokAsset? asset)
    {
        if (asset is null)
            return null;

        var candidate = !string.IsNullOrWhiteSpace(asset.AssetUrl)
            ? asset.AssetUrl
            : asset.FilePath;

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.ToString()
            : null;
    }

    private static void EnsureUsableToken(TikTokOAuthTokenResult token)
    {
        if (token is null)
            throw new TikTokApiException("A TikTok OAuth token result is required to create a client advertiser connection.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new TikTokApiException("The TikTok OAuth token result does not contain an access token.");
    }
}
