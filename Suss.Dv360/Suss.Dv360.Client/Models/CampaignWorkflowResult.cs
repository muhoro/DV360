namespace Suss.Dv360.Client.Models;

/// <summary>
/// Output model returned by the Phase 1 campaign creation workflow after all resources
/// have been successfully created and linked in DV360.
/// <para>
/// Every object in this result carries its DV360-assigned identifier, confirming
/// server-side creation. Use these IDs for subsequent operations (e.g., updating
/// statuses, pulling reports).
/// </para>
/// </summary>
public sealed class CampaignWorkflowResult
{
    /// <summary>
    /// The created campaign with its DV360-assigned <see cref="Dv360Campaign.CampaignId"/>.
    /// </summary>
    public required Dv360Campaign Campaign { get; set; }

    /// <summary>
    /// All created creatives, each with its DV360-assigned <see cref="Dv360Creative.CreativeId"/>.
    /// </summary>
    public required IReadOnlyList<Dv360Creative> Creatives { get; set; }

    /// <summary>
    /// The created insertion order with its DV360-assigned <see cref="Dv360InsertionOrder.InsertionOrderId"/>.
    /// </summary>
    public required Dv360InsertionOrder InsertionOrder { get; set; }

    /// <summary>
    /// All created line items, each with its DV360-assigned <see cref="Dv360LineItem.LineItemId"/>.
    /// </summary>
    public required IReadOnlyList<Dv360LineItem> LineItems { get; set; }
}
