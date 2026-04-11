using Google;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using GoogleData = Google.Apis.DisplayVideo.v4.Data;
using TargetingTypeEnum = Google.Apis.DisplayVideo.v4.AdvertisersResource.LineItemsResource.TargetingTypesResource.AssignedTargetingOptionsResource.CreateRequest.TargetingTypeEnum;
using ListTargetingTypeEnum = Google.Apis.DisplayVideo.v4.AdvertisersResource.LineItemsResource.TargetingTypesResource.AssignedTargetingOptionsResource.ListRequest.TargetingTypeEnum;
using DeleteTargetingTypeEnum = Google.Apis.DisplayVideo.v4.AdvertisersResource.LineItemsResource.TargetingTypesResource.AssignedTargetingOptionsResource.DeleteRequest.TargetingTypeEnum;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Internal implementation of <see cref="ITargetingService"/> that creates
/// <c>AssignedTargetingOption</c> resources via the DV360 API v4.
/// <para>
/// Each targeting type maps to a specific <c>TargetingType</c> enum and a corresponding
/// <c>*AssignedTargetingOptionDetails</c> sub-object in the Google SDK. This service
/// iterates all non-null targeting lists in <see cref="Dv360LineItemTargeting"/> and
/// creates the appropriate API resources.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory that provides an authenticated <c>DisplayVideoService</c> instance.</param>
/// <param name="logger">Logger for structured diagnostic output.</param>
internal sealed class TargetingService(
    IDisplayVideoServiceFactory serviceFactory,
    ILogger<TargetingService> logger) : ITargetingService
{
    /// <inheritdoc />
    public async Task AssignTargetingAsync(long advertiserId, long lineItemId,
        Dv360LineItemTargeting targeting, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Assigning targeting options to line item {LineItemId} for advertiser {AdvertiserId}",
            lineItemId, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);

            // Process each targeting type that has values configured.
            if (targeting.GeoTargets is { Count: > 0 })
            {
                foreach (var geo in targeting.GeoTargets)
                {
                    var option = new GoogleData.AssignedTargetingOption
                    {
                        GeoRegionDetails = new GoogleData.GeoRegionAssignedTargetingOptionDetails
                        {
                            TargetingOptionId = geo.TargetingOptionId,
                            Negative = geo.Negative
                        }
                    };

                    await CreateAssignedTargetingOptionAsync(service, advertiserId, lineItemId,
                        TargetingTypeEnum.TARGETINGTYPEGEOREGION, option, cancellationToken);

                    logger.LogDebug("Assigned geo targeting {TargetingOptionId} (negative={Negative}) to line item {LineItemId}",
                        geo.TargetingOptionId, geo.Negative, lineItemId);
                }
            }

            if (targeting.DeviceTypeTargets is { Count: > 0 })
            {
                // DV360 assigns default device type targeting when a line item is created.
                // We must remove those defaults before assigning the requested device types
                // to avoid "already assigned" 400 errors from the API.
                await DeleteExistingAssignedTargetingOptionsAsync(service, advertiserId, lineItemId,
                    ListTargetingTypeEnum.TARGETINGTYPEDEVICETYPE,
                    DeleteTargetingTypeEnum.TARGETINGTYPEDEVICETYPE,
                    cancellationToken);

                foreach (var device in targeting.DeviceTypeTargets)
                {
                    var option = new GoogleData.AssignedTargetingOption
                    {
                        DeviceTypeDetails = new GoogleData.DeviceTypeAssignedTargetingOptionDetails
                        {
                            DeviceType = device.DeviceType
                        }
                    };

                    await CreateAssignedTargetingOptionAsync(service, advertiserId, lineItemId,
                        TargetingTypeEnum.TARGETINGTYPEDEVICETYPE, option, cancellationToken);

                    logger.LogDebug("Assigned device type targeting {DeviceType} to line item {LineItemId}",
                        device.DeviceType, lineItemId);
                }
            }

            if (targeting.BrowserTargets is { Count: > 0 })
            {
                foreach (var browser in targeting.BrowserTargets)
                {
                    var option = new GoogleData.AssignedTargetingOption
                    {
                        BrowserDetails = new GoogleData.BrowserAssignedTargetingOptionDetails
                        {
                            TargetingOptionId = browser.TargetingOptionId,
                            Negative = browser.Negative
                        }
                    };

                    await CreateAssignedTargetingOptionAsync(service, advertiserId, lineItemId,
                        TargetingTypeEnum.TARGETINGTYPEBROWSER, option, cancellationToken);

                    logger.LogDebug("Assigned browser targeting {TargetingOptionId} (negative={Negative}) to line item {LineItemId}",
                        browser.TargetingOptionId, browser.Negative, lineItemId);
                }
            }

            if (targeting.ChannelTargets is { Count: > 0 })
            {
                foreach (var channel in targeting.ChannelTargets)
                {
                    var option = new GoogleData.AssignedTargetingOption
                    {
                        ChannelDetails = new GoogleData.ChannelAssignedTargetingOptionDetails
                        {
                            ChannelId = channel.ChannelId,
                            Negative = channel.Negative
                        }
                    };

                    await CreateAssignedTargetingOptionAsync(service, advertiserId, lineItemId,
                        TargetingTypeEnum.TARGETINGTYPECHANNEL, option, cancellationToken);

                    logger.LogDebug("Assigned channel targeting {ChannelId} (negative={Negative}) to line item {LineItemId}",
                        channel.ChannelId, channel.Negative, lineItemId);
                }
            }

            if (targeting.ContentLabelExclusions is { Count: > 0 })
            {
                foreach (var label in targeting.ContentLabelExclusions)
                {
                    var option = new GoogleData.AssignedTargetingOption
                    {
                        DigitalContentLabelExclusionDetails = new GoogleData.DigitalContentLabelAssignedTargetingOptionDetails
                        {
                            ExcludedContentRatingTier = label.ContentLabelType
                        }
                    };

                    await CreateAssignedTargetingOptionAsync(service, advertiserId, lineItemId,
                        TargetingTypeEnum.TARGETINGTYPEDIGITALCONTENTLABELEXCLUSION, option, cancellationToken);

                    logger.LogDebug("Assigned content label exclusion {ContentLabelType} to line item {LineItemId}",
                        label.ContentLabelType, lineItemId);
                }
            }

            if (targeting.ContentInstreamPositionTargets is { Count: > 0 })
            {
                foreach (var position in targeting.ContentInstreamPositionTargets)
                {
                    var option = new GoogleData.AssignedTargetingOption
                    {
                        ContentInstreamPositionDetails = new GoogleData.ContentInstreamPositionAssignedTargetingOptionDetails
                        {
                            ContentInstreamPosition = position.ContentInstreamPosition
                        }
                    };

                    await CreateAssignedTargetingOptionAsync(service, advertiserId, lineItemId,
                        TargetingTypeEnum.TARGETINGTYPECONTENTINSTREAMPOSITION, option, cancellationToken);

                    logger.LogDebug("Assigned content instream position {Position} to line item {LineItemId}",
                        position.ContentInstreamPosition, lineItemId);
                }
            }

            if (targeting.ViewabilityTargets is { Count: > 0 })
            {
                foreach (var viewability in targeting.ViewabilityTargets)
                {
                    var option = new GoogleData.AssignedTargetingOption
                    {
                        ViewabilityDetails = new GoogleData.ViewabilityAssignedTargetingOptionDetails
                        {
                            Viewability = viewability.TargetingOptionId
                        }
                    };

                    await CreateAssignedTargetingOptionAsync(service, advertiserId, lineItemId,
                        TargetingTypeEnum.TARGETINGTYPEVIEWABILITY, option, cancellationToken);

                    logger.LogDebug("Assigned viewability targeting {TargetingOptionId} to line item {LineItemId}",
                        viewability.TargetingOptionId, lineItemId);
                }
            }

            logger.LogInformation("Successfully assigned all targeting options to line item {LineItemId}", lineItemId);
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to assign targeting options to line item {LineItemId} for advertiser {AdvertiserId}",
                lineItemId, advertiserId);
            throw new Dv360ApiException(
                $"Failed to assign targeting options to line item {lineItemId} for advertiser {advertiserId}.", ex);
        }
    }

    /// <summary>
    /// Lists and deletes all existing <c>AssignedTargetingOption</c> resources for a given
    /// targeting type on the specified line item. This is necessary for targeting types
    /// (such as device type) where DV360 creates default assignments automatically when
    /// the line item is created.
    /// </summary>
    private static async Task DeleteExistingAssignedTargetingOptionsAsync(
        Google.Apis.DisplayVideo.v4.DisplayVideoService service,
        long advertiserId, long lineItemId,
        ListTargetingTypeEnum listTargetingType,
        DeleteTargetingTypeEnum deleteTargetingType,
        CancellationToken cancellationToken)
    {
        var listRequest = service.Advertisers.LineItems.TargetingTypes.AssignedTargetingOptions
            .List(advertiserId, lineItemId, listTargetingType);

        var listResult = await listRequest.ExecuteAsync(cancellationToken);

        if (listResult.AssignedTargetingOptions is not { Count: > 0 })
            return;

        foreach (var existing in listResult.AssignedTargetingOptions)
        {
            var deleteRequest = service.Advertisers.LineItems.TargetingTypes.AssignedTargetingOptions
                .Delete(advertiserId, lineItemId, deleteTargetingType, existing.AssignedTargetingOptionId);

            await deleteRequest.ExecuteAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Creates a single <c>AssignedTargetingOption</c> under the specified line item and targeting type.
    /// </summary>
    private static async Task CreateAssignedTargetingOptionAsync(
        Google.Apis.DisplayVideo.v4.DisplayVideoService service,
        long advertiserId, long lineItemId, TargetingTypeEnum targetingType,
        GoogleData.AssignedTargetingOption option, CancellationToken cancellationToken)
    {
        var request = service.Advertisers.LineItems.TargetingTypes.AssignedTargetingOptions
            .Create(option, advertiserId, lineItemId, targetingType);

        await request.ExecuteAsync(cancellationToken);
    }
}
