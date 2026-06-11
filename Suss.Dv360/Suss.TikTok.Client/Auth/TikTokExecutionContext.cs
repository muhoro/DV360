namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Resolved execution context for TikTok Marketing API calls.
/// <para>
/// Campaign, ad group, ad, creative, asset, audience, and reporting operations should use this
/// context's <see cref="AccessToken"/> and <see cref="AdvertiserId"/> instead of guessing from raw
/// organization settings. TikTok identity and Spark authorization remain separate concerns.
/// </para>
/// </summary>
public sealed class TikTokExecutionContext
{
    /// <summary>The organization/customer in the host application.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>The TikTok advertiser id that owns the API operation.</summary>
    public required string AdvertiserId { get; set; }

    /// <summary>The valid access token to send in the TikTok <c>Access-Token</c> header.</summary>
    public required string AccessToken { get; set; }

    /// <summary>The execution lane used to resolve this context.</summary>
    public TikTokExecutionLane ExecutionLane { get; set; }

    /// <summary>The stored customer connection id, when using a client advertiser account.</summary>
    public Guid? ConnectionId { get; set; }

    /// <summary>The advertiser currency, when known.</summary>
    public string? Currency { get; set; }

    /// <summary>The advertiser timezone, when known.</summary>
    public string? Timezone { get; set; }
}
