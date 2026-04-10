using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Suss.Dv360.Client.Configuration;

namespace Suss.Dv360.Client.Auth;

/// <summary>
/// Authenticates against the DV360 API using the interactive OAuth 2.0 user consent flow.
/// <para>
/// This strategy is best suited for developer tools, desktop utilities, or scenarios where
/// a human user must grant consent. Refresh tokens are persisted to a
/// <see cref="FileDataStore"/> so that subsequent runs reuse the existing authorization
/// without re-prompting the user.
/// </para>
/// </summary>
/// <param name="options">Injected client configuration containing OAuth client ID, secret, and token store path.</param>
/// <param name="logger">Logger for diagnostic output during the OAuth flow.</param>
internal sealed class OAuthUserAuthProvider(
    IOptions<Dv360ClientOptions> options,
    ILogger<OAuthUserAuthProvider> logger) : IDv360AuthProvider
{
    /// <summary>
    /// The OAuth 2.0 scope required to access the Display &amp; Video 360 API.
    /// </summary>
    private const string DisplayVideoScope = "https://www.googleapis.com/auth/display-video";

    private readonly Dv360ClientOptions _options = options.Value;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Dv360ClientOptions.OAuthClientId"/> or
    /// <see cref="Dv360ClientOptions.OAuthClientSecret"/> is not configured.
    /// </exception>
    public async Task<IConfigurableHttpClientInitializer> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Authenticating with OAuth 2.0 user credentials");

        if (string.IsNullOrWhiteSpace(_options.OAuthClientId))
            throw new InvalidOperationException("OAuthClientId must be configured for OAuth user authentication.");

        if (string.IsNullOrWhiteSpace(_options.OAuthClientSecret))
            throw new InvalidOperationException("OAuthClientSecret must be configured for OAuth user authentication.");

        // Launch the OAuth 2.0 authorization code flow.
        // On first run this opens a browser for user consent; subsequent runs reuse the
        // refresh token stored in the FileDataStore directory.
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = _options.OAuthClientId,
                ClientSecret = _options.OAuthClientSecret
            },
            [DisplayVideoScope],
            "user",
            cancellationToken,
            new FileDataStore(_options.TokenStorePath ?? "tokens"));

        return credential;
    }
}
