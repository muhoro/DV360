using Suss.TikTok.Client.Models;
using Suss.TikTok.Client.Auth;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Provides ad-group-level operations (the middle tier carrying targeting, budget, and bidding).
/// </summary>
public interface ITikTokAdGroupService
{
    /// <summary>
    /// Creates an ad group under the specified advertiser and parent campaign.
    /// </summary>
    /// <param name="advertiserId">The TikTok advertiser id that will own the ad group.</param>
    /// <param name="adGroup">
    /// The ad group to create. Must have <see cref="TikTokAdGroup.CampaignId"/> set. On return,
    /// <see cref="TikTokAdGroup.AdGroupId"/> is populated.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="adGroup"/> with its server-assigned id populated.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when the TikTok API returns an error.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="TikTokAdGroup.CampaignId"/> is not set.</exception>
    Task<TikTokAdGroup> CreateAsync(string advertiserId, TikTokAdGroup adGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an ad group using the supplied TikTok execution context.
    /// </summary>
    Task<TikTokAdGroup> CreateAsync(
        TikTokExecutionContext executionContext,
        TikTokAdGroup adGroup,
        CancellationToken cancellationToken = default);
}
