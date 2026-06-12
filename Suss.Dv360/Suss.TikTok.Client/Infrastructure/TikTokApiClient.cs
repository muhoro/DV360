using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Suss.TikTok.Client.Auth;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Exceptions;
using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Infrastructure;

/// <summary>
/// Default <see cref="ITikTokApiClient"/> implementation backed by <see cref="HttpClient"/>.
/// <para>
/// This is the single place that knows how to talk HTTP to TikTok. It builds full URLs from the
/// configured host and version, attaches the <c>Access-Token</c> header (resolved lazily from the
/// <see cref="ITikTokAuthProvider"/>), sends/receives JSON, and unwraps the standard envelope —
/// converting non-zero codes into a <see cref="TikTokApiException"/>. Resource services consume
/// the unwrapped <c>data</c> payloads and never see envelopes or raw HTTP.
/// </para>
/// </summary>
/// <param name="httpClient">The HTTP client (configured with timeouts/handlers by DI).</param>
/// <param name="authProvider">Resolves the access token for the <c>Access-Token</c> header.</param>
/// <param name="options">Client options carrying the base URL and API version.</param>
/// <param name="logger">Logger for request/response diagnostics.</param>
internal sealed class TikTokApiClient(
    HttpClient httpClient,
    ITikTokAuthProvider authProvider,
    IOptions<TikTokClientOptions> options,
    ILogger<TikTokApiClient> logger) : ITikTokApiClient
{
    private readonly TikTokClientOptions _options = options.Value;

    /// <summary>Shared serializer options; property names are mapped via <c>[JsonPropertyName]</c> on models.</summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<TData> GetAsync<TData>(
        string path,
        IDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(path, queryParameters);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<TData>(request, accessToken: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TData> GetAsync<TData>(
        TikTokExecutionContext executionContext,
        string path,
        IDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        EnsureExecutionContext(executionContext);
        var url = BuildUrl(path, queryParameters);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<TData>(request, executionContext.AccessToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TData> PostAsync<TData>(
        string path,
        object body,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(path, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: SerializerOptions)
        };
        return await SendAsync<TData>(request, accessToken: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TData> PostAsync<TData>(
        TikTokExecutionContext executionContext,
        string path,
        object body,
        CancellationToken cancellationToken = default)
    {
        EnsureExecutionContext(executionContext);
        var url = BuildUrl(path, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: SerializerOptions)
        };
        return await SendAsync<TData>(request, executionContext.AccessToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TData> PostMultipartAsync<TData>(
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(path, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        return await SendAsync<TData>(request, accessToken: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TData> PostMultipartAsync<TData>(
        TikTokExecutionContext executionContext,
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default)
    {
        EnsureExecutionContext(executionContext);
        var url = BuildUrl(path, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        return await SendAsync<TData>(request, executionContext.AccessToken, cancellationToken);
    }

    /// <summary>
    /// Core send routine shared by all verbs: attaches auth, executes the request, and unwraps the envelope.
    /// </summary>
    private async Task<TData> SendAsync<TData>(
        HttpRequestMessage request,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        // TikTok uses a custom "Access-Token" header. Context-based calls pass the already
        // resolved token; legacy calls resolve through the configured auth provider.
        var token = accessToken ?? await authProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            throw new TikTokApiException("TikTok API call cannot be sent because the access token is missing.");

        request.Headers.Remove("Access-Token");
        request.Headers.Add("Access-Token", token);

        HttpResponseMessage response;
        try
        {
            logger.LogDebug("TikTok API request: {Method} {Uri}", request.Method, request.RequestUri);
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not TikTokApiException)
        {
            throw new TikTokApiException($"Transport failure calling '{request.RequestUri}'.", ex);
        }

        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // A non-success HTTP status is unusual for this API (it favors envelope codes), but we still
        // surface it clearly when it happens (e.g., 401/5xx from gateways).
        if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(rawBody))
        {
            throw new TikTokApiException(
                $"TikTok API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) for '{request.RequestUri}'.");
        }

        TikTokApiResponse<TData>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<TikTokApiResponse<TData>>(rawBody, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new TikTokApiException($"Could not parse the TikTok API response for '{request.RequestUri}'.", ex);
        }

        if (envelope is null)
        {
            throw new TikTokApiException($"Empty response from TikTok API for '{request.RequestUri}'.");
        }

        // The contract: code == 0 is success. Anything else is a business/validation error.
        if (envelope.Code != 0)
        {
            logger.LogError(
                "TikTok API error. Code={Code}, Message={Message}, RequestId={RequestId}",
                envelope.Code, envelope.Message, envelope.RequestId);

            throw new TikTokApiException(
                $"TikTok API error {envelope.Code}: {envelope.Message}",
                envelope.Code, envelope.RequestId, rawBody);
        }

        if (envelope.Data is null)
        {
            throw new TikTokApiException(
                $"TikTok API returned success but no data payload for '{request.RequestUri}'.",
                envelope.Code, envelope.RequestId, rawBody);
        }

        return envelope.Data;
    }

    /// <summary>
    /// Builds an absolute endpoint URL from the configured base URL, the <c>/open_api/{version}/</c>
    /// prefix, the relative path, and any query parameters.
    /// </summary>
    private string BuildUrl(string path, IDictionary<string, string?>? queryParameters)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var relative = path.TrimStart('/');
        var url = $"{baseUrl}/open_api/{_options.ApiVersion}/{relative}";

        if (queryParameters is { Count: > 0 })
        {
            var query = string.Join("&", queryParameters
                .Where(kvp => kvp.Value is not null)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

            if (!string.IsNullOrEmpty(query))
                url = $"{url}?{query}";
        }

        return url;
    }

    private static void EnsureExecutionContext(TikTokExecutionContext executionContext)
    {
        if (executionContext is null)
            throw new TikTokApiException("A TikTok execution context is required for this API call.");

        if (string.IsNullOrWhiteSpace(executionContext.AccessToken))
            throw new TikTokApiException("The TikTok execution context is missing an access token.");

        if (string.IsNullOrWhiteSpace(executionContext.AdvertiserId))
            throw new TikTokApiException("The TikTok execution context is missing an advertiser id.");
    }
}
