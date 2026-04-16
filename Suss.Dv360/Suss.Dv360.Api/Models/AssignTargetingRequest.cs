using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Api.Models;

/// <summary>Request body for assigning targeting options to a line item.</summary>
public sealed class AssignTargetingRequest
{
    /// <summary>
    /// Targeting configuration to apply. Each non-null, non-empty list results in
    /// one or more AssignedTargetingOption resources being created in DV360.
    /// </summary>
    public required Dv360LineItemTargeting Targeting { get; set; }
}
