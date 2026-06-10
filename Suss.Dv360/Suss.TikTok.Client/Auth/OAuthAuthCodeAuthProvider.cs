using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
/// <param name="httpClient">An <see cref="HttpClient"/> used to call the token endpoint.</param>
/// <param name="options">Client options carrying the app id, secret, and auth code.</param>
internal sealed class OAuthAuthCodeAuthProvider(
    HttpClient httpClient,
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

            // The token endpoint is unauthenticated (no Access-Token header) and takes the app
            // credentials in the JSON body.
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/open_api/{_options.ApiVersion}/oauth2/access_token/";
            var payload = new
            {
                app_id = _options.AppId,
                secret = _options.AppSecret,
                auth_code = _options.AuthCode,
                grant_type = "authorization_code"
            };

            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
            }
            catch (Exception ex) when (ex is not TikTokApiException)
            {
                throw new TikTokApiException("Failed to reach the TikTok OAuth token endpoint.", ex);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // TikTok returns the access token inside the standard envelope's "data" object.
            TokenEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<TokenEnvelope>(body);
            }
            catch (JsonException ex)
            {
                throw new TikTokApiException("Could not parse the TikTok OAuth token response.", ex);
            }

            if (envelope is null || envelope.Code != 0 || envelope.Data?.AccessToken is null)
            {
                throw new TikTokApiException(
                    $"TikTok OAuth token exchange failed: {envelope?.Message ?? "unknown error"}.",
                    envelope?.Code, envelope?.RequestId, body);
            }

            _cachedToken = envelope.Data.AccessToken;
            return _cachedToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>Minimal envelope used only to parse the token-exchange response.</summary>
    private sealed class TokenEnvelope
    {
        [JsonPropertyName("code")] public long Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("request_id")] public string? RequestId { get; set; }
        [JsonPropertyName("data")] public TokenData? Data { get; set; }
    }

    /// <summary>The <c>data</c> payload of the token-exchange response.</summary>
    private sealed class TokenData
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    }
}
