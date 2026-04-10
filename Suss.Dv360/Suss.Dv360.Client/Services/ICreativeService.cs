using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides CRUD operations for Display &amp; Video 360 creatives.
/// <para>
/// Supports hosted display creatives and third-party tag creatives.
/// Dimension properties are flattened from the Google SDK’s nested <c>Dimensions</c> object.
/// </para>
/// </summary>
public interface ICreativeService
{
    /// <summary>
    /// Creates a new creative under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the creative.</param>
    /// <param name="creative">The creative definition. <see cref="Dv360Creative.CreativeId"/> is populated on return.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="creative"/> instance with <see cref="Dv360Creative.CreativeId"/> set.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<Dv360Creative> CreateAsync(long advertiserId, Dv360Creative creative, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single creative by its identifier.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the creative.</param>
    /// <param name="creativeId">The creative identifier to retrieve.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The creative if found; otherwise <c>null</c>.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns a non-404 error.</exception>
    Task<Dv360Creative?> GetAsync(long advertiserId, long creativeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all creatives under the specified advertiser, handling pagination automatically.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID whose creatives to list.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only collection of all creatives for the advertiser.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<IReadOnlyList<Dv360Creative>> ListAsync(long advertiserId, CancellationToken cancellationToken = default);
}
