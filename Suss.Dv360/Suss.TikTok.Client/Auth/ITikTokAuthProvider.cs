namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Abstracts the acquisition of a TikTok Marketing API access token.
/// <para>
/// This is the primary extensibility point for authentication. The library ships with two
/// built-in implementations:
/// <list type="bullet">
///   <item><description><see cref="StaticTokenAuthProvider"/> – returns a pre-issued long-lived token from configuration.</description></item>
///   <item><description><see cref="OAuthAuthCodeAuthProvider"/> – exchanges an OAuth authorization code for an access token.</description></item>
/// </list>
/// Implement this interface to support additional strategies (e.g., refreshing tokens from a
/// secrets vault, or rotating tokens fetched from a custom token service).
/// </para>
/// <para>
/// Unlike SDK-based clients, TikTok auth resolves to a simple bearer string that is attached to
/// every request via the <c>Access-Token</c> header. The token is cached by callers, so this
/// method may perform a network call (token exchange) on first use.
/// </para>
/// </summary>
public interface ITikTokAuthProvider
{
    /// <summary>
    /// Obtains a valid TikTok <c>access_token</c> to attach to outgoing API requests.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the acquisition (e.g., during shutdown).</param>
    /// <returns>The access token string placed in the <c>Access-Token</c> header.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when a token cannot be acquired.</exception>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
