using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Api.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Line item targeting assignment.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/line-items/{lineItemId:long}/targeting")]
[Produces("application/json")]
public sealed class TargetingController : ControllerBase
{
    private readonly ITargetingService _targeting;

    public TargetingController(ITargetingService targeting) => _targeting = targeting;

    /// <summary>
    /// Assign targeting options to a line item. Each non-null, non-empty list in the
    /// targeting body results in one or more <c>AssignedTargetingOption</c> resources
    /// being created in DV360 (geo, device type, browser, channel, content labels,
    /// instream position, viewability).
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="lineItemId">DV360 line item ID.</param>
    /// <param name="body">Targeting configuration to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        long advertiserId,
        long lineItemId,
        [FromBody] AssignTargetingRequest body,
        CancellationToken cancellationToken)
    {
        await _targeting.AssignTargetingAsync(advertiserId, lineItemId, body.Targeting, cancellationToken);
        return NoContent();
    }
}
