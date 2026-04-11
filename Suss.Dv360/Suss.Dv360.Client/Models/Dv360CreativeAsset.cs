namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a media asset (image, video, HTML5 bundle, etc.) that is uploaded to DV360
/// and associated with a creative.
/// <para>
/// Before creating a creative, each asset file must be uploaded via the DV360
/// <c>advertisers.assets.upload</c> endpoint, which returns a <see cref="MediaId"/>.
/// That <see cref="MediaId"/> is then referenced in the creative's <c>Assets</c> collection
/// as an <c>AssetAssociation</c>.
/// </para>
/// </summary>
public sealed class Dv360CreativeAsset
{
    /// <summary>
    /// The local file path of the asset to upload (e.g., <c>"assets/banner_300x250.png"</c>).
    /// Required when creating a new creative with hosted assets from a local file.
    /// Ignored when <see cref="Url"/> is set or when reading existing creatives from DV360.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// A remote URL from which the asset will be downloaded before uploading to DV360
    /// (e.g., <c>"https://example.com/banners/banner_300x250.png"</c>).
    /// When set, <see cref="FilePath"/> is ignored and the asset is streamed directly from the URL.
    /// <see cref="MimeType"/> is still required; <see cref="Filename"/> is derived from the URL
    /// if not explicitly provided.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The MIME type of the asset file (e.g., <c>"image/png"</c>, <c>"video/mp4"</c>,
    /// <c>"application/zip"</c> for HTML5 bundles).
    /// Required when <see cref="FilePath"/> or <see cref="Url"/> is specified.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// The role this asset plays within the creative
    /// (e.g., <c>"ASSET_ROLE_MAIN"</c>, <c>"ASSET_ROLE_BACKUP"</c>).
    /// Defaults to <c>"ASSET_ROLE_MAIN"</c>.
    /// </summary>
    public string Role { get; set; } = "ASSET_ROLE_MAIN";

    /// <summary>
    /// The filename sent to DV360 during upload. If <c>null</c>, the filename is derived
    /// from <see cref="FilePath"/> or <see cref="Url"/>.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// The DV360-assigned media identifier. <c>null</c> until the asset has been uploaded.
    /// Populated automatically by the <see cref="Services.CreativeService"/> during creative creation.
    /// </summary>
    public long? MediaId { get; set; }

    /// <summary>
    /// The asset content URL returned by DV360 after upload.
    /// <c>null</c> until the asset has been uploaded.
    /// </summary>
    public string? Content { get; set; }
}
