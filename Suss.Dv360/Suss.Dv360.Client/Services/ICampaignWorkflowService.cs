using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Orchestrates the Phase 1 campaign creation workflow, executing the following steps
/// in sequence:
/// <list type="number">
///   <item><description>Upload all creatives.</description></item>
///   <item><description>Create the campaign.</description></item>
///   <item><description>Create the insertion order and line items under the campaign.</description></item>
///   <item><description>Link every creative to every line item.</description></item>
/// </list>
/// <para>
/// Parent identifiers (campaign ID, insertion order ID) are wired automatically between
/// steps, so callers only need to populate the individual resource definitions.
/// </para>
/// </summary>
public interface ICampaignWorkflowService
{
    /// <summary>
    /// Executes the full campaign creation workflow.
    /// </summary>
    /// <param name="request">The workflow input containing all resource definitions.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A <see cref="CampaignWorkflowResult"/> containing all created resources with their
    /// DV360-assigned identifiers.
    /// </returns>
    /// <exception cref="Exceptions.Dv360ApiException">
    /// Thrown when any DV360 API call within the workflow fails.
    /// </exception>
    Task<CampaignWorkflowResult> ExecuteAsync(CampaignWorkflowRequest request, CancellationToken cancellationToken = default);
}
