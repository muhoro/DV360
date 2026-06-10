namespace Suss.TikTok.Client.Models;

/// <summary>
/// Identifies the kind of media being uploaded to TikTok. Each type uses a different upload endpoint
/// and produces a different server-assigned identifier referenced later by creatives/ads.
/// </summary>
public enum TikTokAssetType
{
    /// <summary>A static image (used for image ads and as video cover/thumbnail).</summary>
    Image,

    /// <summary>A video (used for in-feed video ads and Spark Ads fallbacks).</summary>
    Video,

    /// <summary>An audio track (used where audio assets are supported).</summary>
    Audio
}

/// <summary>
/// Represents a single media asset to upload to TikTok and the identifiers returned after upload.
/// <para>
/// Mirrors the DV360 "upload then reference" pattern: an asset is uploaded first, then its
/// server-assigned id (<see cref="ImageId"/> / <see cref="VideoId"/> / <see cref="AudioId"/>) is
/// referenced by an ad's creative. Set <see cref="FilePath"/> (or <see cref="Content"/>) and
/// <see cref="AssetType"/> before upload; the id fields are populated afterwards.
/// </para>
/// </summary>
public sealed class TikTokAsset
{
    /// <summary>The kind of media this asset represents. Determines which upload endpoint is used.</summary>
    public required TikTokAssetType AssetType { get; set; }

    /// <summary>Absolute or relative path to the local file to upload. Required unless <see cref="Content"/> is set.</summary>
    public string? FilePath { get; set; }

    /// <summary>Raw file bytes to upload. If set, used instead of reading <see cref="FilePath"/>.</summary>
    public byte[]? Content { get; set; }

    /// <summary>The file name reported to TikTok (e.g., "promo.mp4"). Defaults to the file name of <see cref="FilePath"/>.</summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Optional friendly name used in the TikTok asset library. If omitted, TikTok derives one.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>The server-assigned image id, populated after a successful image upload.</summary>
    public string? ImageId { get; set; }

    /// <summary>The server-assigned video id, populated after a successful video upload.</summary>
    public string? VideoId { get; set; }

    /// <summary>The server-assigned audio id, populated after a successful audio upload.</summary>
    public string? AudioId { get; set; }

    /// <summary>
    /// A CDN/preview URL returned by TikTok for the uploaded asset, when provided (videos/images).
    /// </summary>
    public string? PreviewUrl { get; set; }
}
