namespace Suss.TikTok.Client.Models;

using Suss.TikTok.Client.Auth;

/// <summary>
/// The creative format of a TikTok ad. Determines which asset ids and fields are required.
/// </summary>
public enum TikTokAdFormat
{
    /// <summary>A single-video in-feed ad. Requires a <see cref="TikTokAd.VideoId"/> and a cover image.</summary>
    SingleVideo,

    /// <summary>A single-image ad (e.g., for certain placements). Requires <see cref="TikTokAd.ImageIds"/>.</summary>
    SingleImage,

    /// <summary>
    /// A Spark Ad that boosts an existing organic TikTok post. Requires a
    /// <see cref="TikTokAd.TikTokItemId"/> (and an authorization code from the creator).
    /// </summary>
    SparkAd
}

/// <summary>
/// A flat representation of a TikTok ad (the bottom tier: campaign → ad group → ad).
/// <para>
/// An ad binds creative content (video/image assets or a Spark Ad post) to display text and a
/// destination. The <see cref="Format"/> dictates which asset references are required. Asset ids
/// here come from uploading <see cref="TikTokAsset"/> instances first. <see cref="AdId"/> is
/// populated after creation; <see cref="AdGroupId"/> is wired by the workflow.
/// </para>
/// </summary>
public sealed class TikTokAd
{
    /// <summary>The server-assigned ad id. <c>null</c> before creation.</summary>
    public string? AdId { get; set; }

    /// <summary>The parent ad group id. Set automatically by the workflow after the ad group is created.</summary>
    public string? AdGroupId { get; set; }

    /// <summary>A human-readable ad name (internal; not shown to users).</summary>
    public required string AdName { get; set; }

    /// <summary>The creative format that determines required asset references.</summary>
    public required TikTokAdFormat Format { get; set; }

    /// <summary>The brand/display name shown on the ad (TikTok <c>display_name</c>).</summary>
    public string? DisplayName { get; set; }

    /// <summary>The ad caption/primary text (TikTok <c>ad_text</c>).</summary>
    public string? AdText { get; set; }

    /// <summary>The call-to-action label (TikTok <c>call_to_action</c>), e.g., <c>LEARN_MORE</c>, <c>SHOP_NOW</c>.</summary>
    public string? CallToAction { get; set; }

    /// <summary>The landing page URL the ad clicks through to (TikTok <c>landing_page_url</c>).</summary>
    public string? LandingPageUrl { get; set; }

    /// <summary>
    /// The uploaded video id (from a <see cref="TikTokAsset"/> upload). For URL-backed campaign
    /// assets, use <see cref="VideoUrl"/> instead.
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>The remote video URL to pass directly to TikTok ad creation.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>
    /// The cover/thumbnail image id used with a video ad (TikTok <c>image_ids</c> first entry for video).
    /// </summary>
    public string? CoverImageId { get; set; }

    /// <summary>The remote cover image URL to pass directly to TikTok ad creation.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Uploaded image ids (from <see cref="TikTokAsset"/> uploads). For URL-backed campaign assets,
    /// use <see cref="ImageUrls"/> instead.
    /// </summary>
    public List<string>? ImageIds { get; set; }

    /// <summary>Remote image URLs to pass directly to TikTok ad creation.</summary>
    public List<string>? ImageUrls { get; set; }

    /// <summary>
    /// The organic TikTok post id to boost. Required for <see cref="TikTokAdFormat.SparkAd"/>
    /// (TikTok <c>tiktok_item_id</c>).
    /// </summary>
    public string? TikTokItemId { get; set; }

    /// <summary>The identity (TikTok <c>identity_id</c>) the ad is published under.</summary>
    public string? IdentityId { get; set; }

    /// <summary>The identity type (TikTok <c>identity_type</c>), e.g., <c>CUSTOMIZED_USER</c>, <c>AUTH_CODE</c>.</summary>
    public string? IdentityType { get; set; }

    /// <summary>
    /// Optional Spark authorization captured from a client's TikTok social account/post. When set,
    /// this supplies <see cref="TikTokItemId"/>, <see cref="IdentityId"/>, and
    /// <see cref="IdentityType"/> for <see cref="TikTokAdFormat.SparkAd"/>.
    /// </summary>
    public TikTokSparkAdAuthorization? SparkAuthorization { get; set; }

    /// <summary>Operation status (TikTok <c>operation_status</c>): <c>ENABLE</c> or <c>DISABLE</c>.</summary>
    public string OperationStatus { get; set; } = "ENABLE";
}
