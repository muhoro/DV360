using Google;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using GoogleData = Google.Apis.DisplayVideo.v4.Data;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Internal implementation of <see cref="ICreativeService"/> that manages DV360 creatives
/// via the Google Display &amp; Video 360 SDK.
/// <para>
/// Maps between the library’s flat <see cref="Dv360Creative"/> model and the Google SDK’s
/// <c>Creative</c> type, flattening the nested <c>Dimensions</c> object into individual
/// width/height properties. Supports hosted and third-party tag creatives.
/// </para>
/// <para>
/// When a creative includes <see cref="Dv360Creative.Assets"/> with file paths, the service
/// automatically uploads each asset via <see cref="IAssetService"/> before creating the
/// creative, and wires the resulting <c>MediaId</c> values into the creative’s
/// <c>AssetAssociation</c> list.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory that provides an authenticated <c>DisplayVideoService</c> instance.</param>
/// <param name="assetService">Service for uploading media assets to DV360.</param>
/// <param name="logger">Logger for structured diagnostic output.</param>
internal sealed class CreativeService(
    IDisplayVideoServiceFactory serviceFactory,
    IAssetService assetService,
    ILogger<CreativeService> logger) : ICreativeService
{
    /// <inheritdoc />
    public async Task<Dv360Creative> CreateAsync(long advertiserId, Dv360Creative creative, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating creative '{DisplayName}' for advertiser {AdvertiserId}", creative.DisplayName, advertiserId);

        try
        {
            // Step 1: Upload any local asset files before creating the creative.
            // Each upload returns a MediaId that the creative references via AssetAssociation.
            if (creative.Assets is { Count: > 0 })
            {
                logger.LogInformation("Uploading {Count} asset(s) for creative '{DisplayName}'",
                    creative.Assets.Count, creative.DisplayName);

                foreach (var asset in creative.Assets)
                {
                    if (!string.IsNullOrWhiteSpace(asset.FilePath) || !string.IsNullOrWhiteSpace(asset.Url))
                    {
                        await assetService.UploadAsync(advertiserId, asset, cancellationToken);
                    }
                }
            }

            var service = await serviceFactory.CreateAsync(cancellationToken);

            // Step 2: Map the flat Dv360Creative to the Google SDK's Creative structure.
            var body = new GoogleData.Creative
            {
                DisplayName = creative.DisplayName,
                EntityStatus = creative.EntityStatus,
                CreativeType = creative.CreativeType,
                HostingSource = creative.HostingSource,
                // Only populate Dimensions when both width and height are specified.
                Dimensions = (creative.WidthPixels is not null && creative.HeightPixels is not null)
                    ? new GoogleData.Dimensions
                    {
                        WidthPixels = creative.WidthPixels,
                        HeightPixels = creative.HeightPixels
                    }
                    : null,
                ThirdPartyTag = creative.ThirdPartyTag,
                // Wire uploaded asset MediaIds into the creative's AssetAssociation list.
                Assets = MapAssetsToGoogle(creative.Assets),
                // Wire exit events (click-through URLs) — DV360 requires at least one.
                ExitEvents = MapExitEventsToGoogle(creative.ExitEvents)
            };

            var request = service.Advertisers.Creatives.Create(body, advertiserId);
            var result = await request.ExecuteAsync(cancellationToken);

            // Populate the server-assigned creative ID back onto the caller's model.
            creative.CreativeId = result.CreativeId;
            logger.LogInformation("Created creative {CreativeId} for advertiser {AdvertiserId}", result.CreativeId, advertiserId);

            return creative;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to create creative for advertiser {AdvertiserId}", advertiserId);
            throw new Dv360ApiException($"Failed to create creative for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dv360Creative?> GetAsync(long advertiserId, long creativeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var request = service.Advertisers.Creatives.Get(advertiserId, creativeId);
            var result = await request.ExecuteAsync(cancellationToken);

            return MapFromGoogle(result);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Return null instead of throwing when the creative does not exist.
            return null;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to get creative {creativeId} for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dv360Creative>> ListAsync(long advertiserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var creatives = new List<Dv360Creative>();
            string? pageToken = null;

            // Iterate through all pages of results until no NextPageToken is returned.
            do
            {
                var request = service.Advertisers.Creatives.List(advertiserId);
                request.PageToken = pageToken;

                var result = await request.ExecuteAsync(cancellationToken);
                if (result.Creatives is not null)
                    creatives.AddRange(result.Creatives.Select(MapFromGoogle));

                pageToken = result.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return creatives;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to list creatives for advertiser {advertiserId}.", ex);
        }
    }

    /// <summary>
    /// Converts uploaded <see cref="Dv360CreativeAsset"/> entries into the Google SDK's
    /// <see cref="GoogleData.AssetAssociation"/> list. Only assets with a valid
    /// <see cref="Dv360CreativeAsset.MediaId"/> (i.e., already uploaded) are included.
    /// </summary>
    private static IList<GoogleData.AssetAssociation>? MapAssetsToGoogle(List<Dv360CreativeAsset>? assets)
    {
        if (assets is not { Count: > 0 })
            return null;

        var associations = new List<GoogleData.AssetAssociation>();

        foreach (var asset in assets)
        {
            if (asset.MediaId is null)
                continue;

            associations.Add(new GoogleData.AssetAssociation
            {
                Asset = new GoogleData.Asset
                {
                    MediaId = asset.MediaId
                },
                Role = asset.Role
            });
        }

        return associations.Count > 0 ? associations : null;
    }

    /// <summary>
    /// Maps the Google SDK's <see cref="GoogleData.AssetAssociation"/> list back to
    /// <see cref="Dv360CreativeAsset"/> entries with <c>MediaId</c>, <c>Content</c>,
    /// and <c>Role</c> populated.
    /// </summary>
    private static List<Dv360CreativeAsset>? MapAssetsFromGoogle(IList<GoogleData.AssetAssociation>? associations)
    {
        if (associations is not { Count: > 0 })
            return null;

        return associations.Select(a => new Dv360CreativeAsset
        {
            MediaId = a.Asset?.MediaId,
            Content = a.Asset?.Content,
            Role = a.Role ?? "ASSET_ROLE_UNSPECIFIED"
        }).ToList();
    }

    /// <summary>
    /// Maps a Google SDK <see cref="GoogleData.Creative"/> to the library's flat
    /// <see cref="Dv360Creative"/> model by extracting nested dimension and asset data.
    /// </summary>
    private static Dv360Creative MapFromGoogle(GoogleData.Creative c) => new()
    {
        CreativeId = c.CreativeId,
        DisplayName = c.DisplayName ?? string.Empty,
        EntityStatus = c.EntityStatus ?? "ENTITY_STATUS_UNSPECIFIED",
        CreativeType = c.CreativeType ?? string.Empty,
        HostingSource = c.HostingSource,
        WidthPixels = c.Dimensions?.WidthPixels,
        HeightPixels = c.Dimensions?.HeightPixels,
        ThirdPartyTag = c.ThirdPartyTag,
        Assets = MapAssetsFromGoogle(c.Assets),
        ExitEvents = MapExitEventsFromGoogle(c.ExitEvents)
    };

    /// <summary>
    /// Converts <see cref="Dv360ExitEvent"/> entries into the Google SDK's
    /// <see cref="GoogleData.ExitEvent"/> list.
    /// </summary>
    private static IList<GoogleData.ExitEvent>? MapExitEventsToGoogle(List<Dv360ExitEvent>? exitEvents)
    {
        if (exitEvents is not { Count: > 0 })
            return null;

        return exitEvents.Select(e => new GoogleData.ExitEvent
        {
            Type = e.Type,
            Url = e.Url
        }).ToList<GoogleData.ExitEvent>();
    }

    /// <summary>
    /// Maps the Google SDK's <see cref="GoogleData.ExitEvent"/> list back to
    /// <see cref="Dv360ExitEvent"/> entries.
    /// </summary>
    private static List<Dv360ExitEvent>? MapExitEventsFromGoogle(IList<GoogleData.ExitEvent>? exitEvents)
    {
        if (exitEvents is not { Count: > 0 })
            return null;

        return exitEvents.Select(e => new Dv360ExitEvent
        {
            Type = e.Type ?? "EXIT_EVENT_TYPE_DEFAULT",
            Url = e.Url ?? string.Empty
        }).ToList();
    }
}
