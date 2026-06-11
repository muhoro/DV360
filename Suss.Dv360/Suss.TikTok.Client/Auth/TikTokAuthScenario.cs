namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Describes the TikTok authorization lane being used for a customer.
/// <para>
/// These scenarios are deliberately separate because managed-account access, customer advertiser
/// access, social identity linking, and Spark post authorization produce different identifiers and should be stored
/// independently by the host application.
/// </para>
/// </summary>
public enum TikTokAuthScenario
{
    /// <summary>
    /// Campaigns run through the platform-owned TikTok advertiser account using the platform's
    /// long-lived access token and advertiser id.
    /// </summary>
    ManagedAdvertiserAccount,

    /// <summary>
    /// A customer-owned TikTok Business/Ads advertiser account is linked to your app through OAuth.
    /// Campaigns run in the customer's advertiser account using the customer-granted token and the
    /// selected customer advertiser id.
    /// </summary>
    ClientAdvertiserAccount,

    /// <summary>
    /// A customer's TikTok social identity is authorized or linked so it can be referenced by ads,
    /// while campaign execution may still use the managed advertiser account.
    /// </summary>
    ClientSocialIdentity,

    /// <summary>
    /// A customer authorizes an organic TikTok post/identity for Spark Ad use, while campaign
    /// execution may still use the managed advertiser account.
    /// </summary>
    SparkAdAuthorization
}
