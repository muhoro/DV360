using Google.Apis.DisplayVideo.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using Suss.Dv360.Client.Auth;
using Suss.Dv360.Client.Configuration;

namespace Suss.Dv360.Client.Infrastructure;

/// <summary>
/// Thread-safe factory that creates and caches an authenticated <see cref="DisplayVideoService"/>.
/// <para>
/// Uses a <see cref="SemaphoreSlim"/>-based double-checked locking pattern to ensure that
/// the expensive credential acquisition and service initialization happens exactly once,
/// even under concurrent access from multiple threads or scoped service instances.
/// Registered as a singleton in the DI container.
/// </para>
/// </summary>
/// <param name="authProvider">The authentication provider used to obtain API credentials.</param>
/// <param name="options">Client options containing the application name for API requests.</param>
internal sealed class DisplayVideoServiceFactory(
    IDv360AuthProvider authProvider,
    IOptions<Dv360ClientOptions> options) : IDisplayVideoServiceFactory
{
    private readonly Dv360ClientOptions _options = options.Value;

    /// <summary>Synchronization primitive to ensure single-threaded initialization.</summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Cached service instance; <c>null</c> until the first successful creation.</summary>
    private DisplayVideoService? _cachedService;

    /// <inheritdoc />
    public async Task<DisplayVideoService> CreateAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: return the cached instance without acquiring the semaphore.
        if (_cachedService is not null)
            return _cachedService;

        // Slow path: acquire the semaphore and perform double-checked initialization.
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Re-check after acquiring the lock in case another thread completed initialization.
            if (_cachedService is not null)
                return _cachedService;

            // Obtain the credential from the configured auth provider.
            var credential = await authProvider.GetCredentialAsync(cancellationToken);

            // Initialize the Google SDK service with the credential and application name.
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _options.ApplicationName
            };

            _cachedService = new DisplayVideoService(initializer);
            return _cachedService;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
