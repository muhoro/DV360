using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides lookup and search for DV360 geographic region targeting options.
/// <para>
/// Wraps the DV360 <c>targetingTypes.targetingOptions.search</c> API to discover geo-region
/// targeting option IDs by display name. This eliminates the need to hard-code numeric IDs
/// like <c>"2840"</c> — instead, consumers can resolve them by human-readable names such as
/// <c>"United States"</c>, <c>"London"</c>, or <c>"Kenya"</c>.
/// </para>
/// <para>
/// Results are cached in memory to avoid redundant API calls across repeated lookups.
/// </para>
/// </summary>
public interface IGeoRegionService
{
    /// <summary>
    /// Searches for geo-region targeting options whose display name matches the specified query.
    /// <para>
    /// The DV360 API performs a partial/fuzzy match, so searching for <c>"London"</c> may return
    /// London (United Kingdom), London (Canada), London (Kentucky), etc. Use the
    /// <see cref="Dv360GeoRegion.GeoRegionType"/> property to filter results by granularity
    /// (country, city, region, etc.).
    /// </para>
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID (required by the API for partner-scoped results).</param>
    /// <param name="searchQuery">
    /// A partial or full name to search for (e.g., <c>"United States"</c>, <c>"London"</c>, <c>"Kenya"</c>).
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of matching geo-region targeting options.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<IReadOnlyList<Dv360GeoRegion>> SearchAsync(long advertiserId, string searchQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the targeting option for an exact geo-region display name.
    /// Returns <c>null</c> if no exact match is found.
    /// <para>
    /// This is a convenience wrapper over <see cref="SearchAsync"/> that performs a
    /// case-insensitive exact match on the <see cref="Dv360GeoRegion.DisplayName"/> property.
    /// </para>
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID.</param>
    /// <param name="displayName">
    /// The exact display name of the geo-region (e.g., <c>"United States"</c>, <c>"Nairobi"</c>).
    /// Comparison is case-insensitive.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The matching geo-region, or <c>null</c> if not found.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<Dv360GeoRegion?> FindByNameAsync(long advertiserId, string displayName,
        CancellationToken cancellationToken = default);
}
