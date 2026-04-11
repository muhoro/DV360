using Google;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using GoogleData = Google.Apis.DisplayVideo.v4.Data;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Internal implementation of <see cref="ILineItemService"/> that manages DV360 line items
/// and creative assignments via the Google Display &amp; Video 360 SDK.
/// <para>
/// Maps between the library's flat <see cref="Dv360LineItem"/> model and the Google SDK's
/// nested structure, which stores budget in <c>LineItemBudget</c>, flight dates in
/// <c>LineItemFlight</c>, and pacing in <c>Pacing</c>.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory that provides an authenticated <c>DisplayVideoService</c> instance.</param>
/// <param name="logger">Logger for structured diagnostic output.</param>
internal sealed class LineItemService(
    IDisplayVideoServiceFactory serviceFactory,
    ILogger<LineItemService> logger) : ILineItemService
{
    /// <inheritdoc />
    public async Task<Dv360LineItem> CreateAsync(long advertiserId, Dv360LineItem lineItem, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating line item '{DisplayName}' for advertiser {AdvertiserId}", lineItem.DisplayName, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var body = MapToGoogle(lineItem);

            var request = service.Advertisers.LineItems.Create(body, advertiserId);
            var result = await request.ExecuteAsync(cancellationToken);

            // Populate the server-assigned line item ID back onto the caller's model.
            lineItem.LineItemId = result.LineItemId;
            logger.LogInformation("Created line item {LineItemId} for advertiser {AdvertiserId}", result.LineItemId, advertiserId);

            return lineItem;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to create line item for advertiser {AdvertiserId}", advertiserId);
            throw new Dv360ApiException($"Failed to create line item for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dv360LineItem?> GetAsync(long advertiserId, long lineItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var request = service.Advertisers.LineItems.Get(advertiserId, lineItemId);
            var result = await request.ExecuteAsync(cancellationToken);

            return MapFromGoogle(result);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Return null instead of throwing when the line item does not exist.
            return null;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to get line item {lineItemId} for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Dv360LineItem>> ListAsync(long advertiserId, CancellationToken cancellationToken = default)
        => ListAsync(advertiserId, options: null, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dv360LineItem>> ListAsync(long advertiserId, LineItemListOptions? options, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var lineItems = new List<Dv360LineItem>();
            string? pageToken = null;

            // Iterate through all pages of results until no NextPageToken is returned.
            do
            {
                var request = service.Advertisers.LineItems.List(advertiserId);
                request.PageToken = pageToken;

                if (options?.PageSize is not null)
                    request.PageSize = options.PageSize;

                if (!string.IsNullOrEmpty(options?.OrderBy))
                    request.OrderBy = options.OrderBy;

                if (!string.IsNullOrEmpty(options?.Filter))
                    request.Filter = options.Filter;

                var result = await request.ExecuteAsync(cancellationToken);
                if (result.LineItems is not null)
                    lineItems.AddRange(result.LineItems.Select(MapFromGoogle));

                pageToken = result.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return lineItems;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to list line items for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dv360LineItem> PatchAsync(long advertiserId, long lineItemId, Dv360LineItem lineItem, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Patching line item {LineItemId} for advertiser {AdvertiserId}", lineItemId, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var body = MapToGoogle(lineItem);

            var patchRequest = service.Advertisers.LineItems.Patch(body, advertiserId, lineItemId);
            patchRequest.UpdateMask = string.Join(",",
                "displayName",
                "entityStatus",
                "flight",
                "budget",
                "pacing",
                "bidStrategy",
                "frequencyCap",
                "partnerRevenueModel",
                "conversionCounting",
                "integrationDetails",
                "excludeNewExchanges",
                "containsEuPoliticalAds",
                "creativeIds");
            var result = await patchRequest.ExecuteAsync(cancellationToken);

            lineItem.LineItemId = result.LineItemId;
            logger.LogInformation("Patched line item {LineItemId} for advertiser {AdvertiserId}", result.LineItemId, advertiserId);

            return lineItem;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to patch line item {LineItemId} for advertiser {AdvertiserId}", lineItemId, advertiserId);
            throw new Dv360ApiException($"Failed to patch line item {lineItemId} for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long advertiserId, long lineItemId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting line item {LineItemId} for advertiser {AdvertiserId}", lineItemId, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var request = service.Advertisers.LineItems.Delete(advertiserId, lineItemId);
            await request.ExecuteAsync(cancellationToken);

            logger.LogInformation("Deleted line item {LineItemId} for advertiser {AdvertiserId}", lineItemId, advertiserId);
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to delete line item {LineItemId} for advertiser {AdvertiserId}", lineItemId, advertiserId);
            throw new Dv360ApiException($"Failed to delete line item {lineItemId} for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task AssignCreativeAsync(long advertiserId, long lineItemId, long creativeId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Assigning creative {CreativeId} to line item {LineItemId}", creativeId, lineItemId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);

            // Retrieve the current line item to read its existing creative assignments.
            var getRequest = service.Advertisers.LineItems.Get(advertiserId, lineItemId);
            var lineItem = await getRequest.ExecuteAsync(cancellationToken);

            // Add the creative ID to the list if not already present.
            var creativeIds = lineItem.CreativeIds?.ToList() ?? [];
            if (!creativeIds.Contains(creativeId))
                creativeIds.Add(creativeId);

            lineItem.CreativeIds = creativeIds;

            // Patch the line item with the updated creative assignments.
            var patchRequest = service.Advertisers.LineItems.Patch(lineItem, advertiserId, lineItemId);
            patchRequest.UpdateMask = "creativeIds";
            await patchRequest.ExecuteAsync(cancellationToken);

            logger.LogInformation("Assigned creative {CreativeId} to line item {LineItemId}", creativeId, lineItemId);
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to assign creative {CreativeId} to line item {LineItemId}", creativeId, lineItemId);
            throw new Dv360ApiException($"Failed to assign creative {creativeId} to line item {lineItemId}.", ex);
        }
    }

    /// <summary>
    /// Maps the library's flat <see cref="Dv360LineItem"/> model to the Google SDK's
    /// nested <see cref="GoogleData.LineItem"/> structure for create and patch operations.
    /// </summary>
    private static GoogleData.LineItem MapToGoogle(Dv360LineItem lineItem)
    {
        var body = new GoogleData.LineItem
        {
            CampaignId = lineItem.CampaignId,
            InsertionOrderId = lineItem.InsertionOrderId,
            DisplayName = lineItem.DisplayName,
            EntityStatus = lineItem.EntityStatus,
            LineItemType = lineItem.LineItemType,
            Budget = new GoogleData.LineItemBudget
            {
                BudgetAllocationType = lineItem.BudgetAllocationType,
                BudgetUnit = lineItem.BudgetUnit,
                MaxAmount = lineItem.MaxBudgetAmountMicros
            },
            Flight = new GoogleData.LineItemFlight
            {
                FlightDateType = lineItem.FlightDateType,
                DateRange = lineItem.FlightDateType == "LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM"
                    ? new GoogleData.DateRange
                    {
                        StartDate = GoogleTypeMapper.ToGoogleDate(lineItem.StartDate),
                        EndDate = GoogleTypeMapper.ToGoogleDate(lineItem.EndDate)
                    }
                    : null
            },
            Pacing = new GoogleData.Pacing
            {
                PacingPeriod = lineItem.PacingPeriod,
                PacingType = lineItem.PacingType,
                DailyMaxMicros = lineItem.PacingType == "PACING_TYPE_AHEAD"
                    ? lineItem.DailyMaxMicros
                    : null
            },
            BidStrategy = MapBidStrategyToGoogle(lineItem),
            FrequencyCap = MapFrequencyCapToGoogle(lineItem),
            PartnerRevenueModel = new GoogleData.PartnerRevenueModel
            {
                MarkupType = lineItem.PartnerRevenueModelMarkupType,
                MarkupAmount = lineItem.PartnerRevenueModelMarkupAmount
            },
            ExcludeNewExchanges = lineItem.ExcludeNewExchanges,
            ContainsEuPoliticalAds = lineItem.ContainsEuPoliticalAds
        };

        // Conversion counting
        if (lineItem.ConversionCounting is not null)
        {
            body.ConversionCounting = new GoogleData.ConversionCountingConfig
            {
                PostViewCountPercentageMillis = lineItem.ConversionCounting.PostViewCountPercentageMillis
            };

            if (lineItem.ConversionCounting.FloodlightActivityConfigs is { Count: > 0 })
            {
                body.ConversionCounting.FloodlightActivityConfigs = lineItem.ConversionCounting.FloodlightActivityConfigs
                    .Select(f => new GoogleData.TrackingFloodlightActivityConfig
                    {
                        FloodlightActivityId = f.FloodlightActivityId,
                        PostClickLookbackWindowDays = f.PostClickLookbackWindowDays,
                        PostViewLookbackWindowDays = f.PostViewLookbackWindowDays
                    })
                    .ToList();
            }
        }

        // Integration details
        if (!string.IsNullOrEmpty(lineItem.IntegrationCode) || !string.IsNullOrEmpty(lineItem.IntegrationDetails))
        {
            body.IntegrationDetails = new GoogleData.IntegrationDetails
            {
                IntegrationCode = lineItem.IntegrationCode,
                Details = lineItem.IntegrationDetails
            };
        }

        // Mobile app (required for app install line item types)
        if (!string.IsNullOrEmpty(lineItem.MobileAppId))
        {
            body.MobileApp = new GoogleData.MobileApp
            {
                AppId = lineItem.MobileAppId,
                Platform = lineItem.MobileAppPlatform
            };
        }

        return body;
    }

    /// <summary>
    /// Maps a Google SDK <see cref="GoogleData.LineItem"/> to the library's flat
    /// <see cref="Dv360LineItem"/> model by extracting nested budget, flight, pacing,
    /// frequency cap, partner revenue, conversion counting, and integration data.
    /// </summary>
    private static Dv360LineItem MapFromGoogle(GoogleData.LineItem li) => new()
    {
        LineItemId = li.LineItemId,
        CampaignId = li.CampaignId ?? 0,
        InsertionOrderId = li.InsertionOrderId ?? 0,
        DisplayName = li.DisplayName ?? string.Empty,
        LineItemType = li.LineItemType ?? string.Empty,
        EntityStatus = li.EntityStatus ?? "ENTITY_STATUS_DRAFT",

        // Flight
        FlightDateType = li.Flight?.FlightDateType ?? "LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM",
        StartDate = GoogleTypeMapper.FromGoogleDate(li.Flight?.DateRange?.StartDate),
        EndDate = GoogleTypeMapper.FromGoogleDate(li.Flight?.DateRange?.EndDate),

        // Budget
        BudgetAllocationType = li.Budget?.BudgetAllocationType ?? string.Empty,
        BudgetUnit = li.Budget?.BudgetUnit ?? string.Empty,
        MaxBudgetAmountMicros = li.Budget?.MaxAmount ?? 0,

        // Pacing
        PacingPeriod = li.Pacing?.PacingPeriod ?? string.Empty,
        PacingType = li.Pacing?.PacingType ?? string.Empty,
        DailyMaxMicros = li.Pacing?.DailyMaxMicros ?? 0,

        // Bid strategy
        FixedBidAmountMicros = li.BidStrategy?.FixedBid?.BidAmountMicros,
        MaximizeSpendPerformanceGoalType = li.BidStrategy?.MaximizeSpendAutoBid?.PerformanceGoalType,
        MaxAverageCpmBidAmountMicros = li.BidStrategy?.MaximizeSpendAutoBid?.MaxAverageCpmBidAmountMicros,

        // Frequency cap
        FrequencyCapUnlimited = li.FrequencyCap?.Unlimited ?? true,
        FrequencyCapMaxImpressions = li.FrequencyCap?.MaxImpressions,
        FrequencyCapTimeUnit = li.FrequencyCap?.TimeUnit,
        FrequencyCapTimeUnitCount = li.FrequencyCap?.TimeUnitCount,

        // Partner revenue model
        PartnerRevenueModelMarkupType = li.PartnerRevenueModel?.MarkupType
            ?? "PARTNER_REVENUE_MODEL_MARKUP_TYPE_TOTAL_MEDIA_COST_MARKUP",
        PartnerRevenueModelMarkupAmount = li.PartnerRevenueModel?.MarkupAmount ?? 0,

        // Conversion counting
        ConversionCounting = MapConversionCountingFromGoogle(li.ConversionCounting),

        // Integration details
        IntegrationCode = li.IntegrationDetails?.IntegrationCode,
        IntegrationDetails = li.IntegrationDetails?.Details,

        // Exchanges
        ExcludeNewExchanges = li.ExcludeNewExchanges ?? false,

        // EU political ads
        ContainsEuPoliticalAds = li.ContainsEuPoliticalAds ?? "DOES_NOT_CONTAIN_EU_POLITICAL_ADVERTISING",

        // Mobile app
        MobileAppId = li.MobileApp?.AppId,
        MobileAppPlatform = li.MobileApp?.Platform,

        // Read-only fields
        UpdateTime = li.UpdateTimeDateTimeOffset,
        ReservationType = li.ReservationType,
        WarningMessages = li.WarningMessages?.ToList()
    };

    /// <summary>
    /// Maps the flat bid strategy properties from <see cref="Dv360LineItem"/> to the Google SDK's
    /// polymorphic <see cref="GoogleData.BiddingStrategy"/>. Uses <c>FixedBidStrategy</c> when
    /// <see cref="Dv360LineItem.FixedBidAmountMicros"/> is set, otherwise <c>MaximizeSpendBidStrategy</c>
    /// when <see cref="Dv360LineItem.MaximizeSpendPerformanceGoalType"/> is set.
    /// </summary>
    private static GoogleData.BiddingStrategy? MapBidStrategyToGoogle(Dv360LineItem lineItem)
    {
        if (lineItem.FixedBidAmountMicros is not null)
        {
            return new GoogleData.BiddingStrategy
            {
                FixedBid = new GoogleData.FixedBidStrategy
                {
                    BidAmountMicros = lineItem.FixedBidAmountMicros
                }
            };
        }

        if (lineItem.MaximizeSpendPerformanceGoalType is not null)
        {
            return new GoogleData.BiddingStrategy
            {
                MaximizeSpendAutoBid = new GoogleData.MaximizeSpendBidStrategy
                {
                    PerformanceGoalType = lineItem.MaximizeSpendPerformanceGoalType,
                    MaxAverageCpmBidAmountMicros = lineItem.MaxAverageCpmBidAmountMicros
                }
            };
        }

        return null;
    }

    /// <summary>
    /// Maps the flat frequency cap properties from <see cref="Dv360LineItem"/> to the
    /// Google SDK's <see cref="GoogleData.FrequencyCap"/>.
    /// </summary>
    private static GoogleData.FrequencyCap MapFrequencyCapToGoogle(Dv360LineItem lineItem)
    {
        if (lineItem.FrequencyCapUnlimited)
        {
            return new GoogleData.FrequencyCap { Unlimited = true };
        }

        return new GoogleData.FrequencyCap
        {
            Unlimited = false,
            MaxImpressions = lineItem.FrequencyCapMaxImpressions,
            TimeUnit = lineItem.FrequencyCapTimeUnit,
            TimeUnitCount = lineItem.FrequencyCapTimeUnitCount
        };
    }

    /// <summary>
    /// Maps Google SDK conversion counting config to the library's flat model.
    /// Returns <c>null</c> when no conversion counting is configured.
    /// </summary>
    private static Dv360ConversionCountingConfig? MapConversionCountingFromGoogle(GoogleData.ConversionCountingConfig? config)
    {
        if (config is null)
            return null;

        return new Dv360ConversionCountingConfig
        {
            PostViewCountPercentageMillis = config.PostViewCountPercentageMillis,
            FloodlightActivityConfigs = config.FloodlightActivityConfigs?
                .Select(f => new Dv360FloodlightActivityConfig
                {
                    FloodlightActivityId = f.FloodlightActivityId ?? 0,
                    PostClickLookbackWindowDays = f.PostClickLookbackWindowDays ?? 0,
                    PostViewLookbackWindowDays = f.PostViewLookbackWindowDays ?? 0
                })
                .ToList()
        };
    }
}
