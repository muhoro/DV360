namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Represents the platform-owned TikTok advertiser account used when customers run campaigns
/// through your ad account.
/// </summary>
public sealed class TikTokManagedAdvertiserConnection
{
    /// <summary>The organization/customer mapped to this managed advertiser execution lane.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>The scenario represented by this connection.</summary>
    public TikTokAuthScenario Scenario { get; init; } = TikTokAuthScenario.ManagedAdvertiserAccount;

    /// <summary>The platform-owned advertiser id used for campaign, ad group, ad, and asset calls.</summary>
    public required string AdvertiserId { get; init; }

    /// <summary>The platform-owned long-lived access token used in the <c>Access-Token</c> header.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Optional platform-owned identity id used as the default ad identity.</summary>
    public string? IdentityId { get; init; }

    /// <summary>Optional platform-owned identity type used as the default ad identity.</summary>
    public string? IdentityType { get; init; }

    /// <summary>Advertiser currency, when known.</summary>
    public string? Currency { get; init; }

    /// <summary>Advertiser timezone, when known.</summary>
    public string? Timezone { get; init; }
}
