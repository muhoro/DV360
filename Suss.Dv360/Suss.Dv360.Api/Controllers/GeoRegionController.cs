using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Geographic targeting option lookup.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/geo-regions")]
[Produces("application/json")]
public sealed class GeoRegionController : ControllerBase
{
    private readonly IGeoRegionService _geoRegions;

    public GeoRegionController(IGeoRegionService geoRegions) => _geoRegions = geoRegions;

    /// <summary>
    /// Search for geo-region targeting options by partial name match.
    /// Use the returned <c>TargetingOptionId</c> when building <c>Dv360GeoTargeting</c>.
    /// Results are cached in-memory to reduce redundant API calls.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="q">Partial or full geo-region name, e.g. <c>Kenya</c> or <c>United</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Dv360GeoRegion>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        long advertiserId,
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Missing query parameter",
                Detail = "The 'q' query parameter is required."
            });

        var results = await _geoRegions.SearchAsync(advertiserId, q, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Find a geo-region by its exact display name (case-insensitive).
    /// Returns 404 if no region matches.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="name">Exact display name, e.g. <c>United States</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("by-name")]
    [ProducesResponseType(typeof(Dv360GeoRegion), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FindByName(
        long advertiserId,
        [FromQuery] string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Missing query parameter",
                Detail = "The 'name' query parameter is required."
            });

        var region = await _geoRegions.FindByNameAsync(advertiserId, name, cancellationToken);
        return region is null ? NotFound() : Ok(region);
    }
}
