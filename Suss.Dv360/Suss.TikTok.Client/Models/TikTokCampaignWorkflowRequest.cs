namespace Suss.TikTok.Client.Models;

/// <summary>
/// Input model for the end-to-end TikTok campaign creation workflow.
/// <para>
/// Bundles every resource needed to execute the full workflow:
/// upload assets → (optionally) create/reference audiences → create campaign → create ad group →
/// create ads. The workflow automatically wires parent identifiers (campaign id → ad group,
/// ad group id → ads) and uploaded asset ids (video/image ids → ads) across steps.
/// </para>
/// </summary>
public sealed class TikTokCampaignWorkflowRequest
{
    /// <summary>
    /// The TikTok advertiser id under which all resources are created. When <c>null</c>, the
    /// advertiser id from <see cref="Configuration.TikTokClientOptions.AdvertiserId"/> is used.
    /// </summary>
    public string? AdvertiserId { get; set; }

    /// <summary>
    /// Optional resolved context. When supplied, the workflow uses its access token and advertiser id
    /// for every TikTok API call.
    /// </summary>
    public TikTokExecutionContext? ExecutionContext { get; set; }

    /// <summary>
    /// Assets to upload before ad creation. Each asset's id (<see cref="TikTokAsset.VideoId"/> /
    /// <see cref="TikTokAsset.ImageId"/> / <see cref="TikTokAsset.AudioId"/>) is populated after upload.
    /// </summary>
    public List<TikTokAsset> Assets { get; set; } = [];

    /// <summary>
    /// Custom audiences to create from customer-match files. Their <see cref="TikTokAudience.AudienceId"/>
    /// values are populated after creation and can be referenced by <see cref="AdGroup"/>.
    /// </summary>
    public List<TikTokAudience> Audiences { get; set; } = [];

    /// <summary>The campaign to create. Its <see cref="TikTokCampaign.CampaignId"/> is populated after creation.</summary>
    public required TikTokCampaign Campaign { get; set; }

    /// <summary>
    /// The ad group to create under the campaign. Its <see cref="TikTokAdGroup.CampaignId"/> is set
    /// automatically by the workflow.
    /// </summary>
    public required TikTokAdGroup AdGroup { get; set; }

    /// <summary>
    /// One or more ads to create under the ad group. Each ad's <see cref="TikTokAd.AdGroupId"/> is set
    /// automatically by the workflow.
    /// </summary>
    public required List<TikTokAd> Ads { get; set; }
}
