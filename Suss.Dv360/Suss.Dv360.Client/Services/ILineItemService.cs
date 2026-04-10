using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides CRUD operations for Display &amp; Video 360 line items and creative assignment.
/// <para>
/// Line items sit under an insertion order and define the targeting, creative delivery,
/// and budget allocation for ad serving. This interface also provides a method to
/// link creatives to line items.
/// </para>
/// </summary>
public interface ILineItemService
{
    /// <summary>
    /// Creates a new line item under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the line item.</param>
    /// <param name="lineItem">The line item definition. <see cref="Dv360LineItem.LineItemId"/> is populated on return.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="lineItem"/> instance with <see cref="Dv360LineItem.LineItemId"/> set.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<Dv360LineItem> CreateAsync(long advertiserId, Dv360LineItem lineItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single line item by its identifier.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the line item.</param>
    /// <param name="lineItemId">The line item identifier to retrieve.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The line item if found; otherwise <c>null</c>.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns a non-404 error.</exception>
    Task<Dv360LineItem?> GetAsync(long advertiserId, long lineItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all line items under the specified advertiser, handling pagination automatically.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID whose line items to list.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only collection of all line items for the advertiser.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<IReadOnlyList<Dv360LineItem>> ListAsync(long advertiserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a creative to a line item so the creative is served when the line item’s
    /// targeting criteria are met.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns both resources.</param>
    /// <param name="lineItemId">The line item to assign the creative to.</param>
    /// <param name="creativeId">The creative to assign.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task AssignCreativeAsync(long advertiserId, long lineItemId, long creativeId, CancellationToken cancellationToken = default);
}
