using System.Text;
using Google;
using Google.Apis.Upload;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using GoogleData = Google.Apis.DisplayVideo.v4.Data;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Internal implementation of <see cref="IAssetService"/> that uploads media assets to DV360
/// via the Google Display &amp; Video 360 SDK's <c>advertisers.assets.upload</c> endpoint.
/// <para>
/// Uses the Google SDK's resumable media upload to stream the asset file to DV360.
/// The returned <see cref="GoogleData.CreateAssetResponse"/> contains the server-assigned
/// <c>MediaId</c> that the creative uses in its <c>AssetAssociation</c> list.
/// </para>
/// </summary>
/// <param name="serviceFactory">Factory that provides an authenticated <c>DisplayVideoService</c> instance.</param>
/// <param name="logger">Logger for structured diagnostic output.</param>
internal sealed class AssetService(
    IDisplayVideoServiceFactory serviceFactory,
    ILogger<AssetService> logger) : IAssetService
{
    /// <summary>
    /// Maximum filename length in UTF-8 bytes as required by the DV360 API.
    /// </summary>
    private const int MaxFilenameBytes = 240;

    /// <summary>
    /// Maximum file size for image assets (10 MB).
    /// </summary>
    private const long MaxImageSizeBytes = 10L * 1024 * 1024;

    /// <summary>
    /// Maximum file size for ZIP/HTML5 bundle assets (200 MB).
    /// </summary>
    private const long MaxZipSizeBytes = 200L * 1024 * 1024;

    /// <summary>
    /// Maximum file size for video assets (1 GB).
    /// </summary>
    private const long MaxVideoSizeBytes = 1L * 1024 * 1024 * 1024;

    /// <inheritdoc />
    public async Task<Dv360CreativeAsset> UploadAsync(long advertiserId, Dv360CreativeAsset asset, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(asset.FilePath))
            throw new InvalidOperationException("FilePath must be set on the asset before uploading.");

        if (string.IsNullOrWhiteSpace(asset.MimeType))
            throw new InvalidOperationException("MimeType must be set on the asset before uploading.");

        // Derive the filename from the file path if not explicitly provided.
        var filename = asset.Filename ?? Path.GetFileName(asset.FilePath);

        // The DV360 API requires the filename to be UTF-8 encoded with a maximum size of 240 bytes.
        ValidateFilename(filename);

        // Validate the file exists and its size is within the DV360 API limits for the given MIME type.
        ValidateFileSize(asset.FilePath, asset.MimeType);

        logger.LogInformation("Uploading asset '{Filename}' ({MimeType}) for advertiser {AdvertiserId}",
            filename, asset.MimeType, advertiserId);

        try
        {
            var service = await serviceFactory.CreateAsync(cancellationToken);

            var body = new GoogleData.CreateAssetRequest
            {
                Filename = filename
            };

            // Open the file and stream it to DV360 via the resumable upload endpoint.
            await using var fileStream = new FileStream(asset.FilePath, FileMode.Open, FileAccess.Read);
            var uploadRequest = service.Advertisers.Assets.Upload(body, advertiserId, fileStream, asset.MimeType);
            var uploadResult = await uploadRequest.UploadAsync(cancellationToken);

            if (uploadResult.Status != UploadStatus.Completed)
            {
                var errorMessage = $"Asset upload failed with status {uploadResult.Status}.";
                logger.LogError(uploadResult.Exception, errorMessage);
                throw new Dv360ApiException(errorMessage, uploadResult.Exception ?? new Exception(errorMessage));
            }

            // Extract the server-assigned media ID and content URL from the response.
            var response = uploadRequest.ResponseBody;
            asset.MediaId = response.Asset?.MediaId;
            asset.Content = response.Asset?.Content;

            logger.LogInformation("Uploaded asset '{Filename}' with MediaId {MediaId} for advertiser {AdvertiserId}",
                filename, asset.MediaId, advertiserId);

            return asset;
        }
        catch (GoogleApiException ex)
        {
            logger.LogError(ex, "Failed to upload asset '{Filename}' for advertiser {AdvertiserId}", filename, advertiserId);
            throw new Dv360ApiException($"Failed to upload asset '{filename}' for advertiser {advertiserId}.", ex);
        }
    }

    /// <summary>
    /// Validates that the filename is a non-empty, UTF-8 encoded string with a maximum
    /// size of <see cref="MaxFilenameBytes"/> bytes, as required by the DV360 API.
    /// </summary>
    /// <param name="filename">The filename to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the filename is empty or exceeds the 240-byte UTF-8 limit.
    /// </exception>
    private static void ValidateFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new InvalidOperationException("Filename must not be empty.");

        var byteCount = Encoding.UTF8.GetByteCount(filename);
        if (byteCount > MaxFilenameBytes)
            throw new InvalidOperationException(
                $"Filename '{filename}' is {byteCount} bytes in UTF-8, which exceeds the DV360 API limit of {MaxFilenameBytes} bytes.");
    }

    /// <summary>
    /// Validates that the asset file exists and its size does not exceed the DV360 API
    /// limits based on MIME type: 10 MB for images, 200 MB for ZIP files, and 1 GB for videos.
    /// </summary>
    /// <param name="filePath">The local file path of the asset.</param>
    /// <param name="mimeType">The MIME type of the asset.</param>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist at the given path.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file size exceeds the DV360 API limit for the given type.</exception>
    private static void ValidateFileSize(string filePath, string mimeType)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException($"Asset file not found: '{filePath}'.", filePath);

        var fileSize = fileInfo.Length;
        var (maxSize, typeName) = GetFileSizeLimit(mimeType);

        if (maxSize.HasValue && fileSize > maxSize.Value)
            throw new InvalidOperationException(
                $"Asset file '{filePath}' is {fileSize:N0} bytes, which exceeds the DV360 API limit of {maxSize.Value:N0} bytes for {typeName} assets.");
    }

    /// <summary>
    /// Returns the maximum allowed file size and a human-readable type name
    /// for the given MIME type, based on DV360 API documentation.
    /// </summary>
    private static (long? MaxSize, string TypeName) GetFileSizeLimit(string mimeType)
    {
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return (MaxImageSizeBytes, "image");

        if (mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            return (MaxVideoSizeBytes, "video");

        if (mimeType.Equals("application/zip", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/x-zip-compressed", StringComparison.OrdinalIgnoreCase))
            return (MaxZipSizeBytes, "ZIP");

        // Unknown MIME types are not validated for size — the API will reject them if needed.
        return (null, "unknown");
    }
}
