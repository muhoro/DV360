using System.Collections.Concurrent;
using Google;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using SearchTargetingTypeEnum = Google.Apis.DisplayVideo.v4.TargetingTypesResource.TargetingOptionsResource.SearchRequest.TargetingTypeEnum;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Internal implementation of <see cref="IGeoRegionService"/> that queries the DV360
/// <c>targetingTypes.targetingOptions.search</c> endpoint for geo-region targeting options
/// and caches results in a thread-safe <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// <para>
/// Geo-region data is stable (country/city IDs rarely change), so results are cached for the
/// process lifetime. The cache key is <c>(advertiserId, lowerCaseSearchQuery)</c> to ensure
/// per-advertiser correctness while avoiding redundant API calls.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory that provides an authenticated <c>DisplayVideoService</c> instance.</param>
/// <param name="logger">Logger for structured diagnostic output.</param>
internal sealed class GeoRegionService(
    IDisplayVideoServiceFactory serviceFactory,
    ILogger<GeoRegionService> logger) : IGeoRegionService
{
    // Cache key: (advertiserId, lowerCaseSearchQuery) ? results.
    // Geo-region data rarely changes, so caching for the process lifetime is safe.
    private static readonly ConcurrentDictionary<(long AdvertiserId, string Query), IReadOnlyList<Dv360GeoRegion>> s_cache = new();

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dv360GeoRegion>> SearchAsync(long advertiserId, string searchQuery,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchQuery);

        var cacheKey = (advertiserId, searchQuery.ToLowerInvariant());

        if (s_cache.TryGetValue(cacheKey, out var cached))
        {
            logger.LogDebug("Cache hit for geo-region search '{SearchQuery}' (advertiser {AdvertiserId})",
                searchQuery, advertiserId);
            return cached;
        }

        logger.LogInformation("Searching DV360 geo-region targeting options for '{SearchQuery}' (advertiser {AdvertiserId})",
            searchQuery, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);
            var results = new List<Dv360GeoRegion>();
            string? pageToken = null;

            // Iterate through all pages of results from the search endpoint.
            do
            {
                var body = new Google.Apis.DisplayVideo.v4.Data.SearchTargetingOptionsRequest
                {
                    AdvertiserId = advertiserId,
                    GeoRegionSearchTerms = new Google.Apis.DisplayVideo.v4.Data.GeoRegionSearchTerms
                    {
                        GeoRegionQuery = searchQuery
                    },
                    PageToken = pageToken
                };

                var request = service.TargetingTypes.TargetingOptions
                    .Search(body, SearchTargetingTypeEnum.TARGETINGTYPEGEOREGION);

                var response = await request.ExecuteAsync(cancellationToken);

                if (response.TargetingOptions is { Count: > 0 })
                {
                    foreach (var option in response.TargetingOptions)
                    {
                        results.Add(new Dv360GeoRegion
                        {
                            TargetingOptionId = option.TargetingOptionId,
                            DisplayName = option.GeoRegionDetails?.DisplayName ?? string.Empty,
                            GeoRegionType = option.GeoRegionDetails?.GeoRegionType ?? string.Empty
                        });
                    }
                }

                pageToken = response.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            logger.LogInformation("Found {Count} geo-region targeting options for '{SearchQuery}'",
                results.Count, searchQuery);

            // Cache the results for subsequent lookups.
            s_cache.TryAdd(cacheKey, results);

            return results;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to search geo-region targeting options for '{SearchQuery}'", searchQuery);
            throw new Dv360ApiException(
                $"Failed to search geo-region targeting options for '{searchQuery}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dv360GeoRegion?> FindByNameAsync(long advertiserId, string displayName,
        CancellationToken cancellationToken = default)
    {
        var results = await SearchAsync(advertiserId, displayName, cancellationToken);

        return results.FirstOrDefault(r =>
            r.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
    }
}
