namespace Suss.Dv360.Client.Models;

/// <summary>
/// Input model for the Phase 1 campaign creation workflow.
/// <para>
/// Bundles all the resources needed to execute the full workflow:
/// upload creatives → create campaign → create insertion order + line items → link creatives.
/// The workflow automatically wires parent identifiers (campaign ID, IO ID) across
/// child resources after each step.
/// </para>
/// </summary>
public sealed class CampaignWorkflowRequest
{
    /// <summary>
    /// The DV360 advertiser identifier under which all resources will be created.
    /// </summary>
    public required long AdvertiserId { get; set; }

    /// <summary>
    /// The campaign to create. Its <see cref="Dv360Campaign.CampaignId"/> will be populated after creation.
    /// </summary>
    public required Dv360Campaign Campaign { get; set; }

    /// <summary>
    /// One or more creatives to upload. Each creative’s <see cref="Dv360Creative.CreativeId"/>
    /// will be populated after creation and then linked to every line item in Step 4.
    /// </summary>
    public required List<Dv360Creative> Creatives { get; set; }

    /// <summary>
    /// The insertion order to create under the campaign. Its <see cref="Dv360InsertionOrder.CampaignId"/>
    /// is set automatically by the workflow.
    /// </summary>
    public required Dv360InsertionOrder InsertionOrder { get; set; }

    /// <summary>
    /// One or more line items to create under the insertion order. Each line item’s
    /// <see cref="Dv360LineItem.CampaignId"/> and <see cref="Dv360LineItem.InsertionOrderId"/>
    /// are set automatically by the workflow.
    /// </summary>
    public required List<Dv360LineItem> LineItems { get; set; }
}
