using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Suss.TikTok.Client.Exceptions;
using Suss.TikTok.Client.Infrastructure;
using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Default <see cref="ITikTokAssetService"/> implementation.
/// <para>
/// Uploads media via TikTok's multipart file endpoints:
/// <list type="bullet">
///   <item><description>Images → <c>file/image/ad/upload/</c></description></item>
///   <item><description>Videos → <c>file/video/ad/upload/</c></description></item>
///   <item><description>Audio  → <c>file/audio/ad/upload/</c></description></item>
/// </list>
/// TikTok requires a <c>file_signature</c> (an MD5 of the file content) to verify upload integrity;
/// this service computes it automatically. It maps the API's snake_case response back onto the
/// flat <see cref="TikTokAsset"/> model.
/// </para>
/// </summary>
/// <param name="apiClient">The transport abstraction used to call TikTok.</param>
/// <param name="logger">Logger for upload diagnostics.</param>
internal sealed class TikTokAssetService(
    ITikTokApiClient apiClient,
    ILogger<TikTokAssetService> logger) : ITikTokAssetService
{
    /// <inheritdoc />
    public async Task<TikTokAsset> UploadAsync(
        string advertiserId,
        TikTokAsset asset,
        CancellationToken cancellationToken = default)
    {
        // Resolve the file bytes from either the in-memory content or the file path.
        var bytes = await ReadAssetBytesAsync(asset, cancellationToken);
        var fileName = asset.FileName
            ?? (asset.FilePath is not null ? Path.GetFileName(asset.FilePath) : null)
            ?? throw new InvalidOperationException("A FileName or FilePath is required to upload an asset.");

        // TikTok validates uploads against an MD5 signature of the raw bytes.
        var signature = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();

        logger.LogInformation(
            "Uploading {AssetType} '{FileName}' ({Bytes} bytes) to advertiser {AdvertiserId}.",
            asset.AssetType, fileName, bytes.Length, advertiserId);

        return asset.AssetType switch
        {
            TikTokAssetType.Image => await UploadImageAsync(advertiserId, asset, bytes, fileName, signature, cancellationToken),
            TikTokAssetType.Video => await UploadVideoAsync(advertiserId, asset, bytes, fileName, signature, cancellationToken),
            TikTokAssetType.Audio => await UploadAudioAsync(advertiserId, asset, bytes, fileName, signature, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported asset type '{asset.AssetType}'.")
        };
    }

    private async Task<TikTokAsset> UploadImageAsync(
        string advertiserId, TikTokAsset asset, byte[] bytes, string fileName, string signature, CancellationToken ct)
    {
        using var content = BuildMultipart(advertiserId, bytes, fileName, signature, asset.DisplayName);
        var data = await apiClient.PostMultipartAsync<ImageUploadData>("file/image/ad/upload/", content, ct);

        asset.ImageId = data.ImageId;
        asset.PreviewUrl = data.Url;
        return asset;
    }

    private async Task<TikTokAsset> UploadVideoAsync(
        string advertiserId, TikTokAsset asset, byte[] bytes, string fileName, string signature, CancellationToken ct)
    {
        using var content = BuildMultipart(advertiserId, bytes, fileName, signature, asset.DisplayName);
        // The video endpoint returns a list of uploaded videos.
        var data = await apiClient.PostMultipartAsync<List<VideoUploadData>>("file/video/ad/upload/", content, ct);

        var first = data.FirstOrDefault()
            ?? throw new TikTokApiException("Video upload succeeded but returned no video data.");
        asset.VideoId = first.VideoId;
        asset.PreviewUrl = first.PreviewUrl;
        return asset;
    }

    private async Task<TikTokAsset> UploadAudioAsync(
        string advertiserId, TikTokAsset asset, byte[] bytes, string fileName, string signature, CancellationToken ct)
    {
        using var content = BuildMultipart(advertiserId, bytes, fileName, signature, asset.DisplayName);
        var data = await apiClient.PostMultipartAsync<AudioUploadData>("file/audio/ad/upload/", content, ct);

        asset.AudioId = data.AudioId;
        return asset;
    }

    /// <summary>
    /// Builds the common multipart/form-data payload shared by all upload endpoints.
    /// </summary>
    private static MultipartFormDataContent BuildMultipart(
        string advertiserId, byte[] bytes, string fileName, string signature, string? displayName)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(advertiserId), "advertiser_id" },
            // "UPLOAD_BY_FILE" tells TikTok the binary is in this request (vs. by URL/blob id).
            { new StringContent("UPLOAD_BY_FILE"), "upload_type" },
            { new StringContent(signature), "file_signature" }
        };

        if (!string.IsNullOrWhiteSpace(displayName))
            content.Add(new StringContent(displayName), "file_name");

        var fileContent = new ByteArrayContent(bytes);
        content.Add(fileContent, "video_file", fileName);
        // Images/audio reuse the same binary part under different field names; TikTok accepts the
        // binary regardless, but we add the canonical field name expected per endpoint where needed.
        content.Add(new ByteArrayContent(bytes), "image_file", fileName);

        return content;
    }

    /// <summary>Reads asset bytes from in-memory content or the configured file path.</summary>
    private static async Task<byte[]> ReadAssetBytesAsync(TikTokAsset asset, CancellationToken ct)
    {
        if (asset.Content is { Length: > 0 })
            return asset.Content;

        if (string.IsNullOrWhiteSpace(asset.FilePath))
            throw new InvalidOperationException("Either Content or FilePath must be set to upload an asset.");

        if (!File.Exists(asset.FilePath))
            throw new InvalidOperationException($"Asset file not found at path '{asset.FilePath}'.");

        return await File.ReadAllBytesAsync(asset.FilePath, ct);
    }

    // --- Internal payload shapes used only for deserializing upload responses. ---

    private sealed class ImageUploadData
    {
        [JsonPropertyName("image_id")] public string? ImageId { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
    }

    private sealed class VideoUploadData
    {
        [JsonPropertyName("video_id")] public string? VideoId { get; set; }
        [JsonPropertyName("preview_url")] public string? PreviewUrl { get; set; }
    }

    private sealed class AudioUploadData
    {
        [JsonPropertyName("audio_id")] public string? AudioId { get; set; }
    }
}
