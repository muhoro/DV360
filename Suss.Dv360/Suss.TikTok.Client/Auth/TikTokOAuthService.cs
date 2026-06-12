using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Exceptions;
using Suss.TikTok.Client.Models;

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
            TikTokAuthMode.ClientSocialIdentity,
            redirectUri,
            state,
            scopes,
            additionalParameters);

    /// <inheritdoc />
    public string BuildAuthorizationUrl(
        TikTokAuthMode authMode,
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

        if (authMode is TikTokAuthMode.ManagedAdvertiserAccount)
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

        // Keep the auth mode in the workflow/host app's pending OAuth state record. TikTok only needs the
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

    private static string AppendQuery(string url, IReadOnlyDictionary<string, string?> parameters)
    {
        var separator = url.Contains('?') ? '&' : '?';
        var query = string.Join("&", parameters
            .Where(parameter => parameter.Value is not null)
            .Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!)}"));

        return string.IsNullOrEmpty(query) ? url : $"{url}{separator}{query}";
    }

    private sealed class TokenEnvelope
    {
        [JsonPropertyName("code")] public long Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("request_id")] public string? RequestId { get; set; }
        [JsonPropertyName("data")] public TikTokOAuthTokenResult? Data { get; set; }
    }

}
