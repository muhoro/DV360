using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Creative management.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/creatives")]
[Produces("application/json")]
public sealed class CreativeController : ControllerBase
{
    private readonly ICreativeService _creatives;

    public CreativeController(ICreativeService creatives) => _creatives = creatives;

    /// <summary>List all creatives for the advertiser.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Dv360Creative>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(long advertiserId, CancellationToken cancellationToken)
    {
        var results = await _creatives.ListAsync(advertiserId, cancellationToken);
        return Ok(results);
    }

    /// <summary>Get a single creative by ID.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="creativeId">DV360 creative ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{creativeId:long}")]
    [ProducesResponseType(typeof(Dv360Creative), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        long advertiserId,
        long creativeId,
        CancellationToken cancellationToken)
    {
        var creative = await _creatives.GetAsync(advertiserId, creativeId, cancellationToken);
        return creative is null ? NotFound() : Ok(creative);
    }

    /// <summary>
    /// Create a new creative. If <c>Assets[].Url</c> is set, the asset is downloaded from the URL
    /// and uploaded to DV360 automatically before the creative record is created.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="creative">Creative definition including assets and exit events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Dv360Creative), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        long advertiserId,
        [FromBody] Dv360Creative creative,
        CancellationToken cancellationToken)
    {
        var created = await _creatives.CreateAsync(advertiserId, creative, cancellationToken);
        return CreatedAtAction(
            nameof(Get),
            new { 
                advertiserId, 
                creativeId = created.CreativeId 
                },
            created);
    }
}
