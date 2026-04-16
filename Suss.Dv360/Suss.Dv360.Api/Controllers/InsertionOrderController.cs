using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Insertion order management.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/insertion-orders")]
[Produces("application/json")]
public sealed class InsertionOrderController : ControllerBase
{
    private readonly IInsertionOrderService _ios;

    public InsertionOrderController(IInsertionOrderService ios) => _ios = ios;

    /// <summary>List all insertion orders for the advertiser.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Dv360InsertionOrder>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(long advertiserId, CancellationToken cancellationToken)
    {
        var results = await _ios.ListAsync(advertiserId, cancellationToken);
        return Ok(results);
    }

    /// <summary>Get a single insertion order by ID.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="insertionOrderId">DV360 insertion order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{insertionOrderId:long}")]
    [ProducesResponseType(typeof(Dv360InsertionOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        long advertiserId,
        long insertionOrderId,
        CancellationToken cancellationToken)
    {
        var io = await _ios.GetAsync(advertiserId, insertionOrderId, cancellationToken);
        return io is null ? NotFound() : Ok(io);
    }

    /// <summary>Create a new insertion order.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="insertionOrder">Insertion order definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Dv360InsertionOrder), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        long advertiserId,
        [FromBody] Dv360InsertionOrder insertionOrder,
        CancellationToken cancellationToken)
    {
        var created = await _ios.CreateAsync(advertiserId, insertionOrder, cancellationToken);
        return CreatedAtAction(
            nameof(Get),
            new { advertiserId, insertionOrderId = created.InsertionOrderId },
            created);
    }
}
