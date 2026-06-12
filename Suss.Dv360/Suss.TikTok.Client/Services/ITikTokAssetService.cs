using Suss.TikTok.Client.Models;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Provides operations for uploading media assets (images, videos, audio) to TikTok.
/// <para>
/// Assets must be uploaded before they can be referenced by an ad. The upload returns a
/// server-assigned id (<see cref="TikTokAsset.ImageId"/> / <see cref="TikTokAsset.VideoId"/> /
/// <see cref="TikTokAsset.AudioId"/>) that ads use to bind creative content. This service is
/// consumed by <see cref="ITikTokCampaignWorkflowService"/> but is also available standalone.
/// </para>
/// </summary>
public interface ITikTokAssetService
{
    /// <summary>
    /// Uploads a single media asset to TikTok under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The TikTok advertiser id that will own the asset.</param>
    /// <param name="asset">
    /// The asset to upload. Requires <see cref="TikTokAsset.AssetType"/> and one of
    /// <see cref="TikTokAsset.Content"/>, <see cref="TikTokAsset.AssetUrl"/>, or
    /// <see cref="TikTokAsset.FilePath"/>. On return, the matching id field and
    /// <see cref="TikTokAsset.PreviewUrl"/> are populated.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="asset"/> instance with server-assigned fields populated.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when the TikTok API returns an error.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
    Task<TikTokAsset> UploadAsync(string advertiserId, TikTokAsset asset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a single media asset using the supplied TikTok execution context.
    /// </summary>
    Task<TikTokAsset> UploadAsync(
        TikTokExecutionContext executionContext,
        TikTokAsset asset,
        CancellationToken cancellationToken = default);
}
