using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Suss.TikTok.Client.Exceptions;
using Suss.TikTok.Client.Infrastructure;
using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Default <see cref="ITikTokAdService"/> implementation.
/// <para>
/// Maps flat <see cref="TikTokAd"/> models to TikTok's <c>ad/create/</c> batch request body. Each
/// ad becomes a "creative" entry; the required fields depend on the ad's
/// <see cref="TikTokAdFormat"/>:
/// <list type="bullet">
///   <item><description><see cref="TikTokAdFormat.SingleVideo"/> → <c>video_id</c> + cover <c>image_ids</c>.</description></item>
///   <item><description><see cref="TikTokAdFormat.SingleImage"/> → one or more <c>image_ids</c>.</description></item>
///   <item><description><see cref="TikTokAdFormat.SparkAd"/> → <c>tiktok_item_id</c> of the organic post.</description></item>
/// </list>
/// The service validates per-format requirements before sending and maps the returned ad ids back.
/// </para>
/// </summary>
/// <param name="apiClient">The transport abstraction used to call TikTok.</param>
/// <param name="logger">Logger for diagnostics.</param>
internal sealed class TikTokAdService(
    ITikTokApiClient apiClient,
    ILogger<TikTokAdService> logger) : ITikTokAdService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TikTokAd>> CreateAsync(
        string advertiserId,
        string adGroupId,
        IReadOnlyList<TikTokAd> ads,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adGroupId))
            throw new InvalidOperationException("adGroupId must be provided before creating ads.");
        if (ads is null || ads.Count == 0)
            throw new InvalidOperationException("At least one ad is required.");

        logger.LogInformation(
            "Creating {Count} ad(s) under ad group {AdGroupId} for advertiser {AdvertiserId}.",
            ads.Count, adGroupId, advertiserId);

        // Validate and map each ad into a creative entry.
        var creatives = ads.Select(MapToCreative).ToList();

        var body = new CreateAdBody
        {
            AdvertiserId = advertiserId,
            AdGroupId = adGroupId,
            Creatives = creatives
        };

        var data = await apiClient.PostAsync<CreateAdData>("ad/create/", body, cancellationToken);

        // Map returned ad ids back onto the input ads, preserving order.
        var returnedIds = data.AdIds ?? [];
        for (var i = 0; i < ads.Count && i < returnedIds.Count; i++)
            ads[i].AdId = returnedIds[i];

        return ads;
    }

    /// <summary>
    /// Validates per-format requirements and maps a single <see cref="TikTokAd"/> to a creative entry.
    /// </summary>
    private static AdCreative MapToCreative(TikTokAd ad)
    {
        var creative = new AdCreative
        {
            AdName = ad.AdName,
            DisplayName = ad.DisplayName,
            AdText = ad.AdText,
            CallToAction = ad.CallToAction,
            LandingPageUrl = ad.LandingPageUrl,
            IdentityId = ad.IdentityId,
            IdentityType = ad.IdentityType,
            OperationStatus = ad.OperationStatus
        };

        switch (ad.Format)
        {
            case TikTokAdFormat.SingleVideo:
                if (string.IsNullOrWhiteSpace(ad.VideoId) && string.IsNullOrWhiteSpace(ad.VideoUrl))
                    throw new InvalidOperationException($"Ad '{ad.AdName}' is SingleVideo but has no VideoId or VideoUrl.");
                creative.AdFormat = "SINGLE_VIDEO";
                creative.VideoId = ad.VideoId;
                creative.VideoUrl = ad.VideoUrl;
                // TikTok requires a cover image for video ads.
                if (!string.IsNullOrWhiteSpace(ad.CoverImageId))
                    creative.ImageIds = [ad.CoverImageId];
                if (!string.IsNullOrWhiteSpace(ad.CoverImageUrl))
                    creative.ImageUrls = [ad.CoverImageUrl];
                break;

            case TikTokAdFormat.SingleImage:
                if ((ad.ImageIds is null || ad.ImageIds.Count == 0) &&
                    (ad.ImageUrls is null || ad.ImageUrls.Count == 0))
                    throw new InvalidOperationException($"Ad '{ad.AdName}' is SingleImage but has no ImageIds or ImageUrls.");
                creative.AdFormat = "SINGLE_IMAGE";
                creative.ImageIds = ad.ImageIds;
                creative.ImageUrls = ad.ImageUrls;
                break;

            case TikTokAdFormat.SparkAd:
                if (string.IsNullOrWhiteSpace(ad.TikTokItemId))
                    throw new InvalidOperationException($"Ad '{ad.AdName}' is SparkAd but has no TikTokItemId for the organic post.");
                // Spark Ads boost an existing organic post and use the authorized identity.
                creative.AdFormat = "SINGLE_VIDEO";
                creative.TikTokItemId = ad.TikTokItemId;
                break;

            default:
                throw new InvalidOperationException($"Unsupported ad format '{ad.Format}'.");
        }

        return creative;
    }

    /// <summary>The <c>ad/create/</c> request body in TikTok's expected snake_case shape.</summary>
    private sealed class CreateAdBody
    {
        [JsonPropertyName("advertiser_id")] public required string AdvertiserId { get; set; }
        [JsonPropertyName("adgroup_id")] public required string AdGroupId { get; set; }
        [JsonPropertyName("creatives")] public required List<AdCreative> Creatives { get; set; }
    }

    /// <summary>A single creative entry within an <c>ad/create/</c> request.</summary>
    private sealed class AdCreative
    {
        [JsonPropertyName("ad_name")] public required string AdName { get; set; }
        [JsonPropertyName("ad_format")] public string? AdFormat { get; set; }
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
        [JsonPropertyName("ad_text")] public string? AdText { get; set; }
        [JsonPropertyName("call_to_action")] public string? CallToAction { get; set; }
        [JsonPropertyName("landing_page_url")] public string? LandingPageUrl { get; set; }
        [JsonPropertyName("video_id")] public string? VideoId { get; set; }
        [JsonPropertyName("video_url")] public string? VideoUrl { get; set; }
        [JsonPropertyName("image_ids")] public List<string>? ImageIds { get; set; }
        [JsonPropertyName("image_urls")] public List<string>? ImageUrls { get; set; }
        [JsonPropertyName("tiktok_item_id")] public string? TikTokItemId { get; set; }
        [JsonPropertyName("identity_id")] public string? IdentityId { get; set; }
        [JsonPropertyName("identity_type")] public string? IdentityType { get; set; }
        [JsonPropertyName("operation_status")] public string? OperationStatus { get; set; }
    }

    /// <summary>The <c>data</c> payload returned by <c>ad/create/</c>.</summary>
    private sealed class CreateAdData
    {
        [JsonPropertyName("ad_ids")] public List<string>? AdIds { get; set; }
    }
}
