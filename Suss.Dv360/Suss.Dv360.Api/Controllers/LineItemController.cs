using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Api.Models;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Line item management and creative assignment.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/line-items")]
[Produces("application/json")]
public sealed class LineItemController : ControllerBase
{
    private readonly ILineItemService _lineItems;

    public LineItemController(ILineItemService lineItems) => _lineItems = lineItems;

    /// <summary>List all line items for the advertiser, with optional filtering and sorting.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="query">Optional filter/sort/page parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Dv360LineItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        long advertiserId,
        [FromQuery] ListLineItemsRequest query,
        CancellationToken cancellationToken)
    {
        if (query.Filter is null && query.OrderBy is null && query.PageSize is null)
        {
            var all = await _lineItems.ListAsync(advertiserId, cancellationToken);
            return Ok(all);
        }

        var options = new LineItemListOptions
        {
            Filter   = query.Filter,
            OrderBy  = query.OrderBy,
            PageSize = query.PageSize
        };
        var filtered = await _lineItems.ListAsync(advertiserId, options, cancellationToken);
        return Ok(filtered);
    }

    /// <summary>Get a single line item by ID.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="lineItemId">DV360 line item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{lineItemId:long}")]
    [ProducesResponseType(typeof(Dv360LineItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        long advertiserId,
        long lineItemId,
        CancellationToken cancellationToken)
    {
        var li = await _lineItems.GetAsync(advertiserId, lineItemId, cancellationToken);
        return li is null ? NotFound() : Ok(li);
    }

    /// <summary>Create a new line item.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="lineItem">Line item definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Dv360LineItem), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        long advertiserId,
        [FromBody] Dv360LineItem lineItem,
        CancellationToken cancellationToken)
    {
        var created = await _lineItems.CreateAsync(advertiserId, lineItem, cancellationToken);
        return CreatedAtAction(
            nameof(Get),
            new { advertiserId, lineItemId = created.LineItemId },
            created);
    }

    /// <summary>
    /// Partially update a line item. Only fields present in the body are updated (DV360 PATCH semantics).
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="lineItemId">DV360 line item ID.</param>
    /// <param name="lineItem">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{lineItemId:long}")]
    [ProducesResponseType(typeof(Dv360LineItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(
        long advertiserId,
        long lineItemId,
        [FromBody] Dv360LineItem lineItem,
        CancellationToken cancellationToken)
    {
        var updated = await _lineItems.PatchAsync(advertiserId, lineItemId, lineItem, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Delete a line item.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="lineItemId">DV360 line item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{lineItemId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        long advertiserId,
        long lineItemId,
        CancellationToken cancellationToken)
    {
        await _lineItems.DeleteAsync(advertiserId, lineItemId, cancellationToken);
        return NoContent();
    }

    /// <summary>Assign a creative to a line item.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="lineItemId">DV360 line item ID.</param>
    /// <param name="body">Body containing the creative ID to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{lineItemId:long}/creatives")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignCreative(
        long advertiserId,
        long lineItemId,
        [FromBody] AssignCreativeRequest body,
        CancellationToken cancellationToken)
    {
        await _lineItems.AssignCreativeAsync(advertiserId, lineItemId, body.CreativeId, cancellationToken);
        return NoContent();
    }
}
