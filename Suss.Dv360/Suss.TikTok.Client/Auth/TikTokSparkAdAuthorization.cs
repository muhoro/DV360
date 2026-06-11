namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Spark Ad authorization captured from a client's TikTok social account or organic post.
/// <para>
/// Store this against the customer/business record that granted permission. Campaign execution can
/// use either your managed advertiser account or the customer's linked advertiser account; this
/// object supplies the social identity/post reference used by the Spark creative.
/// </para>
/// </summary>
public sealed class TikTokSparkAdAuthorization
{
    /// <summary>The stored Spark authorization id in the host application.</summary>
    public Guid Id { get; init; }

    /// <summary>The organization/customer that owns this Spark permission.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>The scenario represented by this authorization.</summary>
    public TikTokAuthScenario Scenario { get; init; } = TikTokAuthScenario.SparkAdAuthorization;

    /// <summary>The organic TikTok post/item id to boost.</summary>
    public required string TikTokItemId { get; init; }

    /// <summary>The authorized identity id to use for the Spark creative.</summary>
    public required string IdentityId { get; init; }

    /// <summary>The identity type accepted by TikTok, commonly <c>AUTH_CODE</c> or a custom identity type.</summary>
    public string IdentityType { get; init; } = "AUTH_CODE";

    /// <summary>
    /// Optional raw authorization code or creator-supplied code if your onboarding flow captures
    /// it before resolving an identity id. This value is for storage/audit and is not sent by the
    /// ad service unless TikTok's identity id for your app is the same value.
    /// </summary>
    public string? AuthorizationCode { get; init; }

    /// <summary>Optional expiry timestamp for creator-supplied Spark permissions.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Authorization status used to reject inactive/revoked Spark permissions before ad creation.</summary>
    public TikTokConnectionStatus AuthorizationStatus { get; init; } = TikTokConnectionStatus.Active;

    /// <summary>Returns true when this Spark permission is active at the supplied time.</summary>
    public bool IsActive(DateTimeOffset now)
        => AuthorizationStatus is TikTokConnectionStatus.Active && (ExpiresAt is null || ExpiresAt > now);
}
