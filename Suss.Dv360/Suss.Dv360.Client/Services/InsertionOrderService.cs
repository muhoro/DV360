using Google;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using GoogleData = Google.Apis.DisplayVideo.v4.Data;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Internal implementation of <see cref="IInsertionOrderService"/> that manages DV360
/// insertion orders via the Google Display &amp; Video 360 SDK.
/// <para>
/// Maps between the library’s flat <see cref="Dv360InsertionOrder"/> model and the
/// Google SDK’s nested structure, which stores budget across multiple
/// <c>InsertionOrderBudgetSegment</c> entries. This implementation uses a single budget
/// segment to keep the Phase 1 model simple.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory that provides an authenticated <c>DisplayVideoService</c> instance.</param>
/// <param name="logger">Logger for structured diagnostic output.</param>
internal sealed class InsertionOrderService(
    IDisplayVideoServiceFactory serviceFactory,
    ILogger<InsertionOrderService> logger) : IInsertionOrderService
{
    /// <inheritdoc />
    public async Task<Dv360InsertionOrder> CreateAsync(long advertiserId, Dv360InsertionOrder insertionOrder, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating insertion order '{DisplayName}' for advertiser {AdvertiserId}", insertionOrder.DisplayName, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);

            // Map the flat Dv360InsertionOrder to the Google SDK's nested InsertionOrder.
            // A single budget segment is created covering the full flight window.
            var body = new GoogleData.InsertionOrder
            {
                CampaignId = insertionOrder.CampaignId,
                DisplayName = insertionOrder.DisplayName,
                EntityStatus = insertionOrder.EntityStatus,
                Budget = new GoogleData.InsertionOrderBudget
                {
                    BudgetUnit = insertionOrder.BudgetUnit,
                    BudgetSegments =
                    [
                        new GoogleData.InsertionOrderBudgetSegment
                        {
                            BudgetAmountMicros = insertionOrder.BudgetAmountMicros,
                            DateRange = new GoogleData.DateRange
                            {
                                StartDate = GoogleTypeMapper.ToGoogleDate(insertionOrder.StartDate),
                                EndDate = GoogleTypeMapper.ToGoogleDate(insertionOrder.EndDate)
                            }
                        }
                    ]
                },
                Pacing = new GoogleData.Pacing
                {
                    PacingPeriod = insertionOrder.PacingPeriod,
                    PacingType = insertionOrder.PacingType,
                    DailyMaxMicros = insertionOrder.PacingType == "PACING_TYPE_AHEAD"
                        ? insertionOrder.DailyMaxMicros
                        : null
                },
                // DV360 API v4 requires a KPI on every insertion order.
                Kpi = new GoogleData.Kpi
                {
                    KpiType = insertionOrder.KpiType,
                    KpiAmountMicros = insertionOrder.KpiAmountMicros
                }
            };

            var request = service.Advertisers.InsertionOrders.Create(body, advertiserId);
            var result = await request.ExecuteAsync(cancellationToken);

            // Populate the server-assigned IO ID back onto the caller's model.
            insertionOrder.InsertionOrderId = result.InsertionOrderId;
            logger.LogInformation("Created insertion order {InsertionOrderId} for advertiser {AdvertiserId}", result.InsertionOrderId, advertiserId);

            return insertionOrder;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to create insertion order for advertiser {AdvertiserId}", advertiserId);
            throw new Dv360ApiException($"Failed to create insertion order for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dv360InsertionOrder?> GetAsync(long advertiserId, long insertionOrderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var request = service.Advertisers.InsertionOrders.Get(advertiserId, insertionOrderId);
            var result = await request.ExecuteAsync(cancellationToken);

            return MapFromGoogle(result);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Return null instead of throwing when the insertion order does not exist.
            return null;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to get insertion order {insertionOrderId} for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dv360InsertionOrder>> ListAsync(long advertiserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var orders = new List<Dv360InsertionOrder>();
            string? pageToken = null;

            // Iterate through all pages of results until no NextPageToken is returned.
            do
            {
                var request = service.Advertisers.InsertionOrders.List(advertiserId);
                request.PageToken = pageToken;

                var result = await request.ExecuteAsync(cancellationToken);
                if (result.InsertionOrders is not null)
                    orders.AddRange(result.InsertionOrders.Select(MapFromGoogle));

                pageToken = result.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return orders;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to list insertion orders for advertiser {advertiserId}.", ex);
        }
    }

    /// <summary>
    /// Maps a Google SDK <see cref="GoogleData.InsertionOrder"/> to the library’s flat
    /// <see cref="Dv360InsertionOrder"/> model by extracting the first budget segment
    /// and the pacing configuration.
    /// </summary>
    private static Dv360InsertionOrder MapFromGoogle(GoogleData.InsertionOrder io)
    {
        // Use the first budget segment for the flat model; multi-segment IOs are not
        // supported in Phase 1.
        var segment = io.Budget?.BudgetSegments?.FirstOrDefault();

        return new Dv360InsertionOrder
        {
            InsertionOrderId = io.InsertionOrderId,
            CampaignId = io.CampaignId ?? 0,
            DisplayName = io.DisplayName ?? string.Empty,
            EntityStatus = io.EntityStatus ?? "ENTITY_STATUS_UNSPECIFIED",
            BudgetUnit = io.Budget?.BudgetUnit ?? "BUDGET_UNIT_UNSPECIFIED",
            BudgetAmountMicros = segment?.BudgetAmountMicros ?? 0,
            StartDate = GoogleTypeMapper.FromGoogleDate(segment?.DateRange?.StartDate),
            EndDate = GoogleTypeMapper.FromGoogleDate(segment?.DateRange?.EndDate),
            PacingPeriod = io.Pacing?.PacingPeriod ?? "PACING_PERIOD_UNSPECIFIED",
            PacingType = io.Pacing?.PacingType ?? "PACING_TYPE_UNSPECIFIED",
            DailyMaxMicros = io.Pacing?.DailyMaxMicros ?? 0,
            KpiType = io.Kpi?.KpiType ?? "KPI_TYPE_UNSPECIFIED",
            KpiAmountMicros = io.Kpi?.KpiAmountMicros
        };
    }
}
