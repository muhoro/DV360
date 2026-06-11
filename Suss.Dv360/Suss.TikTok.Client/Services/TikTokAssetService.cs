using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Suss.TikTok.Client.Auth;
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
/// <param name="httpClientFactory">Creates HTTP clients used to download remote campaign assets.</param>
/// <param name="logger">Logger for upload diagnostics.</param>
internal sealed class TikTokAssetService(
    ITikTokApiClient apiClient,
    IHttpClientFactory httpClientFactory,
    ILogger<TikTokAssetService> logger) : ITikTokAssetService
{
    /// <inheritdoc />
    public async Task<TikTokAsset> UploadAsync(
        string advertiserId,
        TikTokAsset asset,
        CancellationToken cancellationToken = default)
        => await UploadAsync(null, advertiserId, asset, cancellationToken);

    /// <inheritdoc />
    public async Task<TikTokAsset> UploadAsync(
        TikTokExecutionContext executionContext,
        TikTokAsset asset,
        CancellationToken cancellationToken = default)
        => await UploadAsync(executionContext, executionContext.AdvertiserId, asset, cancellationToken);

    private async Task<TikTokAsset> UploadAsync(
        TikTokExecutionContext? executionContext,
        string advertiserId,
        TikTokAsset asset,
        CancellationToken cancellationToken)
    {
        // Resolve the file bytes from in-memory content, a remote URL, or a local file path.
        var source = await ReadAssetSourceAsync(asset, cancellationToken);
        var bytes = source.Bytes;
        var fileName = asset.FileName
            ?? source.FileName
            ?? throw new InvalidOperationException("A FileName, AssetUrl, FilePath, or Content source name is required to upload an asset.");

        // TikTok validates uploads against an MD5 signature of the raw bytes.
        var signature = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();

        logger.LogInformation(
            "Uploading {AssetType} '{FileName}' ({Bytes} bytes) to advertiser {AdvertiserId}.",
            asset.AssetType, fileName, bytes.Length, advertiserId);

        return asset.AssetType switch
        {
            TikTokAssetType.Image => await UploadImageAsync(executionContext, advertiserId, asset, bytes, fileName, signature, cancellationToken),
            TikTokAssetType.Video => await UploadVideoAsync(executionContext, advertiserId, asset, bytes, fileName, signature, cancellationToken),
            TikTokAssetType.Audio => await UploadAudioAsync(executionContext, advertiserId, asset, bytes, fileName, signature, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported asset type '{asset.AssetType}'.")
        };
    }

    private async Task<TikTokAsset> UploadImageAsync(
        TikTokExecutionContext? executionContext, string advertiserId, TikTokAsset asset, byte[] bytes, string fileName, string signature, CancellationToken ct)
    {
        using var content = BuildMultipart(advertiserId, bytes, fileName, signature, asset.DisplayName, "image_file", "image_signature");
        var data = executionContext is null
            ? await apiClient.PostMultipartAsync<ImageUploadData>("file/image/ad/upload/", content, ct)
            : await apiClient.PostMultipartAsync<ImageUploadData>(executionContext, "file/image/ad/upload/", content, ct);

        asset.ImageId = data.ImageId;
        asset.PreviewUrl = data.Url;
        return asset;
    }

    private async Task<TikTokAsset> UploadVideoAsync(
        TikTokExecutionContext? executionContext, string advertiserId, TikTokAsset asset, byte[] bytes, string fileName, string signature, CancellationToken ct)
    {
        using var content = BuildMultipart(advertiserId, bytes, fileName, signature, asset.DisplayName, "video_file", "video_signature");
        var data = executionContext is null
            ? await apiClient.PostMultipartAsync<JsonElement>("file/video/ad/upload/", content, ct)
            : await apiClient.PostMultipartAsync<JsonElement>(executionContext, "file/video/ad/upload/", content, ct);

        var first = ParseVideoUploadData(data)
            ?? throw new TikTokApiException("Video upload succeeded but returned no video data.");
        asset.VideoId = first.VideoId;
        asset.PreviewUrl = first.PreviewUrl;
        return asset;
    }

    private async Task<TikTokAsset> UploadAudioAsync(
        TikTokExecutionContext? executionContext, string advertiserId, TikTokAsset asset, byte[] bytes, string fileName, string signature, CancellationToken ct)
    {
        using var content = BuildMultipart(advertiserId, bytes, fileName, signature, asset.DisplayName, "audio_file", "audio_signature");
        var data = executionContext is null
            ? await apiClient.PostMultipartAsync<AudioUploadData>("file/audio/ad/upload/", content, ct)
            : await apiClient.PostMultipartAsync<AudioUploadData>(executionContext, "file/audio/ad/upload/", content, ct);

        asset.AudioId = data.AudioId;
        return asset;
    }

    /// <summary>
    /// Builds the common multipart/form-data payload shared by all upload endpoints.
    /// </summary>
    private static MultipartFormDataContent BuildMultipart(
        string advertiserId,
        byte[] bytes,
        string fileName,
        string signature,
        string? displayName,
        string fileFieldName,
        string signatureFieldName)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(advertiserId), "advertiser_id" },
            // "UPLOAD_BY_FILE" tells TikTok the binary is in this request (vs. by URL/blob id).
            { new StringContent("UPLOAD_BY_FILE"), "upload_type" },
            { new StringContent(signature), signatureFieldName }
        };

        if (!string.IsNullOrWhiteSpace(displayName))
            content.Add(new StringContent(displayName), "file_name");

        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, fileFieldName, fileName);

        return content;
    }

    /// <summary>Reads asset bytes from in-memory content, a remote URL, or the configured file path.</summary>
    private async Task<AssetSource> ReadAssetSourceAsync(TikTokAsset asset, CancellationToken ct)
    {
        if (asset.Content is { Length: > 0 })
            return new AssetSource(asset.Content, asset.FileName);

        var url = GetAssetUrl(asset);
        if (url is not null)
        {
            var httpClient = httpClientFactory.CreateClient(nameof(TikTokAssetService));
            var bytes = await httpClient.GetByteArrayAsync(url, ct);
            return new AssetSource(bytes, GetFileNameFromUrl(url));
        }

        if (string.IsNullOrWhiteSpace(asset.FilePath))
            throw new InvalidOperationException("Content, AssetUrl, or FilePath must be set to upload an asset.");

        if (!File.Exists(asset.FilePath))
            throw new InvalidOperationException($"Asset file not found at path '{asset.FilePath}'.");

        return new AssetSource(await File.ReadAllBytesAsync(asset.FilePath, ct), Path.GetFileName(asset.FilePath));
    }

    private static Uri? GetAssetUrl(TikTokAsset asset)
    {
        var candidate = !string.IsNullOrWhiteSpace(asset.AssetUrl)
            ? asset.AssetUrl
            : asset.FilePath;

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;
    }

    private static string? GetFileNameFromUrl(Uri url)
    {
        var fileName = Path.GetFileName(url.LocalPath);
        return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
    }

    private static VideoUploadData? ParseVideoUploadData(JsonElement data)
    {
        return data.ValueKind switch
        {
            JsonValueKind.Object => ReadVideoUploadObject(data),
            JsonValueKind.Array => data.EnumerateArray().Select(ReadVideoUploadObject).FirstOrDefault(video => video is not null),
            _ => null
        };
    }

    private static VideoUploadData? ReadVideoUploadObject(JsonElement data)
    {
        var videoId = GetStringProperty(data, "video_id");
        var previewUrl = GetStringProperty(data, "preview_url");

        if (!string.IsNullOrWhiteSpace(videoId))
            return new VideoUploadData { VideoId = videoId, PreviewUrl = previewUrl };

        if (data.TryGetProperty("video_info", out var videoInfo) && videoInfo.ValueKind == JsonValueKind.Array)
            return videoInfo.EnumerateArray().Select(ReadVideoUploadObject).FirstOrDefault(video => video is not null);

        return null;
    }

    private static string? GetStringProperty(JsonElement data, string propertyName)
    {
        return data.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
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

    private sealed record AssetSource(byte[] Bytes, string? FileName);
}
