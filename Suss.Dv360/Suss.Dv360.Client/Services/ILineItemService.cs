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
    /// Lists line items under the specified advertiser with server-side filtering, sorting,
    /// and page size control. Handles pagination automatically.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID whose line items to list.</param>
    /// <param name="options">
    /// Options controlling filtering, sorting, and page size. Pass <c>null</c> for API defaults.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only collection of matching line items for the advertiser.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<IReadOnlyList<Dv360LineItem>> ListAsync(long advertiserId, LineItemListOptions? options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing line item under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the line item.</param>
    /// <param name="lineItemId">The line item identifier to update.</param>
    /// <param name="lineItem">The updated line item definition.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated <paramref name="lineItem"/> instance with <see cref="Dv360LineItem.LineItemId"/> set.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<Dv360LineItem> PatchAsync(long advertiserId, long lineItemId, Dv360LineItem lineItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a line item.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the line item.</param>
    /// <param name="lineItemId">The line item identifier to delete.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task DeleteAsync(long advertiserId, long lineItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a creative to a line item so the creative is served when the line item's
    /// targeting criteria are met.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns both resources.</param>
    /// <param name="lineItemId">The line item to assign the creative to.</param>
    /// <param name="creativeId">The creative to assign.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task AssignCreativeAsync(long advertiserId, long lineItemId, long creativeId, CancellationToken cancellationToken = default);
}
