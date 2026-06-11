namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Lifecycle status for stored TikTok advertiser, identity, and Spark authorizations.
/// </summary>
public enum TikTokConnectionStatus
{
    /// <summary>The authorization is usable.</summary>
    Active = 1,

    /// <summary>The authorization or token has expired.</summary>
    Expired = 2,

    /// <summary>The user or TikTok revoked the authorization.</summary>
    Revoked = 3,

    /// <summary>The authorization is malformed or otherwise unusable.</summary>
    Invalid = 4
}
