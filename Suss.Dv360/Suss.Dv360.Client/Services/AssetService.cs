using Google;
using Google.Apis.Upload;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Exceptions;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Models;
using GoogleData = Google.Apis.DisplayVideo.v3.Data;

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
    /// <inheritdoc />
    public async Task<Dv360CreativeAsset> UploadAsync(long advertiserId, Dv360CreativeAsset asset, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(asset.FilePath))
            throw new InvalidOperationException("FilePath must be set on the asset before uploading.");

        if (string.IsNullOrWhiteSpace(asset.MimeType))
            throw new InvalidOperationException("MimeType must be set on the asset before uploading.");

        // Derive the filename from the file path if not explicitly provided.
        var filename = asset.Filename ?? Path.GetFileName(asset.FilePath);
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
}
