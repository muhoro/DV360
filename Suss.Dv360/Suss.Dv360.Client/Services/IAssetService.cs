using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides operations for uploading media assets (images, videos, HTML5 bundles) to DV360.
/// <para>
/// Assets must be uploaded before they can be referenced by a creative. The upload returns
/// a <see cref="Dv360CreativeAsset.MediaId"/> that is used in <c>AssetAssociation</c> entries
/// on the creative. This service is consumed internally by <see cref="CreativeService"/>
/// during creative creation but is also available for standalone use.
/// </para>
/// </summary>
public interface IAssetService
{
    /// <summary>
    /// Uploads a media asset file to DV360 under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that will own the asset.</param>
    /// <param name="asset">
    /// The asset to upload. Must have <see cref="Dv360CreativeAsset.FilePath"/> and
    /// <see cref="Dv360CreativeAsset.MimeType"/> set. On return, <see cref="Dv360CreativeAsset.MediaId"/>
    /// and <see cref="Dv360CreativeAsset.Content"/> are populated with the server-assigned values.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="asset"/> instance with server-assigned fields populated.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Dv360CreativeAsset.FilePath"/> or <see cref="Dv360CreativeAsset.MimeType"/> is not set.
    /// </exception>
    Task<Dv360CreativeAsset> UploadAsync(long advertiserId, Dv360CreativeAsset asset, CancellationToken cancellationToken = default);
}
