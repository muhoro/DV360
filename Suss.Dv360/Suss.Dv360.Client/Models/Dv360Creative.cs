namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a Display &amp; Video 360 creative asset.
/// <para>
/// Maps to the Google SDK’s <c>Creative</c> model, flattening nested <c>Dimensions</c>
/// into individual width/height properties. Supports third-party tag creatives and
/// hosted display creatives.
/// </para>
/// </summary>
public sealed class Dv360Creative
{
    /// <summary>
    /// The DV360-assigned creative identifier. <c>null</c> until the creative is created.
    /// </summary>
    public long? CreativeId { get; set; }

    /// <summary>
    /// Human-readable name of the creative shown in the DV360 UI.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The entity lifecycle status (e.g., <c>"ENTITY_STATUS_ACTIVE"</c>).
    /// Defaults to <c>"ENTITY_STATUS_ACTIVE"</c>.
    /// </summary>
    public string EntityStatus { get; set; } = "ENTITY_STATUS_ACTIVE";

    /// <summary>
    /// The type of creative (e.g., <c>"CREATIVE_TYPE_STANDARD"</c>, <c>"CREATIVE_TYPE_VIDEO"</c>).
    /// </summary>
    public required string CreativeType { get; set; }

    /// <summary>
    /// Where the creative asset is hosted
    /// (e.g., <c>"HOSTING_SOURCE_HOSTED"</c>, <c>"HOSTING_SOURCE_THIRD_PARTY"</c>).
    /// <c>null</c> when not applicable.
    /// </summary>
    public string? HostingSource { get; set; }

    /// <summary>
    /// The width of the creative in pixels. <c>null</c> when dimensions are not specified.
    /// </summary>
    public int? WidthPixels { get; set; }

    /// <summary>
    /// The height of the creative in pixels. <c>null</c> when dimensions are not specified.
    /// </summary>
    public int? HeightPixels { get; set; }

    /// <summary>
    /// The third-party ad tag markup (HTML/JavaScript) for externally hosted creatives.
    /// <c>null</c> when the creative is DV360-hosted.
    /// </summary>
    public string? ThirdPartyTag { get; set; }

    /// <summary>
    /// The media assets (images, videos, HTML5 bundles) associated with this creative.
    /// <para>
    /// For hosted creatives (<c>HOSTING_SOURCE_HOSTED</c>), provide at least one asset with
    /// a <see cref="Dv360CreativeAsset.FilePath"/> set. The files are uploaded automatically
    /// during creative creation, and each asset's <see cref="Dv360CreativeAsset.MediaId"/>
    /// is populated upon successful upload.
    /// </para>
    /// <para>
    /// Not required for third-party tag creatives where <see cref="ThirdPartyTag"/> is used instead.
    /// </para>
    /// </summary>
    public List<Dv360CreativeAsset>? Assets { get; set; }
}
