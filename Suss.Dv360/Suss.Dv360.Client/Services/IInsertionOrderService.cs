using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Services;

/// <summary>
/// Provides CRUD operations for Display &amp; Video 360 insertion orders (IOs).
/// <para>
/// Insertion orders define the budget, flight dates, and pacing for ad delivery.
/// This interface exposes a simplified model with a single budget segment; the
/// internal implementation maps to/from the Google SDK’s multi-segment structure.
/// </para>
/// </summary>
public interface IInsertionOrderService
{
    /// <summary>
    /// Creates a new insertion order under the specified advertiser.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the insertion order.</param>
    /// <param name="insertionOrder">
    /// The insertion order definition. <see cref="Dv360InsertionOrder.InsertionOrderId"/> is populated on return.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The same <paramref name="insertionOrder"/> instance with <see cref="Dv360InsertionOrder.InsertionOrderId"/> set.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<Dv360InsertionOrder> CreateAsync(long advertiserId, Dv360InsertionOrder insertionOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single insertion order by its identifier.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID that owns the insertion order.</param>
    /// <param name="insertionOrderId">The insertion order identifier to retrieve.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The insertion order if found; otherwise <c>null</c>.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns a non-404 error.</exception>
    Task<Dv360InsertionOrder?> GetAsync(long advertiserId, long insertionOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all insertion orders under the specified advertiser, handling pagination automatically.
    /// </summary>
    /// <param name="advertiserId">The DV360 advertiser ID whose insertion orders to list.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only collection of all insertion orders for the advertiser.</returns>
    /// <exception cref="Exceptions.Dv360ApiException">Thrown when the DV360 API returns an error.</exception>
    Task<IReadOnlyList<Dv360InsertionOrder>> ListAsync(long advertiserId, CancellationToken cancellationToken = default);
}
