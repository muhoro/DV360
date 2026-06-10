namespace Suss.TikTok.Client.Models;

/// <summary>
/// Output model returned by the TikTok campaign creation workflow after all resources have been
/// successfully created and linked.
/// <para>
/// Every object carries its TikTok-assigned identifier, confirming server-side creation. Use these
/// ids for subsequent operations (status updates, reporting, etc.).
/// </para>
/// </summary>
public sealed class TikTokCampaignWorkflowResult
{
    /// <summary>All uploaded assets, each with its server-assigned video/image/audio id.</summary>
    public required IReadOnlyList<TikTokAsset> Assets { get; set; }

    /// <summary>All created/referenced audiences, each with its server-assigned <see cref="TikTokAudience.AudienceId"/>.</summary>
    public required IReadOnlyList<TikTokAudience> Audiences { get; set; }

    /// <summary>The created campaign with its TikTok-assigned <see cref="TikTokCampaign.CampaignId"/>.</summary>
    public required TikTokCampaign Campaign { get; set; }

    /// <summary>The created ad group with its TikTok-assigned <see cref="TikTokAdGroup.AdGroupId"/>.</summary>
    public required TikTokAdGroup AdGroup { get; set; }

    /// <summary>All created ads, each with its TikTok-assigned <see cref="TikTokAd.AdId"/>.</summary>
    public required IReadOnlyList<TikTokAd> Ads { get; set; }
}
