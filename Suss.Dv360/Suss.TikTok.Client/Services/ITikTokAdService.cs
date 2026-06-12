using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Provides ad-level operations (the bottom tier binding creative content to an ad group).
/// </summary>
public interface ITikTokAdService
{
    /// <summary>
    /// Creates one or more ads under the specified advertiser and parent ad group.
    /// <para>
    /// TikTok's <c>ad/create/</c> endpoint accepts a batch of creatives in a single call, so this
    /// method creates all <paramref name="ads"/> together and maps the returned ids back onto each.
    /// </para>
    /// </summary>
    /// <param name="advertiserId">The TikTok advertiser id that will own the ads.</param>
    /// <param name="adGroupId">The parent ad group id all ads are created under.</param>
    /// <param name="ads">
    /// The ads to create. Each must carry the asset references required by its
    /// <see cref="TikTokAd.Format"/>. On return, each ad's <see cref="TikTokAd.AdId"/> is populated.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="ads"/> list with server-assigned ids populated.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when the TikTok API returns an error.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a required asset reference is missing for a format.</exception>
    Task<IReadOnlyList<TikTokAd>> CreateAsync(
        string advertiserId,
        string adGroupId,
        IReadOnlyList<TikTokAd> ads,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates one or more ads using the supplied TikTok execution context.
    /// </summary>
    Task<IReadOnlyList<TikTokAd>> CreateAsync(
        TikTokExecutionContext executionContext,
        string adGroupId,
        IReadOnlyList<TikTokAd> ads,
        CancellationToken cancellationToken = default);
}
