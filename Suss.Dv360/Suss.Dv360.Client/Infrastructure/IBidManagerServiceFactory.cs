using Google.Apis.DoubleClickBidManager.v2;

namespace Suss.Dv360.Client.Infrastructure;

/// <summary>
/// Factory interface for creating authenticated <see cref="DoubleClickBidManagerService"/> instances.
/// <para>
/// Follows the same pattern as <see cref="IDisplayVideoServiceFactory"/> to ensure
/// consistent authentication and caching behavior across DV360 APIs.
/// </para>
/// </summary>
public interface IBidManagerServiceFactory
{
    /// <summary>
    /// Creates or returns a cached authenticated <see cref="DoubleClickBidManagerService"/> instance.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An authenticated service instance ready for API calls.</returns>
    Task<DoubleClickBidManagerService> CreateAsync(CancellationToken cancellationToken = default);
}
