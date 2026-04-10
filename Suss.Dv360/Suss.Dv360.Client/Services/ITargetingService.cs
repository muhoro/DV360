using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Manages assigned targeting options for Display &amp; Video 360 line items.
/// <para>
/// In the DV360 API v4, targeting is applied by creating <c>AssignedTargetingOption</c>
/// resources under a line item for each targeting type (geo, device, browser, etc.).
/// This service abstracts the per-type API calls behind a single method that accepts
/// the library's flat <see cref="Dv360LineItemTargeting"/> model.
/// </para>
/// </summary>
public interface ITargetingService
{
    /// <summary>
    /// Assigns all targeting options defined in the <paramref name="targeting"/> model
    /// to the specified line item.
    /// <para>
    /// Each non-null, non-empty targeting list in the model results in one or more
    /// <c>AssignedTargetingOption</c> API calls. Targeting types that are <c>null</c>
    /// or empty are skipped.
    /// </para>
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the line item.</param>
    /// <param name="lineItemId">The line item to assign targeting to.</param>
    /// <param name="targeting">The targeting parameters to assign.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when any DV360 API call fails.</exception>
    Task AssignTargetingAsync(long advertiserId, long lineItemId,
        Dv360LineItemTargeting targeting, CancellationToken cancellationToken = default);
}
