using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Media asset upload.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/assets")]
[Produces("application/json")]
public sealed class AssetController : ControllerBase
{
    private readonly IAssetService _assets;

    public AssetController(IAssetService assets) => _assets = assets;

    /// <summary>
    /// Upload a media asset to DV360. Set <c>Url</c> and <c>MimeType</c> on the body to stream
    /// the file from a remote URL directly into DV360 via resumable upload.
    /// On success the response contains <c>MediaId</c> and <c>Content</c> (asset URL) populated.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="asset">Asset descriptor with <c>Url</c> and <c>MimeType</c> set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Dv360CreativeAsset), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        long advertiserId,
        [FromBody] Dv360CreativeAsset asset,
        CancellationToken cancellationToken)
    {
        var uploaded = await _assets.UploadAsync(advertiserId, asset, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, uploaded);
    }
}
