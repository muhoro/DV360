namespace Suss.TikTok.Client.Models;

/// <summary>
/// Represents a customer-owned TikTok advertiser account linked to your app through OAuth.
/// <para>
/// Use this auth mode when the customer has their own TikTok Business/Ads account and grants your
/// app permission to manage campaigns in their advertiser account. Store this connection against
/// the customer/business record in the host application.
/// </para>
/// </summary>
public sealed class TikTokClientAdvertiserConnection
{
    /// <summary>The stored connection id in the host application.</summary>
    public Guid Id { get; init; }

    /// <summary>The organization/customer that owns this connection.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>The authorization mode represented by this connection.</summary>
    public TikTokAuthMode AuthMode { get; init; } = TikTokAuthMode.ClientAdvertiserAccount;

    /// <summary>The customer-owned advertiser id selected after OAuth.</summary>
    public required string AdvertiserId { get; init; }

    /// <summary>The OAuth access token returned after the customer's business user grants consent.</summary>
    public required string AccessToken { get; set; }

    /// <summary>The OAuth refresh token, when returned by TikTok.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Access-token lifetime in seconds, when returned by TikTok.</summary>
    public long? ExpiresIn { get; init; }

    /// <summary>Refresh-token lifetime in seconds, when returned by TikTok.</summary>
    public long? RefreshExpiresIn { get; init; }

    /// <summary>Granted scope string, when returned by TikTok.</summary>
    public string? Scope { get; init; }

    /// <summary>Absolute access-token expiry timestamp, when known.</summary>
    public DateTimeOffset? AccessTokenExpiresAt { get; set; }

    /// <summary>Absolute refresh-token expiry timestamp, when known.</summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>Connection authorization status.</summary>
    public TikTokConnectionStatus AuthorizationStatus { get; set; } = TikTokConnectionStatus.Active;

    /// <summary>Advertiser display name, when known from an advertiser-list call.</summary>
    public string? AdvertiserName { get; init; }

    /// <summary>Advertiser account status, when known.</summary>
    public string? AdvertiserStatus { get; init; }

    /// <summary>Advertiser currency, when known.</summary>
    public string? Currency { get; init; }

    /// <summary>Advertiser timezone, when known.</summary>
    public string? Timezone { get; init; }

    /// <summary>Advertiser country, when known.</summary>
    public string? Country { get; init; }
}
