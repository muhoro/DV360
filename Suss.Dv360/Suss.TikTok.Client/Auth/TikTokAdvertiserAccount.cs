using System.Text.Json.Serialization;

namespace Suss.TikTok.Client.Auth;

/// <summary>
/// Metadata for an advertiser account available to a TikTok OAuth token.
/// </summary>
public sealed class TikTokAdvertiserAccount
{
    /// <summary>The TikTok advertiser id.</summary>
    [JsonPropertyName("advertiser_id")]
    public required string AdvertiserId { get; init; }

    /// <summary>The advertiser display name, when returned by TikTok.</summary>
    [JsonPropertyName("advertiser_name")]
    public string? Name { get; init; }

    /// <summary>The advertiser status, when returned by TikTok.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>The advertiser currency, when returned by TikTok.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    /// <summary>The advertiser timezone, when returned by TikTok.</summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    /// <summary>The advertiser country, when returned by TikTok.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }
}
