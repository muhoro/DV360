using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides CRUD operations for Display &amp; Video 360 campaigns.
/// <para>
/// Consumers interact with this interface via <see cref="Dv360Campaign"/> models that
/// hide the Google SDK’s nested <c>CampaignGoal</c> and <c>CampaignFlight</c> structures.
/// </para>
/// </summary>
public interface ICampaignService
{
    /// <summary>
    /// Creates a new campaign under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the campaign.</param>
    /// <param name="campaign">The campaign definition. <see cref="Dv360Campaign.CampaignId"/> is populated on return.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="campaign"/> instance with <see cref="Dv360Campaign.CampaignId"/> set.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<Dv360Campaign> CreateAsync(long advertiserId, Dv360Campaign campaign, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single campaign by its identifier.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the campaign.</param>
    /// <param name="campaignId">The campaign identifier to retrieve.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The campaign if found; otherwise <c>null</c>.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns a non-404 error.</exception>
    Task<Dv360Campaign?> GetAsync(long advertiserId, long campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all campaigns under the specified advertiser, handling pagination automatically.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID whose campaigns to list.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only collection of all campaigns for the advertiser.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<IReadOnlyList<Dv360Campaign>> ListAsync(long advertiserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists campaigns under the specified advertiser with server-side filtering, sorting,
    /// and page size control. Handles pagination automatically.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID whose campaigns to list.</param>
    /// <param name="options">
    /// Options controlling filtering, sorting, and page size. Pass <c>null</c> for API defaults.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only collection of matching campaigns for the advertiser.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<IReadOnlyList<Dv360Campaign>> ListAsync(long advertiserId, CampaignListOptions? options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing campaign under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the campaign.</param>
    /// <param name="campaignId">The campaign identifier to update.</param>
    /// <param name="campaign">The updated campaign definition. <see cref="Dv360Campaign.CampaignId"/> is populated on return.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="campaign"/> instance with <see cref="Dv360Campaign.CampaignId"/> set.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<Dv360Campaign> PatchAsync(long advertiserId, long campaignId, Dv360Campaign campaign, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a campaign.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the campaign.</param>
    /// <param name="campaignId">The campaign identifier to delete.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task DeleteAsync(long advertiserId, long campaignId, CancellationToken cancellationToken = default);
}
