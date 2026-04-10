using Google;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using GoogleData = Google.Apis.DisplayVideo.v4.Data;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Internal implementation of <see cref="ICampaignService"/> that manages DV360 campaigns
/// via the Google Display &amp; Video 360 SDK.
/// <para>
/// Handles the mapping between the library’s flat <see cref="Dv360Campaign"/> model and
/// the Google SDK’s nested <c>Campaign</c> structure (which includes <c>CampaignGoal</c>
/// and <c>CampaignFlight</c> sub-objects). All <see cref="GoogleApiException"/> errors
/// are caught and re-thrown as <see cref="Dv360ApiException"/>.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory that provides an authenticated <c>DisplayVideoService</c> instance.</param>
/// <param name="logger">Logger for structured diagnostic output.</param>
internal sealed class CampaignService(
    IDisplayVideoServiceFactory serviceFactory,
    ILogger<CampaignService> logger) : ICampaignService
{
    /// <inheritdoc />
    public async Task<Dv360Campaign> CreateAsync(long advertiserId, Dv360Campaign campaign, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating campaign '{DisplayName}' for advertiser {AdvertiserId}", campaign.DisplayName, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);

            // Map the flat Dv360Campaign to the Google SDK's nested Campaign structure.
            var body = new GoogleData.Campaign
            {
                DisplayName = campaign.DisplayName,
                EntityStatus = campaign.EntityStatus,
                CampaignGoal = new GoogleData.CampaignGoal
                {
                    CampaignGoalType = campaign.GoalType,
                    // Only create a PerformanceGoal sub-object when a goal type is specified.
                    PerformanceGoal = campaign.PerformanceGoalType is not null
                        ? new GoogleData.PerformanceGoal
                        {
                            PerformanceGoalType = campaign.PerformanceGoalType,
                            PerformanceGoalAmountMicros = campaign.PerformanceGoalAmountMicros
                        }
                        : null
                },
                CampaignFlight = new GoogleData.CampaignFlight
                {
                    PlannedDates = new GoogleData.DateRange
                    {
                        StartDate = GoogleTypeMapper.ToGoogleDate(campaign.StartDate),
                        EndDate = GoogleTypeMapper.ToGoogleDate(campaign.EndDate)
                    }
                },
                FrequencyCap = new GoogleData.FrequencyCap
                {
                    Unlimited = true
                },
                // DV360 API v4 requires at least one CampaignBudget with a valid date range.
                CampaignBudgets =
                [
                    new GoogleData.CampaignBudget
                    {
                        BudgetAmountMicros = campaign.BudgetAmountMicros,
                        BudgetUnit = campaign.BudgetUnit,
                        ExternalBudgetSource = "EXTERNAL_BUDGET_SOURCE_NONE",
                        DateRange = new GoogleData.DateRange
                        {
                            StartDate = GoogleTypeMapper.ToGoogleDate(campaign.StartDate),
                            EndDate = GoogleTypeMapper.ToGoogleDate(campaign.EndDate)
                        }
                    }
                ]
            };

            var request = service.Advertisers.Campaigns.Create(body, advertiserId);
            var result = await request.ExecuteAsync(cancellationToken);

            // Populate the server-assigned campaign ID back onto the caller's model.
            campaign.CampaignId = result.CampaignId;
            logger.LogInformation("Created campaign {CampaignId} for advertiser {AdvertiserId}", result.CampaignId, advertiserId);

            return campaign;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to create campaign for advertiser {AdvertiserId}", advertiserId);
            throw new Dv360ApiException($"Failed to create campaign for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dv360Campaign?> GetAsync(long advertiserId, long campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var request = service.Advertisers.Campaigns.Get(advertiserId, campaignId);
            var result = await request.ExecuteAsync(cancellationToken);

            return MapFromGoogle(result);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Return null instead of throwing when the campaign does not exist.
            return null;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to get campaign {campaignId} for advertiser {advertiserId}.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dv360Campaign>> ListAsync(long advertiserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var campaigns = new List<Dv360Campaign>();
            string? pageToken = null;

            // Iterate through all pages of results until no NextPageToken is returned.
            do
            {
                var request = service.Advertisers.Campaigns.List(advertiserId);
                request.PageToken = pageToken;

                var result = await request.ExecuteAsync(cancellationToken);
                if (result.Campaigns is not null)
                    campaigns.AddRange(result.Campaigns.Select(MapFromGoogle));

                pageToken = result.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return campaigns;
        }
        catch (GoogleApiException ex)
        {
            throw new Dv360ApiException($"Failed to list campaigns for advertiser {advertiserId}.", ex);
        }
    }

    /// <summary>
    /// Maps a Google SDK <see cref="GoogleData.Campaign"/> to the library’s flat
    /// <see cref="Dv360Campaign"/> model by extracting nested goal and flight data.
    /// </summary>
    private static Dv360Campaign MapFromGoogle(GoogleData.Campaign c)
    {
        // Use the first budget for the flat model.
        var budget = c.CampaignBudgets?.FirstOrDefault();

        return new()
        {
            CampaignId = c.CampaignId,
            DisplayName = c.DisplayName ?? string.Empty,
            EntityStatus = c.EntityStatus ?? "ENTITY_STATUS_UNSPECIFIED",
            GoalType = c.CampaignGoal?.CampaignGoalType ?? string.Empty,
            PerformanceGoalType = c.CampaignGoal?.PerformanceGoal?.PerformanceGoalType,
            PerformanceGoalAmountMicros = c.CampaignGoal?.PerformanceGoal?.PerformanceGoalAmountMicros,
            BudgetUnit = budget?.BudgetUnit ?? "BUDGET_UNIT_CURRENCY",
            BudgetAmountMicros = budget?.BudgetAmountMicros ?? 0,
            StartDate = GoogleTypeMapper.FromGoogleDate(c.CampaignFlight?.PlannedDates?.StartDate),
            EndDate = GoogleTypeMapper.FromGoogleDate(c.CampaignFlight?.PlannedDates?.EndDate)
        };
    }
}
