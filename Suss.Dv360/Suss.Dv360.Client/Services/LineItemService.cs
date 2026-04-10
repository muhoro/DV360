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
/// Maps between the library’s flat <see cref="Dv360LineItem"/> model and the Google SDK’s
/// nested structure, which stores budget in <c>LineItemBudget</c>, flight dates in
/// <c>LineItemFlight</c>, and pacing in <c>Pacing</c>. Custom flight dates are used
/// with <c>LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM</c>.
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

            // Map the flat Dv360LineItem to the Google SDK's nested LineItem structure.
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
                    // Use custom flight dates so start/end are explicitly controlled.
                    FlightDateType = "LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM",
                    DateRange = new GoogleData.DateRange
                    {
                        StartDate = GoogleTypeMapper.ToGoogleDate(lineItem.StartDate),
                        EndDate = GoogleTypeMapper.ToGoogleDate(lineItem.EndDate)
                    }
                },
                Pacing = new GoogleData.Pacing
                {
                    PacingPeriod = lineItem.PacingPeriod,
                    PacingType = lineItem.PacingType,
                    DailyMaxMicros = lineItem.PacingType == "PACING_TYPE_AHEAD"
                        ? lineItem.DailyMaxMicros
                        : null
                },
                // DV360 API v4 requires a bid strategy on every line item.
                BidStrategy = MapBidStrategyToGoogle(lineItem),
                FrequencyCap = new GoogleData.FrequencyCap
                {
                    Unlimited = true,
                    //TimeUnit = "TIME_UNIT_LIFETIME"
                },
                PartnerRevenueModel = new GoogleData.PartnerRevenueModel
                {
                    MarkupType = "PARTNER_REVENUE_MODEL_MARKUP_TYPE_TOTAL_MEDIA_COST_MARKUP",
                    MarkupAmount = 0
                }
            };

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
    public async Task<IReadOnlyList<Dv360LineItem>> ListAsync(long advertiserId, CancellationToken cancellationToken = default)
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
    /// Maps a Google SDK <see cref="GoogleData.LineItem"/> to the library’s flat
    /// <see cref="Dv360LineItem"/> model by extracting nested budget, flight, and pacing data.
    /// </summary>
    private static Dv360LineItem MapFromGoogle(GoogleData.LineItem li) => new()
    {
        LineItemId = li.LineItemId,
        CampaignId = li.CampaignId ?? 0,
        InsertionOrderId = li.InsertionOrderId ?? 0,
        DisplayName = li.DisplayName ?? string.Empty,
        LineItemType = li.LineItemType ?? string.Empty,
        EntityStatus = li.EntityStatus ?? "ENTITY_STATUS_UNSPECIFIED",
        BudgetAllocationType = li.Budget?.BudgetAllocationType ?? string.Empty,
        BudgetUnit = li.Budget?.BudgetUnit ?? string.Empty,
        MaxBudgetAmountMicros = li.Budget?.MaxAmount ?? 0,
        StartDate = GoogleTypeMapper.FromGoogleDate(li.Flight?.DateRange?.StartDate),
        EndDate = GoogleTypeMapper.FromGoogleDate(li.Flight?.DateRange?.EndDate),
        PacingPeriod = li.Pacing?.PacingPeriod ?? string.Empty,
        PacingType = li.Pacing?.PacingType ?? string.Empty,
        DailyMaxMicros = li.Pacing?.DailyMaxMicros ?? 0,
        FixedBidAmountMicros = li.BidStrategy?.FixedBid?.BidAmountMicros,
        MaximizeSpendPerformanceGoalType = li.BidStrategy?.MaximizeSpendAutoBid?.PerformanceGoalType,
        MaxAverageCpmBidAmountMicros = li.BidStrategy?.MaximizeSpendAutoBid?.MaxAverageCpmBidAmountMicros
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
}
