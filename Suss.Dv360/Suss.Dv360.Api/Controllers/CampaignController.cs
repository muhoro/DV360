using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Api.Models;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Campaign management and full workflow orchestration.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/campaigns")]
[Produces("application/json")]
public sealed class CampaignController : ControllerBase
{
    private readonly ICampaignService _campaigns;
    private readonly ICampaignWorkflowService _workflow;

    public CampaignController(ICampaignService campaigns, ICampaignWorkflowService workflow)
    {
        _campaigns = campaigns;
        _workflow  = workflow;
    }

    /// <summary>
    /// Execute the full campaign creation workflow: upload creatives, create campaign,
    /// create insertion order and line items, and link creatives to line items.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="request">Full workflow definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("workflow")]
    [ProducesResponseType(typeof(CampaignWorkflowResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ExecuteWorkflow(
        long advertiserId,
        [FromBody] CampaignWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        // Route param is authoritative — prevent body/route mismatch.
        request.AdvertiserId = advertiserId;
        var result = await _workflow.ExecuteAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>List all campaigns for the advertiser, with optional filtering and sorting.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="query">Optional filter/sort/page parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Dv360Campaign>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        long advertiserId,
        [FromQuery] ListCampaignsRequest query,
        CancellationToken cancellationToken)
    {
        if (query.Filter is null && query.OrderBy is null && query.PageSize is null)
        {
            var campaigns = await _campaigns.ListAsync(advertiserId, cancellationToken);
            return Ok(campaigns);
        }

        var options = new CampaignListOptions
        {
            Filter   = query.Filter,
            OrderBy  = query.OrderBy,
            PageSize = query.PageSize
        };
        var filtered = await _campaigns.ListAsync(advertiserId, options, cancellationToken);
        return Ok(filtered);
    }

    /// <summary>Get a single campaign by ID.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="campaignId">DV360 campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{campaignId:long}")]
    [ProducesResponseType(typeof(Dv360Campaign), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        long advertiserId,
        long campaignId,
        CancellationToken cancellationToken)
    {
        var campaign = await _campaigns.GetAsync(advertiserId, campaignId, cancellationToken);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    /// <summary>Create a new campaign.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="campaign">Campaign definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Dv360Campaign), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        long advertiserId,
        [FromBody] Dv360Campaign campaign,
        CancellationToken cancellationToken)
    {
        var created = await _campaigns.CreateAsync(advertiserId, campaign, cancellationToken);
        return CreatedAtAction(
            nameof(Get),
            new { advertiserId, campaignId = created.CampaignId },
            created);
    }

    /// <summary>
    /// Partially update a campaign. Only fields present in the body are updated (DV360 PATCH semantics).
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="campaignId">DV360 campaign ID.</param>
    /// <param name="campaign">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{campaignId:long}")]
    [ProducesResponseType(typeof(Dv360Campaign), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(
        long advertiserId,
        long campaignId,
        [FromBody] Dv360Campaign campaign,
        CancellationToken cancellationToken)
    {
        var updated = await _campaigns.PatchAsync(advertiserId, campaignId, campaign, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Delete a campaign.</summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="campaignId">DV360 campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{campaignId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        long advertiserId,
        long campaignId,
        CancellationToken cancellationToken)
    {
        await _campaigns.DeleteAsync(advertiserId, campaignId, cancellationToken);
        return NoContent();
    }
}
