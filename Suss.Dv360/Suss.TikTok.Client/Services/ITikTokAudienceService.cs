using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Provides custom-audience operations: creating customer-match audiences from emails, phone numbers,
/// or mobile advertiser ids, and referencing them by id for ad group targeting.
/// </summary>
public interface ITikTokAudienceService
{
    /// <summary>
    /// Creates a custom audience from a customer-match file and returns its server-assigned id.
    /// <para>
    /// Identifier values are normalized and SHA-256 hashed locally (unless already pre-hashed) before
    /// upload, so raw PII never leaves the process unhashed.
    /// </para>
    /// </summary>
    /// <param name="advertiserId">The TikTok advertiser id that will own the audience.</param>
    /// <param name="audience">
    /// The audience to create. Requires a populated <see cref="TikTokAudience.File"/>. On return,
    /// <see cref="TikTokAudience.AudienceId"/> is populated.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="audience"/> with its server-assigned id populated.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when the TikTok API returns an error.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="TikTokAudience.File"/> is missing.</exception>
    Task<TikTokAudience> CreateFromFileAsync(string advertiserId, TikTokAudience audience, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a custom audience using the supplied TikTok execution context.
    /// </summary>
    Task<TikTokAudience> CreateFromFileAsync(
        TikTokExecutionContext executionContext,
        TikTokAudience audience,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the existing custom audiences for an advertiser, allowing callers to reference an
    /// audience by id without re-uploading identifiers.
    /// </summary>
    /// <param name="advertiserId">The TikTok advertiser id whose audiences are listed.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The advertiser's audiences with ids and names populated.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when the TikTok API returns an error.</exception>
    Task<IReadOnlyList<TikTokAudience>> ListAsync(string advertiserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists custom audiences using the supplied TikTok execution context.
    /// </summary>
    Task<IReadOnlyList<TikTokAudience>> ListAsync(
        TikTokExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}
