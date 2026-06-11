using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Exceptions;

namespace Suss.TikTok.Client.Auth;

/// <inheritdoc />
internal sealed class TikTokOAuthService(
    HttpClient httpClient,
    IOptions<TikTokClientOptions> options) : ITikTokOAuthService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TikTokClientOptions _options = options.Value;

    /// <inheritdoc />
    public string BuildAuthorizationUrl(
        string redirectUri,
        string state,
        IEnumerable<string>? scopes = null,
        IReadOnlyDictionary<string, string?>? additionalParameters = null)
        => BuildAuthorizationUrl(
            TikTokAuthScenario.ClientSocialIdentity,
            redirectUri,
            state,
            scopes,
            additionalParameters);

    /// <inheritdoc />
    public string BuildAuthorizationUrl(
        TikTokAuthScenario scenario,
        string redirectUri,
        string state,
        IEnumerable<string>? scopes = null,
        IReadOnlyDictionary<string, string?>? additionalParameters = null)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
            throw new TikTokApiException($"'{nameof(TikTokClientOptions.AppId)}' must be configured to build a TikTok OAuth URL.");

        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new TikTokApiException("A redirect URI is required to build a TikTok OAuth URL.");

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
            throw new TikTokApiException("The TikTok OAuth redirect URI must be an absolute URI.");

        if (string.IsNullOrWhiteSpace(state))
            throw new TikTokApiException("A state value is required to build a TikTok OAuth URL.");

        if (scenario is TikTokAuthScenario.ManagedAdvertiserAccount)
            throw new TikTokApiException("Managed advertiser account mode uses the configured lifetime access token and does not start OAuth.");

        var query = new Dictionary<string, string?>
        {
            ["app_id"] = _options.AppId,
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
            ["response_type"] = "code"
        };

        var requestedScopes = scopes?.Where(scope => !string.IsNullOrWhiteSpace(scope)).ToArray()
            ?? _options.Scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).ToArray();

        if (requestedScopes.Length > 0)
            query["scope"] = string.Join(",", requestedScopes);

        if (additionalParameters is not null)
        {
            foreach (var parameter in additionalParameters)
                query[parameter.Key] = parameter.Value;
        }

        // Keep the scenario in the host app's pending OAuth state record. TikTok only needs the
        // opaque state value; the callback can use it to recover ClientAdvertiser/Social/Spark intent.
        var authUrl = string.IsNullOrWhiteSpace(_options.AuthorizationUrl)
            ? $"{_options.BaseUrl.TrimEnd('/')}/portal/auth"
            : _options.AuthorizationUrl;

        return AppendQuery(authUrl, query);
    }

    /// <inheritdoc />
    public async Task<TikTokOAuthTokenResult> ExchangeAuthorizationCodeAsync(
        string authCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) ||
            string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            throw new TikTokApiException(
                $"'{nameof(TikTokClientOptions.AppId)}' and '{nameof(TikTokClientOptions.AppSecret)}' " +
                "must be configured to exchange a TikTok OAuth code.");
        }

        if (string.IsNullOrWhiteSpace(authCode))
            throw new TikTokApiException("An authorization code is required for the TikTok OAuth token exchange.");

        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/open_api/{_options.ApiVersion}/oauth2/access_token/";
        var payload = new
        {
            app_id = _options.AppId,
            secret = _options.AppSecret,
            auth_code = authCode,
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
        TokenEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<TokenEnvelope>(body, SerializerOptions);
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

        return envelope.Data;
    }

    /// <inheritdoc />
    public TikTokClientAdvertiserConnection CreateClientAdvertiserConnection(
        TikTokOAuthTokenResult token,
        string advertiserId)
    {
        EnsureUsableToken(token);

        if (string.IsNullOrWhiteSpace(advertiserId))
            throw new TikTokApiException("A customer advertiser id is required to create a client advertiser connection.");

        if (token.AdvertiserIds.Count > 0 && !token.AdvertiserIds.Contains(advertiserId))
        {
            throw new TikTokApiException(
                $"Advertiser id '{advertiserId}' was not present in the TikTok OAuth token response.");
        }

        return new TikTokClientAdvertiserConnection
        {
            AdvertiserId = advertiserId,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresIn = token.ExpiresIn,
            RefreshExpiresIn = token.RefreshExpiresIn,
            Scope = token.Scope,
            AccessTokenExpiresAt = token.ExpiresIn is null ? null : DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn.Value),
            RefreshTokenExpiresAt = token.RefreshExpiresIn is null ? null : DateTimeOffset.UtcNow.AddSeconds(token.RefreshExpiresIn.Value),
            AdvertiserName = token.Advertisers.FirstOrDefault(advertiser => advertiser.AdvertiserId == advertiserId)?.Name,
            AdvertiserStatus = token.Advertisers.FirstOrDefault(advertiser => advertiser.AdvertiserId == advertiserId)?.Status,
            Currency = token.Advertisers.FirstOrDefault(advertiser => advertiser.AdvertiserId == advertiserId)?.Currency,
            Timezone = token.Advertisers.FirstOrDefault(advertiser => advertiser.AdvertiserId == advertiserId)?.Timezone,
            Country = token.Advertisers.FirstOrDefault(advertiser => advertiser.AdvertiserId == advertiserId)?.Country
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<TikTokClientAdvertiserConnection> CreateClientAdvertiserConnections(
        TikTokOAuthTokenResult token)
    {
        EnsureUsableToken(token);

        if (token.AdvertiserIds.Count == 0)
        {
            throw new TikTokApiException(
                "TikTok did not return advertiser ids in the OAuth token response. " +
                "Call the advertiser-list endpoint for this token, then create a connection for the selected advertiser id.");
        }

        return token.AdvertiserIds
            .Where(advertiserId => !string.IsNullOrWhiteSpace(advertiserId))
            .Select(advertiserId => CreateClientAdvertiserConnection(token, advertiserId))
            .ToArray();
    }

    /// <inheritdoc />
    public TikTokManagedAdvertiserConnection GetManagedAdvertiserConnection()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new TikTokApiException(
                $"Managed advertiser account mode requires '{nameof(TikTokClientOptions.AccessToken)}' " +
                "to contain your platform-owned long-lived TikTok access token.");
        }

        if (string.IsNullOrWhiteSpace(_options.AdvertiserId))
        {
            throw new TikTokApiException(
                $"Managed advertiser account mode requires '{nameof(TikTokClientOptions.AdvertiserId)}' " +
                "to contain your platform-owned TikTok advertiser id.");
        }

        return new TikTokManagedAdvertiserConnection
        {
            AdvertiserId = _options.AdvertiserId,
            AccessToken = _options.AccessToken,
            IdentityId = _options.ManagedIdentityId,
            IdentityType = _options.ManagedIdentityType
        };
    }

    private static string AppendQuery(string url, IReadOnlyDictionary<string, string?> parameters)
    {
        var separator = url.Contains('?') ? '&' : '?';
        var query = string.Join("&", parameters
            .Where(parameter => parameter.Value is not null)
            .Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!)}"));

        return string.IsNullOrEmpty(query) ? url : $"{url}{separator}{query}";
    }

    private static void EnsureUsableToken(TikTokOAuthTokenResult token)
    {
        if (token is null)
            throw new TikTokApiException("A TikTok OAuth token result is required to create a client advertiser connection.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new TikTokApiException("The TikTok OAuth token result does not contain an access token.");
    }

    private sealed class TokenEnvelope
    {
        [JsonPropertyName("code")] public long Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("request_id")] public string? RequestId { get; set; }
        [JsonPropertyName("data")] public TikTokOAuthTokenResult? Data { get; set; }
    }

}
