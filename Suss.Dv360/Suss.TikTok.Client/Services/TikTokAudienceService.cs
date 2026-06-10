using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Suss.TikTok.Client.Infrastructure;
using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Default <see cref="ITikTokAudienceService"/> implementation.
/// <para>
/// Builds customer-match audiences from emails, phone numbers, or mobile advertiser ids. Raw values
/// are normalized (lowercase/trim for emails and MAIDs; trim for phone numbers) and SHA-256 hashed
/// locally before being sent to TikTok's <c>dmp/custom_audience/file/upload/</c> +
/// <c>dmp/custom_audience/create/</c> endpoints. Existing audiences can be listed and referenced by
/// id via <see cref="ListAsync"/>.
/// </para>
/// </summary>
/// <param name="apiClient">The transport abstraction used to call TikTok.</param>
/// <param name="logger">Logger for diagnostics.</param>
internal sealed class TikTokAudienceService(
    ITikTokApiClient apiClient,
    ILogger<TikTokAudienceService> logger) : ITikTokAudienceService
{
    /// <inheritdoc />
    public async Task<TikTokAudience> CreateFromFileAsync(
        string advertiserId,
        TikTokAudience audience,
        CancellationToken cancellationToken = default)
    {
        if (audience.File is null || audience.File.Values.Count == 0)
            throw new InvalidOperationException("TikTokAudience.File with at least one value is required.");

        logger.LogInformation(
            "Creating audience '{Name}' from {Count} {IdType} value(s) for advertiser {AdvertiserId}.",
            audience.AudienceName, audience.File.Values.Count, audience.File.IdType, advertiserId);

        // 1) Normalize + hash the identifiers locally (unless caller says they are pre-hashed).
        var hashed = audience.File.ValuesArePreHashed
            ? audience.File.Values.ToList()
            : audience.File.Values.Select(v => HashIdentifier(v, audience.File.IdType)).ToList();

        // 2) Upload the hashed identifiers to obtain a reusable file_path token.
        var calcType = MapCalculateType(audience.File.IdType);
        var uploadBody = new UploadFileBody
        {
            AdvertiserId = advertiserId,
            CalculateType = calcType,
            FileName = $"{audience.AudienceName}.csv",
            IdSchema = MapIdSchema(audience.File.IdType),
            Data = string.Join(",", hashed)
        };

        var uploadData = await apiClient.PostAsync<UploadFileData>(
            "dmp/custom_audience/file/upload/", uploadBody, cancellationToken);

        // 3) Create the audience referencing the uploaded file token.
        var createBody = new CreateAudienceBody
        {
            AdvertiserId = advertiserId,
            CustomAudienceName = audience.AudienceName,
            FilePaths = uploadData.FilePath is null ? [] : [uploadData.FilePath],
            CalculateType = calcType
        };

        var createData = await apiClient.PostAsync<CreateAudienceData>(
            "dmp/custom_audience/create/", createBody, cancellationToken);

        audience.AudienceId = createData.CustomAudienceId;
        return audience;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TikTokAudience>> ListAsync(
        string advertiserId,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?> { ["advertiser_id"] = advertiserId };
        var data = await apiClient.GetAsync<ListAudienceData>(
            "dmp/custom_audience/list/", query, cancellationToken);

        return (data.List ?? [])
            .Select(a => new TikTokAudience
            {
                AudienceId = a.CustomAudienceId,
                AudienceName = a.Name ?? string.Empty
            })
            .ToList();
    }

    /// <summary>
    /// Normalizes a raw identifier per its type and returns its lowercase hex SHA-256 hash.
    /// </summary>
    private static string HashIdentifier(string value, TikTokAudienceIdType idType)
    {
        // Normalization rules differ per identifier type to maximize match rates.
        var normalized = idType switch
        {
            TikTokAudienceIdType.Email => value.Trim().ToLowerInvariant(),
            TikTokAudienceIdType.MobileAdvertiserId => value.Trim().ToLowerInvariant(),
            // Phone numbers are expected in E.164 (e.g., +14155550100); only trim whitespace.
            TikTokAudienceIdType.PhoneNumber => value.Trim(),
            _ => value.Trim()
        };

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Maps the identifier type to TikTok's <c>calculate_type</c> value.</summary>
    private static string MapCalculateType(TikTokAudienceIdType idType) => idType switch
    {
        TikTokAudienceIdType.Email => "EMAIL_SHA256",
        TikTokAudienceIdType.PhoneNumber => "PHONE_SHA256",
        TikTokAudienceIdType.MobileAdvertiserId => "IDFA_SHA256",
        _ => "EMAIL_SHA256"
    };

    /// <summary>Maps the identifier type to TikTok's <c>id_schema</c> hint.</summary>
    private static string MapIdSchema(TikTokAudienceIdType idType) => idType switch
    {
        TikTokAudienceIdType.Email => "EMAIL_SHA256",
        TikTokAudienceIdType.PhoneNumber => "PHONE_SHA256",
        TikTokAudienceIdType.MobileAdvertiserId => "IDFA_SHA256",
        _ => "EMAIL_SHA256"
    };

    // --- Internal payload shapes. ---

    private sealed class UploadFileBody
    {
        [JsonPropertyName("advertiser_id")] public required string AdvertiserId { get; set; }
        [JsonPropertyName("calculate_type")] public required string CalculateType { get; set; }
        [JsonPropertyName("file_name")] public required string FileName { get; set; }
        [JsonPropertyName("id_schema")] public required string IdSchema { get; set; }
        [JsonPropertyName("data")] public required string Data { get; set; }
    }

    private sealed class UploadFileData
    {
        [JsonPropertyName("file_path")] public string? FilePath { get; set; }
    }

    private sealed class CreateAudienceBody
    {
        [JsonPropertyName("advertiser_id")] public required string AdvertiserId { get; set; }
        [JsonPropertyName("custom_audience_name")] public required string CustomAudienceName { get; set; }
        [JsonPropertyName("file_paths")] public required List<string> FilePaths { get; set; }
        [JsonPropertyName("calculate_type")] public required string CalculateType { get; set; }
    }

    private sealed class CreateAudienceData
    {
        [JsonPropertyName("custom_audience_id")] public string? CustomAudienceId { get; set; }
    }

    private sealed class ListAudienceData
    {
        [JsonPropertyName("list")] public List<AudienceEntry>? List { get; set; }
    }

    private sealed class AudienceEntry
    {
        [JsonPropertyName("custom_audience_id")] public string? CustomAudienceId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
