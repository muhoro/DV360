using Microsoft.Extensions.Options;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Exceptions;

namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Authentication provider that returns a pre-issued, long-lived TikTok access token supplied
/// directly through <see cref="TikTokClientOptions.AccessToken"/>.
/// <para>
/// This is the simplest mode and is appropriate for unattended workloads where a token has already
/// been minted (TikTok access tokens are long-lived and do not auto-expire like Google's). No
/// network call is performed.
/// </para>
/// </summary>
/// <param name="options">Client options carrying the configured access token.</param>
internal sealed class StaticTokenAuthProvider(IOptions<TikTokClientOptions> options) : ITikTokAuthProvider
{
    private readonly TikTokClientOptions _options = options.Value;

    /// <inheritdoc />
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Validate eagerly so misconfiguration produces a clear, actionable error rather than a
        // confusing 401 from the API later.
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new TikTokApiException(
                $"{nameof(AuthMode)} is '{AuthMode.AccessToken}' but no '{nameof(TikTokClientOptions.AccessToken)}' " +
                "was configured. Provide a long-lived access token or switch to OAuth.");
        }

        return Task.FromResult(_options.AccessToken);
    }
}
