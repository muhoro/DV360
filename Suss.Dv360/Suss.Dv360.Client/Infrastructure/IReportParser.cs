using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Infrastructure;

/// <summary>
/// Interface for parsing report files into strongly-typed row data.
/// </summary>
public interface IReportParser
{
    /// <summary>
    /// Parses CSV report content into a collection of report rows.
    /// </summary>
    /// <param name="csvContent">The raw CSV content from the report file.</param>
    /// <param name="groupBy">The primary grouping dimension used in the report.</param>
    /// <returns>A list of parsed report rows.</returns>
    IReadOnlyList<CampaignReportRow> Parse(string csvContent, ReportGrouping groupBy);

    /// <summary>
    /// Parses CSV report content from a stream (for large files).
    /// </summary>
    /// <param name="csvStream">The stream containing CSV data.</param>
    /// <param name="groupBy">The primary grouping dimension used in the report.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async enumerable of parsed report rows.</returns>
    IAsyncEnumerable<CampaignReportRow> ParseStreamAsync(
        Stream csvStream,
        ReportGrouping groupBy,
        CancellationToken cancellationToken = default);
}
