namespace Suss.TikTok.Client.Models;

/// <summary>
/// The kind of identifier contained in a customer-match audience file.
/// <para>
/// TikTok hashes these identifiers (SHA-256) to match users while preserving privacy. This client
/// hashes the raw values for you before upload when <see cref="TikTokAudienceFile.ValuesArePreHashed"/>
/// is <c>false</c>.
/// </para>
/// </summary>
public enum TikTokAudienceIdType
{
    /// <summary>Email addresses (normalized to lowercase, trimmed, then SHA-256 hashed).</summary>
    Email,

    /// <summary>Phone numbers in E.164 format (trimmed, then SHA-256 hashed).</summary>
    PhoneNumber,

    /// <summary>Mobile advertising IDs (IDFA/GAID), lowercased then SHA-256 hashed.</summary>
    MobileAdvertiserId
}

/// <summary>
/// Represents the raw identifiers used to build (or append to) a TikTok custom audience.
/// </summary>
public sealed class TikTokAudienceFile
{
    /// <summary>The type of identifier the <see cref="Values"/> contain.</summary>
    public required TikTokAudienceIdType IdType { get; set; }

    /// <summary>
    /// The raw or pre-hashed identifier values (emails, phone numbers, or mobile advertiser ids).
    /// </summary>
    public required IReadOnlyList<string> Values { get; set; }

    /// <summary>
    /// When <c>true</c>, <see cref="Values"/> are already SHA-256 hashed and are uploaded as-is.
    /// When <c>false</c> (default), the client normalizes and hashes them before upload.
    /// </summary>
    public bool ValuesArePreHashed { get; set; }
}

/// <summary>
/// A flat representation of a TikTok custom audience.
/// <para>
/// Audiences can be created from customer-match files (emails, phone numbers, mobile advertiser ids)
/// and then referenced by ad groups via <see cref="TikTokAdGroup.IncludedAudienceIds"/> /
/// <see cref="TikTokAdGroup.ExcludedAudienceIds"/>. <see cref="AudienceId"/> is populated after
/// creation/upload.
/// </para>
/// </summary>
public sealed class TikTokAudience
{
    /// <summary>The server-assigned audience id. <c>null</c> before creation; populated afterwards.</summary>
    public string? AudienceId { get; set; }

    /// <summary>A human-readable audience name.</summary>
    public required string AudienceName { get; set; }

    /// <summary>
    /// The customer-match file used to build the audience. When creating an audience from identifiers,
    /// this is required.
    /// </summary>
    public TikTokAudienceFile? File { get; set; }
}
