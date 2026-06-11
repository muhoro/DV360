namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Customer TikTok social identity authorization metadata.
/// <para>
/// This is not campaign API authorization and must not replace advertiser-account credentials.
/// </para>
/// </summary>
public sealed class TikTokIdentityConnection
{
    /// <summary>The stored identity connection id in the host application.</summary>
    public Guid Id { get; init; }

    /// <summary>The organization/customer that owns this identity connection.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>The TikTok identity id.</summary>
    public required string IdentityId { get; init; }

    /// <summary>The TikTok identity type.</summary>
    public required string IdentityType { get; init; }

    /// <summary>The TikTok display name, when known.</summary>
    public string? DisplayName { get; init; }

    /// <summary>The TikTok profile image URL, when known.</summary>
    public string? ProfileImageUrl { get; init; }

    /// <summary>The source of this authorization, such as OAuth, QR, or Spark code.</summary>
    public string? AuthorizationSource { get; init; }

    /// <summary>The current authorization status.</summary>
    public TikTokConnectionStatus AuthorizationStatus { get; init; } = TikTokConnectionStatus.Active;

    /// <summary>The time this authorization expires, when known.</summary>
    public DateTimeOffset? AuthorizationExpiresAt { get; init; }
}
