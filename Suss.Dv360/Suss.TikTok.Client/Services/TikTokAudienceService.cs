using System.Security.Cryptography;
using System.Net.Http.Headers;
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
/// id via the list methods.
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
        => await CreateFromFileAsync(null, advertiserId, audience, cancellationToken);

    /// <inheritdoc />
    public async Task<TikTokAudience> CreateFromFileAsync(
        TikTokExecutionContext executionContext,
        TikTokAudience audience,
        CancellationToken cancellationToken = default)
        => await CreateFromFileAsync(executionContext, executionContext.AdvertiserId, audience, cancellationToken);

    private async Task<TikTokAudience> CreateFromFileAsync(
        TikTokExecutionContext? executionContext,
        string advertiserId,
        TikTokAudience audience,
        CancellationToken cancellationToken)
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
        using var uploadBody = BuildUploadFileContent(
            advertiserId,
            calcType,
            $"{audience.AudienceName}.csv",
            MapIdSchema(audience.File.IdType),
            string.Join(Environment.NewLine, hashed));
        var uploadData = executionContext is null
            ? await apiClient.PostMultipartAsync<UploadFileData>(
                "dmp/custom_audience/file/upload/", uploadBody, cancellationToken)
            : await apiClient.PostMultipartAsync<UploadFileData>(
                executionContext, "dmp/custom_audience/file/upload/", uploadBody, cancellationToken);

        // 3) Create the audience referencing the uploaded file token.
        var createBody = new CreateAudienceBody
        {
            AdvertiserId = advertiserId,
            CustomAudienceName = audience.AudienceName,
            FilePaths = uploadData.FilePath is null ? [] : [uploadData.FilePath],
            CalculateType = calcType
        };

        var createData = executionContext is null
            ? await apiClient.PostAsync<CreateAudienceData>(
                "dmp/custom_audience/create/", createBody, cancellationToken)
            : await apiClient.PostAsync<CreateAudienceData>(
                executionContext, "dmp/custom_audience/create/", createBody, cancellationToken);

        audience.AudienceId = createData.CustomAudienceId;
        return audience;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TikTokAudience>> ListAsync(
        string advertiserId,
        CancellationToken cancellationToken = default)
        => await ListAsync(null, advertiserId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TikTokAudience>> ListAsync(
        TikTokExecutionContext executionContext,
        CancellationToken cancellationToken = default)
        => await ListAsync(executionContext, executionContext.AdvertiserId, cancellationToken);

    private async Task<IReadOnlyList<TikTokAudience>> ListAsync(
        TikTokExecutionContext? executionContext,
        string advertiserId,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?> { ["advertiser_id"] = advertiserId };
        var data = executionContext is null
            ? await apiClient.GetAsync<ListAudienceData>(
                "dmp/custom_audience/list/", query, cancellationToken)
            : await apiClient.GetAsync<ListAudienceData>(
                executionContext, "dmp/custom_audience/list/", query, cancellationToken);

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

    private static MultipartFormDataContent BuildUploadFileContent(
        string advertiserId,
        string calculateType,
        string fileName,
        string idSchema,
        string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var signature = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();

        var content = new MultipartFormDataContent
        {
            { new StringContent(advertiserId), "advertiser_id" },
            { new StringContent(calculateType), "calculate_type" },
            { new StringContent(fileName), "file_name" },
            { new StringContent(idSchema), "id_schema" },
            { new StringContent(signature), "file_signature" }
        };

        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);

        return content;
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
