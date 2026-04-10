using Google.Apis.DisplayVideo.v4;

namespace Suss.Dv360.Client.Infrastructure;

/// <summary>
/// Factory abstraction for creating authenticated <see cref="DisplayVideoService"/> instances.
/// <para>
/// Decouples service classes from the details of credential acquisition and SDK initialization.
/// The default implementation (<see cref="DisplayVideoServiceFactory"/>) caches the service
/// instance in a thread-safe manner so the credential is acquired only once.
/// </para>
/// </summary>
internal interface IDisplayVideoServiceFactory
{
    /// <summary>
    /// Returns an authenticated <see cref="DisplayVideoService"/> ready for API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the creation (e.g., during app shutdown).</param>
    /// <returns>A fully initialized and authenticated Google Display &amp; Video 360 service instance.</returns>
    Task<DisplayVideoService> CreateAsync(CancellationToken cancellationToken = default);
}
