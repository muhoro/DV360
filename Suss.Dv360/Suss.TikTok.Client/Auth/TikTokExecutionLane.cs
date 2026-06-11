namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Identifies which advertiser-account lane should execute TikTok campaign API calls.
/// </summary>
public enum TikTokExecutionLane
{
    /// <summary>Use the platform-owned managed TikTok advertiser account.</summary>
    ManagedAdvertiserAccount = 1,

    /// <summary>Use a customer-owned TikTok advertiser account linked through OAuth.</summary>
    ClientAdvertiserAccount = 2
}
