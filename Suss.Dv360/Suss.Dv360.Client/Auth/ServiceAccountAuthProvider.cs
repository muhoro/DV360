using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Suss.Dv360.Client.Configuration;

namespace Suss.Dv360.Client.Auth;

/// <summary>
/// Authenticates against the DV360 API using a Google service account JSON key file.
/// <para>
/// This strategy is ideal for unattended server-to-server scenarios (e.g., backend services,
/// scheduled jobs) where no interactive user consent is required. The credential is scoped
/// to the <c>display-video</c> OAuth 2.0 scope.
/// </para>
/// </summary>
/// <param name="options">Injected client configuration containing the service account key path.</param>
/// <param name="logger">Logger for diagnostic output during credential acquisition.</param>
internal sealed class ServiceAccountAuthProvider(
    IOptions<Dv360ClientOptions> options,
    ILogger<ServiceAccountAuthProvider> logger) : IDv360AuthProvider
{
    /// <summary>
    /// The OAuth 2.0 scope required to access the Display &amp; Video 360 API.
    /// </summary>
    private const string DisplayVideoScope = "https://www.googleapis.com/auth/display-video";

    private readonly Dv360ClientOptions _options = options.Value;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Dv360ClientOptions.ServiceAccountKeyPath"/> is not configured.
    /// </exception>
    public async Task<IConfigurableHttpClientInitializer> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Authenticating with service account key from {KeyPath}", _options.ServiceAccountKeyPath);

        if (string.IsNullOrWhiteSpace(_options.ServiceAccountKeyPath))
            throw new InvalidOperationException("ServiceAccountKeyPath must be configured for service account authentication.");

        // Read the JSON key file from disk and create a scoped credential.
        // GoogleCredential handles token refresh automatically.
        await using var stream = new FileStream(_options.ServiceAccountKeyPath, FileMode.Open, FileAccess.Read);
        var credential = await GoogleCredential.FromStreamAsync(stream, cancellationToken);

        return credential.CreateScoped(DisplayVideoScope);
    }
}
