using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Provides campaign-level operations (the top of the TikTok delivery hierarchy).
/// </summary>
public interface ITikTokCampaignService
{
    /// <summary>
    /// Creates a campaign under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The TikTok advertiser id that will own the campaign.</param>
    /// <param name="campaign">
    /// The campaign to create. On return, <see cref="TikTokCampaign.CampaignId"/> is populated.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="campaign"/> with its server-assigned id populated.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when the TikTok API returns an error.</exception>
    Task<TikTokCampaign> CreateAsync(string advertiserId, TikTokCampaign campaign, CancellationToken cancellationToken = default);
}
