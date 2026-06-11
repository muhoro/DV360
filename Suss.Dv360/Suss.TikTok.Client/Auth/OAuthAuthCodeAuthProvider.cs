using Microsoft.Extensions.Options;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Exceptions;

namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Authentication provider that performs the TikTok OAuth 2.0 authorization-code exchange to mint
/// an access token.
/// <para>
/// Workflow:
/// <list type="number">
///   <item><description>A user authorizes the app in TikTok's consent screen.</description></item>
///   <item><description>TikTok redirects back with a one-time <c>auth_code</c>.</description></item>
///   <item><description>This provider POSTs <c>app_id</c> + <c>secret</c> + <c>auth_code</c> to
///   <c>/open_api/v1.3/oauth2/access_token/</c> and receives a long-lived <c>access_token</c>.</description></item>
/// </list>
/// The resolved token is cached for the lifetime of this provider (registered as a singleton) so
/// the exchange happens at most once. The exchange is guarded by a <see cref="SemaphoreSlim"/> to
/// be safe under concurrent first use.
/// </para>
/// </summary>
/// <param name="oauthService">The OAuth service used to exchange the configured auth code.</param>
/// <param name="options">Client options carrying the app id, secret, and auth code.</param>
internal sealed class OAuthAuthCodeAuthProvider(
    ITikTokOAuthService oauthService,
    IOptions<TikTokClientOptions> options) : ITikTokAuthProvider
{
    private readonly TikTokClientOptions _options = options.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _cachedToken;

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: token already exchanged.
        if (_cachedToken is not null)
            return _cachedToken;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Re-check after acquiring the lock.
            if (_cachedToken is not null)
                return _cachedToken;

            // Validate the configuration required for the exchange.
            if (string.IsNullOrWhiteSpace(_options.AppId) ||
                string.IsNullOrWhiteSpace(_options.AppSecret) ||
                string.IsNullOrWhiteSpace(_options.AuthCode))
            {
                throw new TikTokApiException(
                    $"{nameof(AuthMode)} is '{AuthMode.OAuthAuthorizationCode}' but one or more of " +
                    $"'{nameof(TikTokClientOptions.AppId)}', '{nameof(TikTokClientOptions.AppSecret)}', " +
                    $"'{nameof(TikTokClientOptions.AuthCode)}' was not configured.");
            }

            var token = await oauthService.ExchangeAuthorizationCodeAsync(_options.AuthCode, cancellationToken);
            _cachedToken = token.AccessToken;
            return _cachedToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

}
