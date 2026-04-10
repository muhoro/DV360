using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Orchestrates the Phase 1 campaign creation workflow by coordinating the individual
/// resource services in the correct dependency order.
/// <para>
/// Execution flow:
/// <list type="number">
///   <item><description><b>Step 1</b> – Upload all creatives (independent of campaign/IO).</description></item>
///   <item><description><b>Step 2</b> – Create the campaign.</description></item>
///   <item><description><b>Step 3a</b> – Create the insertion order under the campaign.</description></item>
///   <item><description><b>Step 3b</b> – Create all line items under the campaign and IO.</description></item>
///   <item><description><b>Step 3c</b> – Apply targeting options to line items.</description></item>
///   <item><description><b>Step 4</b> – Link every creative to every line item (cross-join).</description></item>
/// </list>
/// Parent identifiers are wired automatically between steps so callers only supply
/// resource definitions without worrying about ID dependencies.
/// </para>
/// </summary>
/// <param name="creativeService">Service for managing DV360 creatives.</param>
/// <param name="campaignService">Service for managing DV360 campaigns.</param>
/// <param name="insertionOrderService">Service for managing DV360 insertion orders.</param>
/// <param name="lineItemService">Service for managing DV360 line items and creative assignments.</param>
/// <param name="targetingService">Service for managing DV360 line item targeting options.</param>
/// <param name="logger">Logger for structured diagnostic output at each workflow step.</param>
internal sealed class CampaignWorkflowService(
    ICreativeService creativeService,
    ICampaignService campaignService,
    IInsertionOrderService insertionOrderService,
    ILineItemService lineItemService,
    ITargetingService targetingService,
    ILogger<CampaignWorkflowService> logger) : ICampaignWorkflowService
{
    /// <inheritdoc />
    public async Task<CampaignWorkflowResult> ExecuteAsync(CampaignWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting campaign creation workflow for advertiser {AdvertiserId}", request.AdvertiserId);

        // Step 1: Upload creatives first – they don’t depend on the campaign or IO.
        logger.LogInformation("Step 1: Creating {Count} creative(s)", request.Creatives.Count);
        var createdCreatives = new List<Dv360Creative>();
        foreach (var creative in request.Creatives)
        {
            var created = await creativeService.CreateAsync(request.AdvertiserId, creative, cancellationToken);
            createdCreatives.Add(created);
        }

        // Step 2: Create the campaign – produces the CampaignId needed by the IO and line items.
        logger.LogInformation("Step 2: Creating campaign '{DisplayName}'", request.Campaign.DisplayName);
        var createdCampaign = await campaignService.CreateAsync(request.AdvertiserId, request.Campaign, cancellationToken);

        // Step 3a: Create the insertion order, wiring the newly created CampaignId.
        request.InsertionOrder.CampaignId = createdCampaign.CampaignId!.Value;
        logger.LogInformation("Step 3a: Creating insertion order '{DisplayName}'", request.InsertionOrder.DisplayName);
        var createdIo = await insertionOrderService.CreateAsync(request.AdvertiserId, request.InsertionOrder, cancellationToken);

        // Step 3b: Create line items, wiring both CampaignId and InsertionOrderId.
        logger.LogInformation("Step 3b: Creating {Count} line item(s)", request.LineItems.Count);
        var createdLineItems = new List<Dv360LineItem>();
        foreach (var lineItem in request.LineItems)
        {
            lineItem.CampaignId = createdCampaign.CampaignId!.Value;
            lineItem.InsertionOrderId = createdIo.InsertionOrderId!.Value;

            var created = await lineItemService.CreateAsync(request.AdvertiserId, lineItem, cancellationToken);
            createdLineItems.Add(created);
        }

        // Step 3c: Assign targeting options to line items that have targeting configured.
        logger.LogInformation("Step 3c: Assigning targeting options to line items");
        foreach (var lineItem in createdLineItems)
        {
            if (lineItem.Targeting is not null)
            {
                await targetingService.AssignTargetingAsync(
                    request.AdvertiserId,
                    lineItem.LineItemId!.Value,
                    lineItem.Targeting,
                    cancellationToken);
            }
        }

        // Step 4: Link every creative to every line item (full cross-join).
        logger.LogInformation("Step 4: Assigning creatives to line items");
        foreach (var lineItem in createdLineItems)
        {
            foreach (var creative in createdCreatives)
            {
                await lineItemService.AssignCreativeAsync(
                    request.AdvertiserId,
                    lineItem.LineItemId!.Value,
                    creative.CreativeId!.Value,
                    cancellationToken);
            }
        }

        logger.LogInformation("Campaign creation workflow completed successfully");

        return new CampaignWorkflowResult
        {
            Campaign = createdCampaign,
            Creatives = createdCreatives,
            InsertionOrder = createdIo,
            LineItems = createdLineItems
        };
    }
}
