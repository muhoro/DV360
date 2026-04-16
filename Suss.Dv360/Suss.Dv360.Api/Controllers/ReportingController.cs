using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Api.Models;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Api.Controllers;

/// <summary>Campaign reporting via the Bid Manager API v2.</summary>
[ApiController]
[Route("api/advertisers/{advertiserId:long}/reports")]
[Produces("application/json")]
public sealed class ReportingController : ControllerBase
{
    private readonly IReportingService _reporting;

    public ReportingController(IReportingService reporting) => _reporting = reporting;

    /// <summary>
    /// Create a Bid Manager query, wait for it to complete, download the CSV from GCS, and
    /// return parsed report rows. This is a synchronous call — it blocks until the report is ready.
    /// For large date ranges, use <c>POST /reports/query</c> instead.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="body">Report query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(CampaignReportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> GetReport(
        long advertiserId,
        [FromBody] CreateReportQueryRequest body,
        CancellationToken cancellationToken)
    {
        var request = BuildReportRequest(advertiserId, body);
        var result  = await _reporting.GetCampaignReportAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create and start a Bid Manager query asynchronously. Returns the query ID immediately
    /// without waiting for the report to complete. Use the returned <c>queryId</c> to check
    /// status in the Bid Manager console or to call <c>POST /reports/download</c> once ready.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID.</param>
    /// <param name="body">Report query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("query")]
    [ProducesResponseType(typeof(CreateQueryResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuery(
        long advertiserId,
        [FromBody] CreateReportQueryRequest body,
        CancellationToken cancellationToken)
    {
        var request = BuildReportRequest(advertiserId, body);
        var queryId = await _reporting.CreateAndRunQueryAsync(request, cancellationToken);
        return Accepted(new CreateQueryResponse { QueryId = queryId });
    }

    /// <summary>
    /// Download and parse a completed report from a GCS URL.
    /// Use when you already have the download URL from a previously completed Bid Manager query.
    /// </summary>
    /// <param name="advertiserId">DV360 advertiser ID (used for context only).</param>
    /// <param name="body">GCS download URL and the grouping dimension used when the report was created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("download")]
    [ProducesResponseType(typeof(CampaignReportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Download(
        long advertiserId,
        [FromBody] DownloadReportRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _reporting.DownloadReportAsync(body.DownloadUrl, body.GroupBy, cancellationToken);
        return Ok(result);
    }

    private static CampaignReportRequest BuildReportRequest(long advertiserId, CreateReportQueryRequest body) =>
        new()
        {
            AdvertiserId         = advertiserId,
            CampaignId           = body.CampaignId,
            LineItemId           = body.LineItemId,
            DateRange            = body.DateRange,
            GroupBy              = body.GroupBy,
            Metrics              = body.Metrics,
            AdditionalDimensions = body.AdditionalDimensions,
            QueryTitle           = body.QueryTitle
        };
}

/// <summary>Response returned when a Bid Manager query is created asynchronously.</summary>
public sealed class CreateQueryResponse
{
    /// <summary>Bid Manager query ID. Use this to track report generation status.</summary>
    public long QueryId { get; set; }
}
