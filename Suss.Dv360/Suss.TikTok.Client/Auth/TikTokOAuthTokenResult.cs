using System.Text.Json.Serialization;

namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Token payload returned by TikTok after an OAuth authorization-code exchange.
/// Store this data against the customer/business account that was linked.
/// </summary>
public sealed class TikTokOAuthTokenResult
{
    /// <summary>The access token to send in TikTok API requests using the <c>Access-Token</c> header.</summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    /// <summary>Refresh token, when returned by the TikTok app/API version.</summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    /// <summary>Access-token lifetime in seconds, when returned by TikTok.</summary>
    [JsonPropertyName("expires_in")]
    public long? ExpiresIn { get; init; }

    /// <summary>Refresh-token lifetime in seconds, when returned by TikTok.</summary>
    [JsonPropertyName("refresh_expires_in")]
    public long? RefreshExpiresIn { get; init; }

    /// <summary>Granted scope string, when returned by TikTok.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>Advertiser accounts available through this authorization, when returned by TikTok.</summary>
    [JsonPropertyName("advertiser_ids")]
    public IReadOnlyList<string> AdvertiserIds { get; init; } = [];

    /// <summary>Advertiser account metadata available through this authorization, when known.</summary>
    public IReadOnlyList<TikTokAdvertiserAccount> Advertisers { get; init; } = [];
}
